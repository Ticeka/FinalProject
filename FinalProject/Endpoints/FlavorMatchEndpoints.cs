using FinalProject.Data;
using FinalProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Endpoints
{
    public static class FlavorMatchEndpoints
    {
        // DTO
        public record FlavorOptionsResponse(List<string> Flavors, List<string> Foods, List<string> Moods);

        // Fixed option sets (ตาม requirement)
        static readonly List<string> FixedFlavors = new() { "หวาน", "เปรี้ยว", "ขม", "เค็ม", "อูมามิ" };
        static readonly List<string> FixedFoods = new() { "ทะเล", "เนื้อ", "ไก่", "หมู" };
        static readonly List<string> FixedMoods = new() { "Party", "Chill", "Celebration", "Fresh", "Sport" };

        // Synonyms to make matching flexible (ไทย/อังกฤษ/คำพ้อง)
        static readonly Dictionary<string, string[]> TasteSynonyms = new(StringComparer.OrdinalIgnoreCase)
        {
            ["หวาน"] = new[] { "sweet", "honey", "caramel", "vanilla", "fruity", "molasses", "sweet rice" },
            ["เปรี้ยว"] = new[] { "sour", "tart", "citrus", "lemon", "lime", "yuzu" },
            ["ขม"] = new[] { "bitter", "hoppy", "roasted", "dark chocolate", "coffee", "ipa" },
            ["เค็ม"] = new[] { "salty", "saline", "briny", "mineral" },
            ["อูมามิ"] = new[] { "umami", "savory", "yeasty", "brothy", "mushroom" },
        };

        static readonly Dictionary<string, string[]> FoodSynonyms = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ทะเล"] = new[] { "ทะเล", "seafood", "ปลา", "fish", "กุ้ง", "shrimp", "หอย", "shell", "ปู", "crab", "oyster", "ซูชิ", "sashimi" },
            ["เนื้อ"] = new[] { "เนื้อ", "beef", "สเต็ก", "steak", "ribeye", "วากิว" },
            ["ไก่"] = new[] { "ไก่", "chicken", "ปีกไก่", "ข้าวหมกไก่" },
            ["หมู"] = new[] { "หมู", "pork", "เบคอน", "คอหมู", "หมูกรอบ", "หมูย่าง", "หมูแดดเดียว" },
        };

        static readonly Dictionary<string, string[]> MoodSynonyms = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Party"] = new[] { "Party", "ปาร์ตี้", "สนุก" },
            ["Chill"] = new[] { "Chill", "ชิล", "Relax", "ผ่อนคลาย" },
            ["Celebration"] = new[] { "Celebration", "ฉลอง", "เฉลิมฉลอง" },
            ["Fresh"] = new[] { "Fresh", "สดชื่น", "Refresh" },
            ["Sport"] = new[] { "Sport", "กีฬา", "Active" },
        };

        public static IEndpointRouteBuilder MapFlavorMatchEndpoints(this IEndpointRouteBuilder app)
        {
            // ================================
            // 1) OPTIONS: คืนชุดตายตัวเสมอ
            // ================================
            app.MapGet("/api/reco/flavor-options", (
                [FromQuery] string? @base,
                AppDbContext db) =>
            {
                return Results.Ok(new FlavorOptionsResponse(
                    new List<string>(FixedFlavors),
                    new List<string>(FixedFoods),
                    new List<string>(FixedMoods)
                ));
            })
            .AllowAnonymous()
            .Produces<FlavorOptionsResponse>(StatusCodes.Status200OK);

            // =================================
            // 2) MATCH: แนะนำโดยกรองตาม Base ก่อน
            // =================================
            app.MapPost("/api/reco/flavor-match", async ([FromBody] FlavorRecoRequest req, AppDbContext db) =>
            {
                if (req is null || string.IsNullOrWhiteSpace(req.Base))
                    return Results.BadRequest("base is required");

                var baseNorm = req.Base.Trim();
                var flavors = (req.Flavors ?? new()).Where(s => !string.IsNullOrWhiteSpace(s))
                                                    .Select(s => s.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var foods = (req.Foods ?? new()).Where(s => !string.IsNullOrWhiteSpace(s))
                                                    .Select(s => s.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var moods = (req.Moods ?? new()).Where(s => !string.IsNullOrWhiteSpace(s))
                                                    .Select(s => s.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                var take = req.Take <= 0 ? 6 : Math.Clamp(req.Take, 3, 24);

                // โหลดข้อมูลพร้อม relations
                var all = await db.LocalBeers
                    .Include(b => b.Flavors)
                    .Include(b => b.FoodPairings)
                    .Include(b => b.MoodPairings)
                    .AsNoTracking()
                    .Take(1000)
                    .ToListAsync();

                // 1) Base filter (หลวม ๆ)
                var baseCandidates = all.Where(b => MatchesBaseLoose(b, baseNorm)).ToList();

                // 2) HARD FILTER — ต้องมีครบทุกตัวเลือกที่ผู้ใช้ติ๊ก (ตรวจแบบคำพ้อง)
                bool HasAll(LocalBeer b)
                {
                    var bFlavors = (b.Flavors ?? new()).Select(x => x.Flavor ?? "");
                    var bFoods = (b.FoodPairings ?? new()).Select(x => x.FoodName ?? "");
                    var bMoods = (b.MoodPairings ?? new()).Select(x => x.Mood ?? "");

                    bool flavorsOk = !flavors.Any() || flavors.All(f => AnyMatch(f, bFlavors, TasteSynonyms));
                    bool foodsOk = !foods.Any() || foods.All(f => AnyMatch(f, bFoods, FoodSynonyms));
                    bool moodsOk = !moods.Any() || moods.All(m => AnyMatch(m, bMoods, MoodSynonyms));

                    return flavorsOk && foodsOk && moodsOk;
                }

                var strict = baseCandidates.Where(HasAll).ToList();

                if (strict.Any())
                {
                    // ถ้าเจอจาก hard filter — เรียงคุณภาพ (ดาว/รีวิว/ราคา)
                    var items = strict
                        .OrderByDescending(b => b.Rating)
                        .ThenByDescending(b => b.RatingCount)
                        .ThenBy(b => b.Price == null || b.Price == 0 ? 1 : 0) // มีราคา > 0 ขึ้นก่อน
                        .Take(take)
                        .Select(b => new FlavorRecoItem
                        {
                            Id = b.Id,
                            Name = b.Name,
                            Type = b.TypeOfLiquor ?? b.Type ?? "",
                            Province = b.Province,
                            ImageUrl = b.ImageUrl ?? "",
                            Rating = b.Rating,
                            RatingCount = b.RatingCount,
                            Price = b.Price,
                            Why = BuildWhy(b, flavors, foods, moods),
                            Score = 3.5  // ให้เต็มเพื่อสื่อว่า "ตรงตามตัวกรอง"
                        })
                        .ToList();

                    return Results.Ok(new FlavorRecoResponse { Base = baseNorm, Flavors = flavors.ToArray(), Items = items });
                }

                // 3) ถ้า hard filter ว่าง → ใช้คะแนนความใกล้เคียง (soft)
                var scored = baseCandidates
                    .Select(b => new { Item = b, Score = FlavorSimilarity(b, baseNorm, flavors, foods, moods) })
                    .Where(x => x.Score > 0.4) // เกณฑ์คัดกรองเบื้องต้น
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
                        Why = BuildWhy(x.Item, flavors, foods, moods),
                        Score = Math.Round(x.Score, 2)
                    })
                    .ToList();

                if (scored.Any())
                    return Results.Ok(new FlavorRecoResponse { Base = baseNorm, Flavors = flavors.ToArray(), Items = scored });

                // 4) ไม่มีข้อมูลพอ — mock เล็กน้อยให้ UI แสดงผลได้
                return Results.Ok(new FlavorRecoResponse { Base = baseNorm, Flavors = flavors.ToArray(), Items = new List<FlavorRecoItem>() });
            })
            .AllowAnonymous();

            return app;
        }

        // ============ Helpers ============

        // กรองตาม Base: เหล้า, เบียร์, ไวน์, สาโท
        static bool MatchesBaseLoose(LocalBeer b, string baseName)
        {
            var typeNorm = (b.TypeOfLiquor ?? b.Type ?? "").Trim().ToLowerInvariant();
            var bn = (baseName ?? "").Trim().ToLowerInvariant();

            var map = new Dictionary<string, string[]>
            {
                ["เบียร์"] = new[] { "เบียร์", "beer", "ipa", "ลาเกอร์", "lager", "เอล", "ale", "stout", "สเตาท์", "pilsner" },
                ["ไวน์"] = new[] { "ไวน์", "wine", "sparkling", "สปาร์กลิ้ง", "riesling", "merlot", "cabernet", "chardonnay" },
                ["เหล้า"] = new[] { "เหล้า", "spirit", "spirits", "liquor", "วิสกี้", "whisky", "whiskey", "รัม", "rum", "จิน", "gin", "วอดก้า", "vodka", "เตกีลา", "tequila", "บรั่นดี", "brandy" },
                ["สาโท"] = new[] { "สาโท", "sato", "rice wine", "sweet rice", "ข้าวหมัก", "สุราข้าว" },
            };

            if (!map.TryGetValue(bn, out var keywords)) return false;
            return keywords.Any(kw => typeNorm.Contains(kw));
        }

        // Flexible string match using synonyms table
        static bool AnyMatch(string requested, IEnumerable<string> values, Dictionary<string, string[]> dict)
        {
            var list = values?.Where(v => !string.IsNullOrWhiteSpace(v)).ToList() ?? new();
            if (list.Count == 0) return false;

            if (list.Any(v => v.Contains(requested, StringComparison.OrdinalIgnoreCase))) return true;

            if (dict.TryGetValue(requested, out var syns))
                return list.Any(v => syns.Any(s => v.Contains(s, StringComparison.OrdinalIgnoreCase)));

            return false;
        }

        static double FlavorSimilarity(LocalBeer b, string baseName,
            List<string> flavors, List<string> foods, List<string> moods)
        {
            // 1) Base weight
            double baseScore = MatchesBaseLoose(b, baseName) ? 1.0 : 0.0;

            // 2) Flavors (ใช้คำพ้อง)
            var beerFlavors = b.Flavors?.Select(f => f.Flavor ?? "").ToList() ?? new();
            double flavorScore = 0;
            foreach (var f in flavors)
                if (AnyMatch(f, beerFlavors, TasteSynonyms)) flavorScore += 0.75;

            // 3) Foods / Moods
            var beerFoods = b.FoodPairings?.Select(f => f.FoodName ?? "").ToList() ?? new();
            var beerMoods = b.MoodPairings?.Select(m => m.Mood ?? "").ToList() ?? new();

            double foodScore = foods.Count(f => AnyMatch(f, beerFoods, FoodSynonyms)) * 0.40;
            double moodScore = moods.Count(m => AnyMatch(m, beerMoods, MoodSynonyms)) * 0.30;

            // 4) Rating (นิ่ม ๆ)
            var ratingScore = Math.Clamp(b.Rating / 5.0, 0, 1) * 0.5;

            var total = baseScore + flavorScore + foodScore + moodScore + ratingScore;
            return Math.Clamp(total, 0, 3.5);
        }

        static string BuildWhy(LocalBeer b, List<string> reqFlavors, List<string> reqFoods, List<string> reqMoods)
        {
            var reasons = new List<string>();

            if (b.Flavors?.Any() == true)
            {
                var matched = b.Flavors
                    .Where(f => reqFlavors.Any(r => AnyMatch(r, new[] { f.Flavor ?? "" }, TasteSynonyms)))
                    .Select(f => f.Flavor)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (matched.Any()) reasons.Add($"รสเด่นตรงใจ: {string.Join(", ", matched)}");
            }

            if (b.FoodPairings?.Any() == true)
            {
                var matched = b.FoodPairings
                    .Where(f => reqFoods.Any(r => AnyMatch(r, new[] { f.FoodName ?? "" }, FoodSynonyms)))
                    .Select(f => f.FoodName)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (matched.Any()) reasons.Add($"เข้าคู่กับ: {string.Join(", ", matched)}");
            }

            if (b.MoodPairings?.Any() == true)
            {
                var matched = b.MoodPairings
                    .Where(m => reqMoods.Any(r => AnyMatch(r, new[] { m.Mood ?? "" }, MoodSynonyms)))
                    .Select(m => m.Mood)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (matched.Any()) reasons.Add($"เหมาะกับอารมณ์: {string.Join(", ", matched)}");
            }

            return string.Join(" • ", reasons);
        }
    }
}
