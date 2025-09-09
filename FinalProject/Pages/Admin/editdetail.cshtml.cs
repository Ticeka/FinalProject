using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using FinalProject.Data;
using FinalProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Pages.Admin.Beers
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        public EditModel(AppDbContext context) => _context = context;

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        [TempData] public string? Flash { get; set; }

        // แสดงผล read-only
        public string CreatedAtDisplay { get; set; } = "-";
        public string UpdatedAtDisplay { get; set; } = "-";

        public class InputModel
        {
            public int Id { get; set; }

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

            public double? AverageRating { get; set; }

            // Pairings อย่างละ 1
            public string? Flavor { get; set; }
            public double? FlavorIntensity { get; set; }
            public string? FoodName { get; set; }
            public string? FoodReason { get; set; }
            public string? Mood { get; set; }
            public string? MoodReason { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (Id == 0) return NotFound();

            var e = await _context.LocalBeers
                .Include(x => x.Flavors)
                .Include(x => x.FoodPairings)
                .Include(x => x.MoodPairings)
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (e is null) return NotFound();

            var f = e.Flavors.FirstOrDefault();
            var food = e.FoodPairings.FirstOrDefault();
            var mood = e.MoodPairings.FirstOrDefault();

            Input = new InputModel
            {
                Id = e.Id,
                Name = e.Name,
                Province = e.Province,
                Description = e.Description,
                District = e.District,
                Type = e.Type,
                Address = e.Address,
                Latitude = e.Latitude,
                Longitude = e.Longitude,
                ImageUrl = e.ImageUrl,
                Website = e.Website,
                FacebookPage = e.FacebookPage,
                PhoneNumber = e.PhoneNumber,
                OpenHours = e.OpenHours,
                AlcoholLevel = e.AlcoholLevel,
                Price = e.Price,
                PlaceOfOrigin = e.PlaceOfOrigin,
                Region = e.Region,
                Creator = e.Creator,
                Volume = e.Volume,
                MainIngredients = e.MainIngredients,
                ProductMethod = e.ProductMethod,
                ProductYear = e.ProductYear,
                Rights = e.Rights,
                Distributor = e.Distributor,
                DistributorChanel = e.DistributorChanel,
                Award = e.Award,
                Notes = e.Notes,
                ProductId = e.ProductId,
                TypeOfLiquor = e.TypeOfLiquor,
                AverageRating = e.AverageRating,

                Flavor = f?.Flavor,
                FlavorIntensity = f?.Intensity,
                FoodName = food?.FoodName,
                FoodReason = food?.Reason,
                Mood = mood?.Mood,
                MoodReason = mood?.Reason
            };

            CreatedAtDisplay = e.CreatedAt.ToString("yyyy-MM-dd HH:mm");
            UpdatedAtDisplay = (e.UpdatedAt?.ToString("yyyy-MM-dd HH:mm")) ?? "-";

            return Page();
        }

        // เติม https:// ให้อัตโนมัติถ้าไม่มี scheme
        // และอนุญาต path/ไฟล์สำหรับรูป
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
            // โหลด entity มาก่อน เพื่อจะได้กรอก read-only values ให้กลับเวลาฟอร์ม invalid
            var e = await _context.LocalBeers
                .Include(x => x.Flavors)
                .Include(x => x.FoodPairings)
                .Include(x => x.MoodPairings)
                .FirstOrDefaultAsync(x => x.Id == Input.Id);

            if (e is null) return NotFound();

            if (!ModelState.IsValid)
            {
                CreatedAtDisplay = e.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                UpdatedAtDisplay = (e.UpdatedAt?.ToString("yyyy-MM-dd HH:mm")) ?? "-";
                return Page();
            }

            // ---- map ฟิลด์หลัก ----
            e.Name = Input.Name?.Trim()!;
            e.Province = Input.Province?.Trim()!;
            e.Description = Input.Description?.Trim();
            e.District = Input.District?.Trim();
            e.Type = Input.Type?.Trim();
            e.Address = Input.Address?.Trim();
            e.Latitude = Input.Latitude;
            e.Longitude = Input.Longitude;

            // URL / Path / Filename
            e.ImageUrl = NormalizeUrl(Input.ImageUrl, allowRelativeOrFilename: true);
            e.Website = NormalizeUrl(Input.Website);
            e.FacebookPage = NormalizeUrl(Input.FacebookPage);

            e.PhoneNumber = string.IsNullOrWhiteSpace(Input.PhoneNumber) ? null : Input.PhoneNumber.Trim();
            e.OpenHours = string.IsNullOrWhiteSpace(Input.OpenHours) ? null : Input.OpenHours.Trim();

            e.AlcoholLevel = Input.AlcoholLevel;
            e.Price = Input.Price;
            e.PlaceOfOrigin = string.IsNullOrWhiteSpace(Input.PlaceOfOrigin) ? null : Input.PlaceOfOrigin.Trim();
            e.Region = string.IsNullOrWhiteSpace(Input.Region) ? null : Input.Region.Trim();
            e.Creator = string.IsNullOrWhiteSpace(Input.Creator) ? null : Input.Creator.Trim();
            e.Volume = Input.Volume;
            e.MainIngredients = string.IsNullOrWhiteSpace(Input.MainIngredients) ? null : Input.MainIngredients.Trim();
            e.ProductMethod = string.IsNullOrWhiteSpace(Input.ProductMethod) ? null : Input.ProductMethod.Trim();
            e.ProductYear = Input.ProductYear;
            e.Rights = string.IsNullOrWhiteSpace(Input.Rights) ? null : Input.Rights.Trim();
            e.Distributor = string.IsNullOrWhiteSpace(Input.Distributor) ? null : Input.Distributor.Trim();
            e.DistributorChanel = string.IsNullOrWhiteSpace(Input.DistributorChanel) ? null : Input.DistributorChanel.Trim();
            e.Award = string.IsNullOrWhiteSpace(Input.Award) ? null : Input.Award.Trim();
            e.Notes = string.IsNullOrWhiteSpace(Input.Notes) ? null : Input.Notes.Trim();
            e.ProductId = string.IsNullOrWhiteSpace(Input.ProductId) ? null : Input.ProductId.Trim();
            e.TypeOfLiquor = string.IsNullOrWhiteSpace(Input.TypeOfLiquor) ? null : Input.TypeOfLiquor.Trim();
            e.AverageRating = Input.AverageRating; // ปิดได้ถ้าไม่ให้แก้
            e.UpdatedAt = DateTime.Now;

            // ---- Pairings: อย่างละ 1 ----
            foreach (var x in e.Flavors.ToList()) _context.Remove(x);
            if (!string.IsNullOrWhiteSpace(Input.Flavor))
                e.Flavors.Add(new LocalBeerFlavor
                {
                    LocalBeerId = e.Id,
                    Flavor = Input.Flavor.Trim(),
                    Intensity = Input.FlavorIntensity ?? 0
                });

            foreach (var x in e.FoodPairings.ToList()) _context.Remove(x);
            if (!string.IsNullOrWhiteSpace(Input.FoodName))
                e.FoodPairings.Add(new LocalBeerFoodPairing
                {
                    LocalBeerId = e.Id,
                    FoodName = Input.FoodName.Trim(),
                    Reason = string.IsNullOrWhiteSpace(Input.FoodReason) ? null : Input.FoodReason.Trim()
                });

            foreach (var x in e.MoodPairings.ToList()) _context.Remove(x);
            if (!string.IsNullOrWhiteSpace(Input.Mood))
                e.MoodPairings.Add(new LocalBeerMoodPairing
                {
                    LocalBeerId = e.Id,
                    Mood = Input.Mood.Trim(),
                    Reason = string.IsNullOrWhiteSpace(Input.MoodReason) ? null : Input.MoodReason.Trim()
                });

            await _context.SaveChangesAsync();

            Flash = "บันทึกเรียบร้อยแล้ว";
            return RedirectToPage("/Detail", new { id = e.Id });
        }
    }
}
