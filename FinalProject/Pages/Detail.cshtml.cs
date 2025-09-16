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

            // ��Ŵ������Ẻ No-Tracking
            LocalBeer = await _context.LocalBeers
                .AsNoTracking()
                .Include(b => b.Flavors)
                .Include(b => b.FoodPairings)
                .Include(b => b.MoodPairings)
                .FirstOrDefaultAsync(b => b.Id == id.Value);

            if (LocalBeer == null) return NotFound();

            // �Ѻ View Ẻ atomic � DB
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE LocalBeers SET ViewCount = ViewCount + 1 WHERE Id = {id.Value};"
            );

            // �ѻവ��ҷ���ʴ�� UI
            LocalBeer.ViewCount = (LocalBeer.ViewCount < int.MaxValue)
                ? LocalBeer.ViewCount + 1
                : LocalBeer.ViewCount;

            // Normalize intensity �����ʴ���
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

        // ====== ź������ (੾���ʹ�Թ) ======
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteAsync(int? id)
        {
            if (id == null) return NotFound();

            // ��ͧ�ѹ�Է��������������
            if (!(User?.IsInRole("Admin") ?? false))
                return Forbid();

            var beer = await _context.LocalBeers
                .Include(b => b.Flavors)
                .Include(b => b.FoodPairings)
                .Include(b => b.MoodPairings)
                .FirstOrDefaultAsync(b => b.Id == id.Value);

            if (beer == null) return NotFound();

            // ź�١����դ�������ѹ�� (�ѹ FK �١���)
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
                TempData["Flash"] = "ź��¡�����º����";
                return RedirectToPage("/List"); // ����¹���·ҧ����˹����¡�âͧ�س
            }
            catch (DbUpdateException)
            {
                // ����� FK ���� (�� Favorites/Comments/Ratings) ���Ѵ��� cascade ���� soft delete ����ç���ҧ��ԧ
                ModelState.AddModelError(string.Empty, "ź�����������ͧ�ҡ��������ѹ��ͧ�����ŷ������Ǣ�ͧ");
                // ��Ŵ��Ѻ���ʴ�������繢�ͤ����Դ��Ҵ
                await OnGetAsync(id);
                return Page();
            }
        }
    }
}
