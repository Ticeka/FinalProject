using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using FinalProject.Data;
using FinalProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinalProject.Pages.Admin.Beers
{
    [Authorize(Roles = "Admin")]
    public class AddDetailModel : PageModel
    {
        private readonly AppDbContext _context;
        public AddDetailModel(AppDbContext context) => _context = context;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [TempData] public string? Flash { get; set; }

        public class InputModel
        {
            [Required, StringLength(200)]
            public string Name { get; set; } = default!;

            [Required, StringLength(100)]
            public string Province { get; set; } = default!;

            public string? Description { get; set; }
            public string? District { get; set; }
            public string? Type { get; set; }
            public string? Address { get; set; }

            public double Latitude { get; set; }
            public double Longitude { get; set; }

            // อนุญาตให้เป็น URL เต็ม, พาธ (/...), หรือชื่อไฟล์
            public string? ImageUrl { get; set; }

            // จะเติม https:// ให้อัตโนมัติถ้าไม่ใส่ scheme
            public string? Website { get; set; }
            public string? FacebookPage { get; set; }

            public string? PhoneNumber { get; set; }
            public string? OpenHours { get; set; }

            public double AlcoholLevel { get; set; }

            [Range(0, 999999)]
            public decimal Price { get; set; }

            public string? PlaceOfOrigin { get; set; }
            public string? Region { get; set; }
            public string? Creator { get; set; }
            public int? Volume { get; set; }
            public string? MainIngredients { get; set; }
            public string? ProductMethod { get; set; }
            public int? ProductYear { get; set; }
            public string? Rights { get; set; }
            public string? Distributor { get; set; }
            public string? DistributorChanel { get; set; }
            public string? Award { get; set; }
            public string? Notes { get; set; }
            public string? ProductId { get; set; }
            public string? TypeOfLiquor { get; set; }

            public double? AverageRating { get; set; } // หากต้องการให้ระบบคำนวณเอง สามารถไม่แสดงในฟอร์มได้

            // Pairings อย่างละ 1
            public string? Flavor { get; set; }
            public double? FlavorIntensity { get; set; }
            public string? FoodName { get; set; }
            public string? FoodReason { get; set; }
            public string? Mood { get; set; }
            public string? MoodReason { get; set; }
        }

        public void OnGet()
        {
            // ค่าดีฟอลต์ (ถ้าต้องการ)
            Input.AlcoholLevel = 0;
            Input.Price = 0;
        }

        // เติม https:// ให้อัตโนมัติถ้าไม่มี scheme + อนุญาต path/ไฟล์สำหรับรูป
        private static string? NormalizeUrl(string? raw, bool allowRelativeOrFilename = false)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            raw = raw.Trim();

            if (allowRelativeOrFilename)
            {
                if (raw.StartsWith("/")) return raw;      // /images/beer.jpg
                if (!raw.Contains("://")) return raw;     // beer.jpg หรือ slug/filename
            }

            if (raw.Contains("://")) return raw;          // http:// หรือ https://
            return "https://" + raw;                      // เติมให้ website/facebook
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var e = new LocalBeer
            {
                Name = Input.Name.Trim(),
                Province = Input.Province.Trim(),
                Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim(),
                District = string.IsNullOrWhiteSpace(Input.District) ? null : Input.District.Trim(),
                Type = string.IsNullOrWhiteSpace(Input.Type) ? null : Input.Type.Trim(),
                Address = string.IsNullOrWhiteSpace(Input.Address) ? null : Input.Address.Trim(),
                Latitude = Input.Latitude,
                Longitude = Input.Longitude,

                // URL / Path / Filename
                ImageUrl = NormalizeUrl(Input.ImageUrl, allowRelativeOrFilename: true),
                Website = NormalizeUrl(Input.Website),
                FacebookPage = NormalizeUrl(Input.FacebookPage),

                PhoneNumber = string.IsNullOrWhiteSpace(Input.PhoneNumber) ? null : Input.PhoneNumber.Trim(),
                OpenHours = string.IsNullOrWhiteSpace(Input.OpenHours) ? null : Input.OpenHours.Trim(),

                AlcoholLevel = Input.AlcoholLevel,
                Price = Input.Price,
                PlaceOfOrigin = string.IsNullOrWhiteSpace(Input.PlaceOfOrigin) ? null : Input.PlaceOfOrigin.Trim(),
                Region = string.IsNullOrWhiteSpace(Input.Region) ? null : Input.Region.Trim(),
                Creator = string.IsNullOrWhiteSpace(Input.Creator) ? null : Input.Creator.Trim(),
                Volume = Input.Volume,
                MainIngredients = string.IsNullOrWhiteSpace(Input.MainIngredients) ? null : Input.MainIngredients.Trim(),
                ProductMethod = string.IsNullOrWhiteSpace(Input.ProductMethod) ? null : Input.ProductMethod.Trim(),
                ProductYear = Input.ProductYear,
                Rights = string.IsNullOrWhiteSpace(Input.Rights) ? null : Input.Rights.Trim(),
                Distributor = string.IsNullOrWhiteSpace(Input.Distributor) ? null : Input.Distributor.Trim(),
                DistributorChanel = string.IsNullOrWhiteSpace(Input.DistributorChanel) ? null : Input.DistributorChanel.Trim(),
                Award = string.IsNullOrWhiteSpace(Input.Award) ? null : Input.Award.Trim(),
                Notes = string.IsNullOrWhiteSpace(Input.Notes) ? null : Input.Notes.Trim(),
                ProductId = string.IsNullOrWhiteSpace(Input.ProductId) ? null : Input.ProductId.Trim(),
                TypeOfLiquor = string.IsNullOrWhiteSpace(Input.TypeOfLiquor) ? null : Input.TypeOfLiquor.Trim(),
                AverageRating = Input.AverageRating,

                CreatedAt = DateTime.Now,
                UpdatedAt = null
            };

            // ---- Pairings: อย่างละ 1 ----
            if (!string.IsNullOrWhiteSpace(Input.Flavor))
            {
                e.Flavors.Add(new LocalBeerFlavor
                {
                    Flavor = Input.Flavor.Trim(),
                    Intensity = Input.FlavorIntensity ?? 0
                });
            }

            if (!string.IsNullOrWhiteSpace(Input.FoodName))
            {
                e.FoodPairings.Add(new LocalBeerFoodPairing
                {
                    FoodName = Input.FoodName.Trim(),
                    Reason = string.IsNullOrWhiteSpace(Input.FoodReason) ? null : Input.FoodReason.Trim()
                });
            }

            if (!string.IsNullOrWhiteSpace(Input.Mood))
            {
                e.MoodPairings.Add(new LocalBeerMoodPairing
                {
                    Mood = Input.Mood.Trim(),
                    Reason = string.IsNullOrWhiteSpace(Input.MoodReason) ? null : Input.MoodReason.Trim()
                });
            }

            _context.LocalBeers.Add(e);
            await _context.SaveChangesAsync();

            Flash = "เพิ่มรายการใหม่เรียบร้อยแล้ว";
            return RedirectToPage("/Detail", new { id = e.Id });
        }
    }
}
