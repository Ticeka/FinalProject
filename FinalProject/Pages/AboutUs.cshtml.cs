using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinalProject.Pages
{
    public class AboutModel : PageModel
    {
        // ตัวเลขสถิติ — ใส่ค่าจริงจาก DB ได้ภายหลัง
        public int StatBreweries { get; private set; } = 128;
        public int StatDrinks { get; private set; } = 764;
        public int StatCities { get; private set; } = 47;
        public int StatReviews { get; private set; } = 5230;

        public void OnGet()
        {
            // TODO: ดึงค่าจริงจาก AppDbContext แล้วเซ็ตให้ props
            // ตัวอย่าง:
            // using var db = ... (ถ้าจะ DI ผ่าน ctor ก็ได้)
            // StatDrinks = db.LocalBeers.Count();
        }
    }
}
