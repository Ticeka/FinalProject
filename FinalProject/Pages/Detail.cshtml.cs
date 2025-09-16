using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;
using FinalProject.Models;
using System.Linq;
using System.Threading.Tasks;

namespace FinalProject.Pages
{
    public class DetailModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailModel(AppDbContext context)
        {
            _context = context;
        }

        public LocalBeer? LocalBeer { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            // โหลดข้อมูลแบบ No-Tracking
            LocalBeer = await _context.LocalBeers
                .AsNoTracking()
                .Include(b => b.Flavors)
                .Include(b => b.FoodPairings)
                .Include(b => b.MoodPairings)
                .FirstOrDefaultAsync(b => b.Id == id.Value);

            if (LocalBeer == null) return NotFound();

            // นับ View แบบ atomic ใน DB
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE LocalBeers SET ViewCount = ViewCount + 1 WHERE Id = {id.Value};"
            );

            // อัปเดตค่าที่แสดงใน UI
            LocalBeer.ViewCount = (LocalBeer.ViewCount < int.MaxValue)
                ? LocalBeer.ViewCount + 1
                : LocalBeer.ViewCount;

            // Normalize intensity เพื่อแสดงผล
            if (LocalBeer.Flavors != null)
            {
                foreach (var f in LocalBeer.Flavors)
                {
                    if (double.IsNaN(f.Intensity)) f.Intensity = 0;
                    if (f.Intensity < 0) f.Intensity = 0;
                    if (f.Intensity > 100) f.Intensity = 100;
                }
            }

            return Page();
        }

        // ====== ลบข้อมูล (เฉพาะแอดมิน) ======
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteAsync(int? id)
        {
            if (id == null) return NotFound();

            // ป้องกันสิทธิ์ฝั่งเซิร์ฟเวอร์
            if (!(User?.IsInRole("Admin") ?? false))
                return Forbid();

            var beer = await _context.LocalBeers
                .Include(b => b.Flavors)
                .Include(b => b.FoodPairings)
                .Include(b => b.MoodPairings)
                .FirstOrDefaultAsync(b => b.Id == id.Value);

            if (beer == null) return NotFound();

            // ลบลูกที่มีความสัมพันธ์ (กัน FK ผูกไว้)
            if (beer.Flavors != null && beer.Flavors.Count > 0)
                _context.RemoveRange(beer.Flavors);
            if (beer.FoodPairings != null && beer.FoodPairings.Count > 0)
                _context.RemoveRange(beer.FoodPairings);
            if (beer.MoodPairings != null && beer.MoodPairings.Count > 0)
                _context.RemoveRange(beer.MoodPairings);

            _context.LocalBeers.Remove(beer);

            try
            {
                await _context.SaveChangesAsync();
                TempData["Flash"] = "ลบรายการเรียบร้อย";
                return RedirectToPage("/List"); // เปลี่ยนปลายทางได้ตามหน้ารายการของคุณ
            }
            catch (DbUpdateException)
            {
                // ถ้ามี FK อื่นๆ (เช่น Favorites/Comments/Ratings) ให้จัดการ cascade หรือ soft delete ตามโครงสร้างจริง
                ModelState.AddModelError(string.Empty, "ลบไม่สำเร็จเนื่องจากความสัมพันธ์ของข้อมูลที่เกี่ยวข้อง");
                // โหลดกลับมาแสดงเพื่อเห็นข้อความผิดพลาด
                await OnGetAsync(id);
                return Page();
            }
        }
    }
}
