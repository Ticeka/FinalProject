using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinalProject.Pages
{
    [IgnoreAntiforgeryToken] // ถ้าคุณยังไม่ใส่ token จาก JS
    public class MixTripModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int Round { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int Score { get; set; } = 0;

        public List<IngredientVm> Ingredients { get; set; } = new();

        public void OnGet() => Ingredients = GetRandomIngredients();

        public JsonResult OnGetShuffle() => new JsonResult(GetRandomIngredients());

        // อ่าน body เองเพื่อให้แน่ใจว่าได้ค่าจริงแม้ model binding จะไม่ทำงาน
        public async Task<IActionResult> OnPostMix()
        {
            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync();
            List<string>? ids = null;
            try
            {
                ids = JsonSerializer.Deserialize<List<string>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch { /* ignore parse errors */ }

            if (ids is null || ids.Count < 3)
                return BadRequest("need 3 ids");

            var set = ids.Select(x => x.ToLowerInvariant()).ToHashSet();
            var okCombo = set.Contains("citrus") && set.Contains("herb") && set.Contains("fizz");

            if (okCombo)
            {
                Score += 10;
                Round += 1;
                return new JsonResult(new { ok = true, message = "เยี่ยม! ส่วนผสมเข้ากันสุด ๆ +10 คะแนน", score = Score });
            }
            else
            {
                Score = Math.Max(0, Score - 2);
                Round += 1;
                return new JsonResult(new { ok = false, message = "ยังไม่ค่อยเข้ากัน ลองสลับดูอีกหน่อย (-2)", score = Score });
            }
        }

        // ===== Helpers =====
        private static readonly IngredientVm[] Pool = new[]
        {
            new IngredientVm("citrus","🍋","Citrus"),
            new IngredientVm("herb","🌿","Herb"),
            new IngredientVm("fizz","✨","Fizz"),
            new IngredientVm("sweet","🍯","Sweet"),
            new IngredientVm("bitter","🍂","Bitter"),
            new IngredientVm("smoke","💨","Smoke"),
            new IngredientVm("spice","🌶️","Spice"),
            new IngredientVm("tropical","🥭","Tropical"),
        };

        private static List<IngredientVm> GetRandomIngredients()
        {
            var rnd = new Random();
            return Pool.OrderBy(_ => rnd.Next()).Take(6).ToList();
        }

        public record IngredientVm(string Id, string Emoji, string Name);
    }
}