using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using FinalProject.Data;

namespace FinalProject.Pages
{
    public class FlavorMatchModel : PageModel
    {
        private readonly AppDbContext _db;

        public FlavorMatchModel(AppDbContext db) => _db = db;

        public List<SelectListItem> Flavors { get; set; } = new();
        public List<SelectListItem> Foods { get; set; } = new();
        public List<SelectListItem> Moods { get; set; } = new();

        public void OnGet()
        {
            // --- FLAVORS ---
            var defaultFlavors = new[]
            {
                "Citrus","Herb","Sweet","Bitter","Smoke","Spice",
                "Malty","Hoppy","Fruity","Floral","Woody",
                // เผื่อชุดที่เจอในข้อมูลตัวอย่าง
                "Sparkling","Apple","Tropical Fruit","Smooth","Crisp","Light","Strong","Sweet Rice"
            };

            var dbFlavors = _db.LocalBeerFlavors
                               .Select(f => f.Flavor)
                               .Where(s => !string.IsNullOrWhiteSpace(s))
                               .ToList();

            Flavors = defaultFlavors
                .Union(dbFlavors, StringComparer.OrdinalIgnoreCase)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Select(s => new SelectListItem(s, s))
                .ToList();

            // --- FOODS ---
            var defaultFoods = new[]
            {
                "ลาบหมูคั่ว","หมูแดดเดียว","ชีสเค้ก","ซูชิ","สเต็กเนื้อริบอาย",
                "แกงมัสมั่น","หอยนางรมสด","เนื้อแดดเดียว","ส้มตำไทย",
                "ลาบเป็ด","พิซซ่า","ปีกไก่ทอด","ปลานึ่งมะนาว","ข้าวหมกไก่",
                "ยำทะเล","สลัดผลไม้","หมูย่าง","บาร์บีคิว","หมูกรอบ","อาหารเหนือ","อาหารทะเล"
            };

            var dbFoods = _db.LocalBeerFoodPairings
                             .Select(f => f.FoodName)
                             .Where(s => !string.IsNullOrWhiteSpace(s))
                             .ToList();

            Foods = defaultFoods
                .Union(dbFoods, StringComparer.OrdinalIgnoreCase)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Select(s => new SelectListItem(s, s))
                .ToList();

            // --- MOODS ---
            var defaultMoods = new[]
            {
                "Chill","Party","Celebration","Romantic","Tropical","Creative",
                "Serious","Refresh","Bold","Fun","Active","Light",
                "Adventure","Summer","Fresh","Classic","Beach","Casual"
            };

            var dbMoods = _db.LocalBeerMoodPairings
                             .Select(m => m.Mood)
                             .Where(s => !string.IsNullOrWhiteSpace(s))
                             .ToList();

            Moods = defaultMoods
                .Union(dbMoods, StringComparer.OrdinalIgnoreCase)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Select(s => new SelectListItem(s, s))
                .ToList();
        }
    }
}
