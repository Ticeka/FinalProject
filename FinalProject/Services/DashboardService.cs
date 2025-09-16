// ปรับ namespace ให้ตรงโปรเจกต์ของคุณ
using System.Linq;
using System.Threading.Tasks;
using FinalProject.Data;
using FinalProject.ViewModels;
using Microsoft.EntityFrameworkCore;

public interface IDashboardService
{
    Task<DashboardVM> BuildAsync(int topN = 10);
}

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db; // เปลี่ยนเป็น DbContext ของคุณ

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardVM> BuildAsync(int topN = 10)
    {
        var vm = new DashboardVM();

        // ===== ตัวอย่าง KPI อื่น ๆ (ถ้ามีตารางตามโปรเจกต์คุณ) =====
        // vm.TotalBeers = await _db.LocalBeers.CountAsync();
        // vm.TotalUsers = await _db.Users.CountAsync();
        // vm.TotalComments = await _db.Comments.CountAsync();
        // vm.TotalFavorites = await _db.Favorites.CountAsync();
        // vm.TotalReviews = await _db.Reviews.CountAsync();
        // vm.RatingsCount = await _db.LocalBeers.SumAsync(b => (int?)b.RatingCount ?? 0);
        // vm.RatingsAvg = await _db.LocalBeers.AnyAsync()
        //     ? await _db.LocalBeers.AverageAsync(b => (double?)b.AverageRating ?? 0.0)
        //     : 0.0;

        // ===== รวมวิวทั้งหมดของเบียร์ =====
        vm.BeerViewcount = await _db.LocalBeers.SumAsync(b => (int?)b.ViewCount ?? 0);

        // ===== เบียร์ถูกดูมากสุด N อันดับ =====
        vm.MostViewedBeers = await _db.LocalBeers
            .OrderByDescending(b => b.ViewCount)
            .ThenBy(b => b.Name) // ผูกลำดับให้เสถียร
            .Take(topN)
            .Select(b => new BeerMini
            {
                Id = b.Id,
                Name = b.Name,
                Province = b.Province,
                Avg = (double?)(b.AverageRating) ?? 0.0, // ถ้าไม่มีฟิลด์นี้ในโมเดล ให้ลบ/แก้ได้
                Count = (int?)(b.RatingCount) ?? 0,      // ถ้าไม่มีฟิลด์นี้ในโมเดล ให้ลบ/แก้ได้
                Favorites = 0,                           // ถ้าคุณมีตาราง Favorites ค่อย Map จริง
                ViewCount = b.ViewCount
            })
            .ToListAsync();

        return vm;
    }
}
