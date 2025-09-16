using System;
using System.Linq;
using System.Threading.Tasks;
using FinalProject.Data;
using FinalProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;               // For [BindProperty]
using FinalProject.Models;                    // LocalBeer, BeerComment, BeerFavorite, UserStats, QuickRating

namespace FinalProject.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _db;
        public DashboardModel(AppDbContext db) => _db = db;

        // ===== Query (SupportsGet) สำหรับ pagination, หน้าเริ่มที่ 1 =====
        [BindProperty(SupportsGet = true)] public int trp { get; set; } = 1; // TopRated page
        [BindProperty(SupportsGet = true)] public int mfp { get; set; } = 1; // MostFavorited page
        [BindProperty(SupportsGet = true)] public int mvp { get; set; } = 1; // MostViewed page

        private const int PageSize = 8;

        public DashboardVM VM { get; private set; } = new();

        // ข้อมูลหน้า (Prev/Next) สำหรับแต่ละลิสต์
        public PageInfo TopRatedPage { get; private set; } = PageInfo.Empty;
        public PageInfo MostFavPage { get; private set; } = PageInfo.Empty;
        public PageInfo MostViewPage { get; private set; } = PageInfo.Empty;

        public async Task OnGetAsync()
        {
            // ---------- METRICS ----------
            VM.TotalBeers = await _db.LocalBeers.AsNoTracking().CountAsync();
            VM.TotalUsers = await _db.Users.AsNoTracking().CountAsync();
            VM.TotalComments = await _db.Set<BeerComment>().AsNoTracking().CountAsync(x => !x.IsDeleted);
            VM.TotalFavorites = await _db.Set<BeerFavorite>().AsNoTracking().CountAsync();

            var statsSum = await _db.Set<UserStats>()
                .AsNoTracking()
                .Select(s => new { s.Reviews, s.Comments, s.Badges })
                .ToListAsync();

            VM.TotalReviews = statsSum.Sum(x => x.Reviews);
            VM.TotalUserComments = statsSum.Sum(x => x.Comments);
            VM.TotalUserBadges = statsSum.Sum(x => x.Badges);

            var ratingAgg = await _db.Set<QuickRating>()
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(g => new { Avg = g.Average(x => (double)x.Score), Cnt = g.Count() })
                .FirstOrDefaultAsync();

            VM.RatingsCount = ratingAgg?.Cnt ?? 0;
            VM.RatingsAvg = ratingAgg?.Avg ?? 0;

            // ✅ รวมวิวทั้งหมดของเบียร์
            VM.BeerViewcount = await _db.LocalBeers.AsNoTracking().SumAsync(b => (int?)b.ViewCount ?? 0);

            // ---------- TOP RATED (มีหน้า) ----------
            var trBase = _db.LocalBeers
                .AsNoTracking()
                .OrderByDescending(b => b.AverageRating ?? (b.RatingCount > 0 ? b.Rating : 0))
                .ThenByDescending(b => b.RatingCount);

            var trTotal = await trBase.CountAsync();
            trp = Math.Max(1, trp);
            var trSkip = (trp - 1) * PageSize;

            VM.TopRatedBeers = await trBase
                .Skip(trSkip)
                .Take(PageSize)
                .Select(b => new BeerMini
                {
                    Id = b.Id,
                    Name = b.Name,
                    Province = b.Province,
                    Avg = b.AverageRating ?? (b.RatingCount > 0 ? b.Rating : 0),
                    Count = b.RatingCount
                })
                .ToListAsync();

            TopRatedPage = PageInfo.Create(trp, PageSize, trTotal,
                prevUrl: BuildUrl(trp - 1, mfp, mvp),
                nextUrl: BuildUrl(trp + 1, mfp, mvp));

            // ---------- MOST FAVORITED (มีหน้า) ----------
            var favAgg = _db.Set<BeerFavorite>()
                .AsNoTracking()
                .GroupBy(f => f.LocalBeerId)
                .Select(g => new { BeerId = g.Key, Cnt = g.Count() })
                .OrderByDescending(x => x.Cnt)
                .ThenBy(x => x.BeerId);

            var mfTotal = await favAgg.CountAsync();
            mfp = Math.Max(1, mfp);
            var mfSkip = (mfp - 1) * PageSize;

            var favPage = await favAgg.Skip(mfSkip).Take(PageSize).ToListAsync();
            var ids = favPage.Select(x => x.BeerId).ToList();

            var map = await _db.LocalBeers
                .AsNoTracking()
                .Where(b => ids.Contains(b.Id))
                .Select(b => new { b.Id, b.Name, b.Province })
                .ToDictionaryAsync(b => b.Id, b => b);

            VM.MostFavoritedBeers = favPage.Select(x => new BeerMini
            {
                Id = x.BeerId,
                Name = map.TryGetValue(x.BeerId, out var b) ? b.Name : $"Beer #{x.BeerId}",
                Province = map.TryGetValue(x.BeerId, out var bb) ? bb.Province : null,
                Favorites = x.Cnt
            }).ToList();

            MostFavPage = PageInfo.Create(mfp, PageSize, mfTotal,
                prevUrl: BuildUrl(trp, mfp - 1, mvp),
                nextUrl: BuildUrl(trp, mfp + 1, mvp));

            // ---------- MOST VIEWED (มีหน้า) ----------
            var mvBase = _db.LocalBeers
                .AsNoTracking()
                .OrderByDescending(b => b.ViewCount)
                .ThenBy(b => b.Name);

            var mvTotal = await mvBase.CountAsync();
            mvp = Math.Max(1, mvp);
            var mvSkip = (mvp - 1) * PageSize;

            VM.MostViewedBeers = await mvBase
                .Skip(mvSkip)
                .Take(PageSize)
                .Select(b => new BeerMini
                {
                    Id = b.Id,
                    Name = b.Name,
                    Province = b.Province,
                    Avg = b.AverageRating ?? (b.RatingCount > 0 ? b.Rating : 0),
                    Count = b.RatingCount,
                    ViewCount = b.ViewCount
                })
                .ToListAsync();

            MostViewPage = PageInfo.Create(mvp, PageSize, mvTotal,
                prevUrl: BuildUrl(trp, mfp, mvp - 1),
                nextUrl: BuildUrl(trp, mfp, mvp + 1));

            // ---------- RECENT COMMENTS ----------
            VM.RecentComments = await (
                from c in _db.Set<BeerComment>().AsNoTracking()
                join b in _db.LocalBeers.AsNoTracking() on c.LocalBeerId equals b.Id
                where !c.IsDeleted
                orderby c.CreatedAt descending
                select new RecentCommentVM
                {
                    Id = c.Id,
                    BeerId = b.Id,
                    BeerName = b.Name,
                    Body = c.Body,
                    UserName = c.UserName ?? c.DisplayName ?? "Guest",
                    CreatedAt = c.CreatedAt
                })
                .Take(10)
                .ToListAsync();

            // ---------- CHART DATA (เท่าเดิม) ----------
            var start = DateTime.Today.AddDays(-29);

            VM.RatingsOverTime = await _db.Set<QuickRating>()
                .AsNoTracking()
                .Where(r => r.CreatedAt >= start)
                .GroupBy(r => r.CreatedAt.Date)
                .Select(g => new DailyRatingPoint
                {
                    Day = g.Key,
                    Count = g.Count(),
                    Avg = g.Average(x => (double)x.Score)
                })
                .OrderBy(x => x.Day)
                .ToListAsync();

            VM.BeersByProvince = await _db.LocalBeers
                .AsNoTracking()
                .GroupBy(b => b.Province)
                .Select(g => new ProvinceCount { Province = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(12)
                .ToListAsync();
        }

        // ===== Helpers =====
        public readonly record struct PageInfo(int Page, int PageSize, int Total, bool HasPrev, bool HasNext, string? PrevUrl, string? NextUrl)
        {
            public static PageInfo Empty => new(1, 8, 0, false, false, null, null);
            public static PageInfo Create(int page, int size, int total, string? prevUrl, string? nextUrl)
            {
                var maxPage = Math.Max(1, (int)Math.Ceiling(total / (double)size));
                page = Math.Clamp(page, 1, maxPage);
                return new PageInfo(
                    page, size, total,
                    HasPrev: page > 1,
                    HasNext: page < maxPage,
                    PrevUrl: page > 1 ? prevUrl : null,
                    NextUrl: page < maxPage ? nextUrl : null
                );
            }
        }

        private string BuildUrl(int trPage, int mfPage, int mvPage)
        {
            int clamp(int v) => Math.Max(1, v);
            trPage = clamp(trPage); mfPage = clamp(mfPage); mvPage = clamp(mvPage);
            // สร้างลิงก์ไปหน้าเดิมพร้อมอัปเดต query ของแต่ละลิสต์
            var path = HttpContext?.Request?.Path.Value ?? "/Admin/Dashboard";
            return $"{path}?trp={trPage}&mfp={mfPage}&mvp={mvPage}#lists";
        }
    }
}
