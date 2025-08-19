using FinalProject.Data;
using FinalProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Endpoints
{
    public static class FlavorMatchEndpoints
    {
        // DTO (เฉพาะ endpoint options)
        public record FlavorOptionsResponse(List<string> Flavors, List<string> Foods, List<string> Moods);

        public static IEndpointRouteBuilder MapFlavorMatchEndpoints(this IEndpointRouteBuilder app)
        {
            // ================================
            // 1) OPTIONS: คืนตัวเลือกตาม Base
            // ================================
            app.MapGet("/api/reco/flavor-options", async (
                [FromQuery] string? @base,
                AppDbContext db) =>
            {
                var baseNorm = (@base ?? "").Trim();
                if (string.IsNullOrWhiteSpace(baseNorm))
                {
                    // ถ้าไม่ส่ง base มา ให้คืนทุกอย่าง (distinct ทั้งระบบ)
                    var allFlavor = await db.LocalBeerFlavors
                        .Select(f => f.Flavor)
                        .Where(s => s != null && s != "")
                        .Distinct()
                        .OrderBy(s => s)
                        .ToListAsync();

                    var allFoods = await db.LocalBeerFoodPairings
                        .Select(f => f.FoodName)
                        .Where(s => s != null && s != "")
                        .Distinct()
                        .OrderBy(s => s)
                        .ToListAsync();

                    var allMoods = await db.LocalBeerMoodPairings
                        .Select(m => m.Mood)
                        .Where(s => s != null && s != "")
                        .Distinct()
                        .OrderBy(s => s)
                        .ToListAsync();

                    return Results.Ok(new FlavorOptionsResponse(allFlavor, allFoods, allMoods));
                }

                // โหลดเบียร์ทั้งหมด (พร้อม relations) แล้วคัดทีหลังให้รองรับคำไทย/อังกฤษ
                var allBeers = await db.LocalBeers
                    .AsNoTracking()
                    .ToListAsync();

                var baseFiltered = allBeers.Where(b => MatchesBaseLoose(b, baseNorm)).Select(b => b.Id).ToList();
                if (baseFiltered.Count == 0)
                    return Results.Ok(new FlavorOptionsResponse(new(), new(), new()));

                // Query รายการ option เฉพาะเบียร์ในหมวดนั้น
                var flavors = await db.LocalBeerFlavors
                    .Where(f => baseFiltered.Contains(f.LocalBeerId))
                    .Select(f => f.Flavor)
                    .Where(s => s != null && s != "")
                    .Distinct()
                    .OrderBy(s => s)
                    .ToListAsync();

                var foods = await db.LocalBeerFoodPairings
                    .Where(f => baseFiltered.Contains(f.LocalBeerId))
                    .Select(f => f.FoodName)
                    .Where(s => s != null && s != "")
                    .Distinct()
                    .OrderBy(s => s)
                    .ToListAsync();

                var moods = await db.LocalBeerMoodPairings
                    .Where(m => baseFiltered.Contains(m.LocalBeerId))
                    .Select(m => m.Mood)
                    .Where(s => s != null && s != "")
                    .Distinct()
                    .OrderBy(s => s)
                    .ToListAsync();

                return Results.Ok(new FlavorOptionsResponse(flavors, foods, moods));
            })
            .AllowAnonymous()
            .Produces<FlavorOptionsResponse>(StatusCodes.Status200OK);

            // =================================
            // 2) MATCH: แนะนำโดยกรองตาม Base ก่อน
            // =================================
            app.MapPost("/api/reco/flavor-match", async (
                [FromBody] FlavorRecoRequest req, AppDbContext db) =>
            {
                if (req is null || string.IsNullOrWhiteSpace(req.Base))
                    return Results.BadRequest("base is required");

                var baseNorm = req.Base.Trim();
                var flavors = req.Flavors?.Where(s => !string.IsNullOrWhiteSpace(s))
                                          .Select(s => s.Trim()).Distinct().ToList() ?? new();
                var take = req.Take <= 0 ? 6 : Math.Clamp(req.Take, 3, 24);

                // โหลดทั้งหมดพร้อม relations (จำกัดจำนวน) แล้วกรองตาม Base ก่อนคำนวณ
                var all = await db.LocalBeers
                    .Include(b => b.Flavors)
                    .Include(b => b.FoodPairings)
                    .Include(b => b.MoodPairings)
                    .AsNoTracking()
                    .Take(1000)
                    .ToListAsync();

                // กรองเฉพาะหมวดที่เลือก (Beer/Wine/Whisky/…)
                var candidates = all.Where(b => MatchesBaseLoose(b, baseNorm)).ToList();

                var scored = candidates
                    .Select(b => new { Item = b, Score = FlavorSimilarity(b, baseNorm, flavors) })
                    .Where(x => x.Score > 0.4)
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.Item.Rating)
                    .ThenByDescending(x => x.Item.RatingCount)
                    .Take(take)
                    .Select(x => new FlavorRecoItem
                    {
                        Id = x.Item.Id,
                        Name = x.Item.Name,
                        Type = x.Item.TypeOfLiquor ?? x.Item.Type ?? "",
                        Province = x.Item.Province,
                        ImageUrl = x.Item.ImageUrl ?? "",
                        Rating = x.Item.Rating,
                        RatingCount = x.Item.RatingCount,
                        Price = x.Item.Price,
                        Why = BuildWhy(x.Item, flavors),
                        Score = Math.Round(x.Score, 2)
                    })
                    .ToList();

                if (scored.Any())
                {
                    return Results.Ok(new FlavorRecoResponse
                    {
                        Base = baseNorm,
                        Flavors = flavors.ToArray(),
                        Items = scored
                    });
                }

                // fallback curated เฉพาะ base นั้น ๆ
                return Results.Ok(new FlavorRecoResponse
                {
                    Base = baseNorm,
                    Flavors = flavors.ToArray(),
                    Items = Curated(baseNorm).Take(take).ToList()
                });
            })
            .AllowAnonymous()
            .Produces<FlavorRecoResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

            return app;
        }

        // ============ Helpers ============

        // กรองตาม Base แบบหลวม ๆ รองรับคำไทย/อังกฤษหลายแบบ
        static bool MatchesBaseLoose(LocalBeer b, string baseName)
        {
            var typeNorm = (b.TypeOfLiquor ?? b.Type ?? "").Trim().ToLowerInvariant();

            string bn = baseName.Trim().ToLowerInvariant();

            // keyword ของแต่ละ base
            var map = new Dictionary<string, string[]>
            {
                ["beer"] = new[] { "beer", "เบียร์", "ipa", "ลาเกอร์", "เอล", "stout", "สโตต", "เบียร์ลาเกอร์" },
                ["wine"] = new[] { "wine", "ไวน์", "sparkling", "สปาร์กลิ้ง" },
                ["whisky"] = new[] { "whisky", "whiskey", "วิสกี้" },
                ["rum"] = new[] { "rum", "รัม" },
                ["gin"] = new[] { "gin", "จิน" },
                ["thai craft"] = new[] { "thai", "ท้องถิ่น", "สุรา", "ลาว", "คราฟท์", "craft" },
                ["mocktail"] = new[] { "mocktail", "ม็อคเทล", "เครื่องดื่มไร้แอลกอฮอล์" },
            };

            string key = bn;
            if (!map.ContainsKey(key)) return false;

            return map[key].Any(kw => typeNorm.Contains(kw));
        }

        static double FlavorSimilarity(LocalBeer b, string baseName, List<string> flavors)
        {
            // Base match score
            double baseScore = 0;
            var baseNorm = (baseName ?? "").Trim().ToLowerInvariant();
            if (MatchesBaseLoose(b, baseNorm)) baseScore = (baseNorm == "mocktail") ? 0.3 : 1.0;

            // Flavor score จากตารางจริง
            var beerFlavors = b.Flavors?.Select(f => f.Flavor.ToLowerInvariant()).ToHashSet()
                              ?? new HashSet<string>();
            double flavorScore = 0;
            foreach (var f in flavors.Select(x => x.ToLowerInvariant()))
                if (beerFlavors.Contains(f)) flavorScore += 0.75;

            // Rating score (นิ่ม ๆ)
            var ratingScore = Math.Clamp(b.Rating / 5.0, 0, 1) * 0.5;

            // รวมและจำกัด
            var total = baseScore + flavorScore + ratingScore;
            return Math.Clamp(total, 0, 3.5);
        }

        static string BuildWhy(LocalBeer b, List<string> reqFlavors)
        {
            var reasons = new List<string>();

            if (b.Flavors?.Any() == true)
            {
                var matched = b.Flavors
                    .Where(f => reqFlavors.Contains(f.Flavor, StringComparer.OrdinalIgnoreCase))
                    .Select(f => f.Flavor)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (matched.Any())
                    reasons.Add($"รสเด่นตรงใจ: {string.Join(", ", matched)}");
            }

            if (b.FoodPairings?.Any() == true)
                reasons.Add($"อาหารแนะนำ: {string.Join(", ", b.FoodPairings.Select(f => f.FoodName))}");

            if (b.MoodPairings?.Any() == true)
                reasons.Add($"อารมณ์ที่ใช่: {string.Join(", ", b.MoodPairings.Select(m => m.Mood))}");

            reasons.Add($"รีวิว {b.Rating:0.0}/5 ({b.RatingCount} คน)");
            return string.Join(" • ", reasons);
        }

        static List<FlavorRecoItem> Curated(string baseNorm)
        {
            string why(string f) => $"โทนเด่น: {f} • curated โดย Sip & Trip";
            var list = new List<FlavorRecoItem>();

            if (baseNorm.Equals("Beer", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new FlavorRecoItem { Name = "Citrus Session IPA", Type = "Beer", Province = "—", Why = why("Citrus, Hoppy"), Score = 2.1 });
                list.Add(new FlavorRecoItem { Name = "Amber Malty Ale", Type = "Beer", Province = "—", Why = why("Malty, Caramel"), Score = 1.8 });
            }
            else if (baseNorm.Equals("Wine", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new FlavorRecoItem { Name = "Citrus-forward Riesling", Type = "Wine", Province = "—", Why = why("Citrus, Floral"), Score = 2.0 });
            }
            else if (baseNorm.Equals("Whisky", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new FlavorRecoItem { Name = "Lightly Peated Malt", Type = "Whisky", Province = "—", Why = why("Smoke, Woody"), Score = 2.0 });
            }
            else if (baseNorm.Equals("Rum", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new FlavorRecoItem { Name = "Island Spiced Rum", Type = "Rum", Province = "—", Why = why("Spice, Sweet"), Score = 1.9 });
            }
            else if (baseNorm.Equals("Gin", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new FlavorRecoItem { Name = "Herbal Dry G&T", Type = "Gin", Province = "—", Why = why("Herb, Citrus"), Score = 1.8 });
            }
            else if (baseNorm.Equals("Thai Craft", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new FlavorRecoItem { Name = "สุราท้องถิ่นสมุนไพร", Type = "Thai Craft", Province = "—", Why = why("Herb, Spice"), Score = 1.7 });
            }
            else // Mocktail
            {
                list.Add(new FlavorRecoItem { Name = "Citrus & Herb Cooler", Type = "Mocktail", Province = "—", Why = why("Citrus, Herb"), Score = 2.2 });
            }

            return list;
        }
    }
}
