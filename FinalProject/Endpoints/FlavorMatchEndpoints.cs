using FinalProject.Data;
using FinalProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Endpoints
{
    public static class FlavorMatchEndpoints
    {
        // DTO
        public record FlavorRecoRequest(string Base, List<string> Flavors, int Take = 6);
        public record FlavorRecoItem(int? Id, string Name, string Type, string Province, string ImageUrl,
                                     double Rating, int RatingCount, decimal? Price, string Why, double Score);
        public record FlavorRecoResponse(string Base, string[] Flavors, List<FlavorRecoItem> Items);

        public static IEndpointRouteBuilder MapFlavorMatchEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/reco/flavor-match", async ([FromBody] FlavorRecoRequest req, AppDbContext db) =>
            {
                if (req is null || string.IsNullOrWhiteSpace(req.Base))
                    return Results.BadRequest("base is required");

                var baseNorm = req.Base.Trim();
                var flavors = req.Flavors?.Where(s => !string.IsNullOrWhiteSpace(s))
                                          .Select(s => s.Trim()).Distinct().ToList() ?? new();
                var take = req.Take <= 0 ? 6 : Math.Clamp(req.Take, 3, 24);

                if (!baseNorm.Equals("Mocktail", StringComparison.OrdinalIgnoreCase))
                {
                    var all = await db.LocalBeers.AsNoTracking().Take(300).ToListAsync();
                    var scored = all.Select(b => new { Item = b, Score = FlavorSimilarity(b, baseNorm, flavors) })
                                    .Where(x => x.Score > 0.4)
                                    .OrderByDescending(x => x.Score)
                                    .ThenByDescending(x => x.Item.Rating)
                                    .ThenByDescending(x => x.Item.RatingCount)
                                    .Take(take)
                                    .ToList();

                    var items = scored.Select(x => new FlavorRecoItem(
                        x.Item.Id,
                        x.Item.Name,
                        x.Item.TypeOfLiquor ?? x.Item.Type ?? "",
                        x.Item.Province,
                        string.IsNullOrWhiteSpace(x.Item.ImageUrl) ? "" : x.Item.ImageUrl!,
                        x.Item.Rating,
                        x.Item.RatingCount,
                        x.Item.Price,
                        $"เข้ากับโทน {string.Join(", ", flavors)} และคะแนนรีวิว {x.Item.Rating:0.0}/5",
                        Math.Round(x.Score, 2)
                    )).ToList();

                    if (items.Any())
                        return Results.Ok(new FlavorRecoResponse(baseNorm, flavors.ToArray(), items));
                }

                // fallback
                var curated = Curated(baseNorm);
                return Results.Ok(new FlavorRecoResponse(baseNorm, flavors.ToArray(), curated.Take(take).ToList()));
            })
            .AllowAnonymous()
            .Produces<FlavorRecoResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

            return app;
        }

        // === internals ===
        static double FlavorSimilarity(LocalBeer b, string baseName, List<string> flavors)
        {
            double baseScore = 0;
            var baseNorm = (baseName ?? "").Trim().ToLowerInvariant();
            var typeNorm = (b.TypeOfLiquor ?? b.Type ?? "").Trim().ToLowerInvariant();

            bool isBeer = typeNorm.Contains("beer") || typeNorm.Contains("เบียร์");
            bool isWine = typeNorm.Contains("wine") || typeNorm.Contains("ไวน์");
            bool isWhisky = typeNorm.Contains("whisky") || typeNorm.Contains("whiskey") || typeNorm.Contains("วิสกี้");
            bool isRum = typeNorm.Contains("rum") || typeNorm.Contains("รัม");
            bool isGin = typeNorm.Contains("gin") || typeNorm.Contains("จิน");
            bool isThai = typeNorm.Contains("thai") || typeNorm.Contains("local") || typeNorm.Contains("สุรา") || typeNorm.Contains("ลาว");

            if ((baseNorm == "beer" && isBeer) ||
                (baseNorm == "wine" && isWine) ||
                (baseNorm == "whisky" && isWhisky) ||
                (baseNorm == "rum" && isRum) ||
                (baseNorm == "gin" && isGin) ||
                (baseNorm == "thai craft" && isThai))
                baseScore = 1.0;
            else if (baseNorm == "mocktail")
                baseScore = 0.2;

            string hay = string.Join(" | ", new[]
            { b.MainIngredients, b.Notes, b.Description, b.Type, b.TypeOfLiquor }
            .Where(s => !string.IsNullOrWhiteSpace(s))).ToLowerInvariant();

            var flavorKeywords = new Dictionary<string, string[]>
            {
                ["citrus"] = new[] { "citrus", "lemon", "lime", "ส้ม", "เปรี้ยว" },
                ["herb"] = new[] { "herb", "rosemary", "thyme", "basil", "สมุนไพร", "ใบไม้" },
                ["sweet"] = new[] { "sweet", "honey", "caramel", "vanilla", "หวาน" },
                ["bitter"] = new[] { "bitter", "ipa", "ฮอป", "ขม" },
                ["smoke"] = new[] { "smoke", "peated", "ควัน", "สโมค" },
                ["spice"] = new[] { "spice", "pepper", "clove", "เครื่องเทศ", "ซินนามอน" },
                ["malty"] = new[] { "malt", "toffee", "biscuit", "มอลต์" },
                ["hoppy"] = new[] { "hop", "citra", "mosaic", "ฮอป" },
                ["fruity"] = new[] { "fruit", "berry", "apple", "กลิ่นผลไม้" },
                ["floral"] = new[] { "floral", "ดอกไม้", "ลาเวนเดอร์" },
                ["woody"] = new[] { "oak", "wood", "ไม้โอ๊ก", "ไม้" },
            };

            double flavorScore = 0;
            foreach (var f in flavors.Select(x => x.Trim().ToLowerInvariant()))
            {
                if (!flavorKeywords.TryGetValue(f, out var kws)) continue;
                foreach (var kw in kws)
                    if (hay.Contains(kw)) flavorScore += 0.6;
            }

            var ratingScore = Math.Clamp(b.Rating / 5.0, 0, 1) * 0.5;
            return baseScore * 1.0 + flavorScore + ratingScore;
        }

        static List<FlavorRecoItem> Curated(string baseNorm)
        {
            string why(string f) => $"โทนเด่น: {f} • ครีเอตโดย Sip & Trip";
            var list = new List<FlavorRecoItem>();
            if (baseNorm.Equals("Beer", StringComparison.OrdinalIgnoreCase))
                list.AddRange(new[]
                {
                    new FlavorRecoItem(null,"Citrus Session IPA","Beer • Session IPA","—","",4.2,0,null,why("Citrus, Hoppy"),2.1),
                    new FlavorRecoItem(null,"Amber Malty Ale","Beer • Amber Ale","—","",4.0,0,null,why("Malty, Caramel"),1.8),
                    new FlavorRecoItem(null,"Herbal Pils","Beer • Pilsner","—","",3.9,0,null,why("Herb, Crisp"),1.6),
                });
            else if (baseNorm.Equals("Wine", StringComparison.OrdinalIgnoreCase))
                list.AddRange(new[]
                {
                    new FlavorRecoItem(null,"Citrus-forward Riesling","White Wine","—","",4.3,0,null,why("Citrus, Floral"),2.0),
                    new FlavorRecoItem(null,"Berry Pinot Noir","Red Wine","—","",4.1,0,null,why("Fruity, Floral"),1.8),
                });
            else if (baseNorm.Equals("Whisky", StringComparison.OrdinalIgnoreCase))
                list.AddRange(new[]
                {
                    new FlavorRecoItem(null,"Lightly Peated Malt","Single Malt","—","",4.4,0,null,why("Smoke, Woody"),2.2),
                    new FlavorRecoItem(null,"Honey Oak Blend","Blended","—","",4.0,0,null,why("Sweet, Woody"),1.7),
                });
            else if (baseNorm.Equals("Rum", StringComparison.OrdinalIgnoreCase))
                list.AddRange(new[]
                {
                    new FlavorRecoItem(null,"Spiced Island Rum","Rum","—","",4.2,0,null,why("Spice, Sweet"),1.9),
                    new FlavorRecoItem(null,"Citrus Highball Rum","Rum Highball","—","",4.0,0,null,why("Citrus, Fizz"),1.6),
                });
            else if (baseNorm.Equals("Gin", StringComparison.OrdinalIgnoreCase))
                list.AddRange(new[]
                {
                    new FlavorRecoItem(null,"Herbal Dry Gin Tonic","Gin","—","",4.1,0,null,why("Herb, Citrus"),1.8),
                    new FlavorRecoItem(null,"Floral Gin Fizz","Gin","—","",4.0,0,null,why("Floral, Fizz"),1.6),
                });
            else if (baseNorm.Equals("Thai Craft", StringComparison.OrdinalIgnoreCase))
                list.AddRange(new[]
                {
                    new FlavorRecoItem(null,"สุราท้องถิ่นสมุนไพร","Thai Craft","—","",4.0,0,null,why("Herb, Spice"),1.7),
                    new FlavorRecoItem(null,"น้ำตาลโตนดโอ๊ค","Thai Craft","—","",3.9,0,null,why("Sweet, Woody"),1.5),
                });
            else // Mocktail
                list.AddRange(new[]
                {
                    new FlavorRecoItem(null,"Citrus & Herb Cooler","Mocktail","—","",4.5,0,null,why("Citrus, Herb"),2.2),
                    new FlavorRecoItem(null,"Berry Fizz","Mocktail","—","",4.3,0,null,why("Fruity, Fizz"),2.0),
                });

            return list;
        }
    }
}
