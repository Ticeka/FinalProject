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
            // defaults — server-render fallback (JS จะ override ด้วย API อีกชั้น)
            var defaultFlavors = new[] { "หวาน", "เปรี้ยว", "ขม", "เค็ม", "อูมามิ" };
            var defaultFoods = new[] { "ทะเล", "เนื้อ", "ไก่", "หมู" };
            var defaultMoods = new[] { "Party", "Chill", "Celebration", "Fresh", "Sport" };

            Flavors = defaultFlavors
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Select(s => new SelectListItem(s, s))
                .ToList();

            Foods = defaultFoods
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Select(s => new SelectListItem(s, s))
                .ToList();

            Moods = defaultMoods
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Select(s => new SelectListItem(s, s))
                .ToList();
        }
    }
}
