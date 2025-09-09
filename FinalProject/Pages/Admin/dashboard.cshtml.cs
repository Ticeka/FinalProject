using System;
using System.Linq;
using System.Threading.Tasks;
using FinalProject.Data;
using FinalProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FinalProject.Models; // LocalBeer, BeerComment, BeerFavorite, UserStats, QuickRating

namespace FinalProject.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _db;
        public DashboardModel(AppDbContext db) => _db = db;

        public DashboardVM VM { get; private set; } = new();

        public async Task OnGetAsync()
        {
            // ---------- METRICS (เรียกทีละตัว, ไม่ใช้ Task.WhenAll) ----------
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

            // ---------- TOP BEERS ----------
            VM.TopRatedBeers = await _db.LocalBeers
                .AsNoTracking()
                .OrderByDescending(b => b.AverageRating ?? (b.RatingCount > 0 ? b.Rating : 0))
                .ThenByDescending(b => b.RatingCount)
                .Select(b => new BeerMini
                {
                    Id = b.Id,
                    Name = b.Name,
                    Province = b.Province,
                    Avg = b.AverageRating ?? (b.RatingCount > 0 ? b.Rating : 0),
                    Count = b.RatingCount
                })
                .Take(8)
                .ToListAsync();

            var favs = await _db.Set<BeerFavorite>()
                .AsNoTracking()
                .GroupBy(f => f.LocalBeerId)
                .Select(g => new { BeerId = g.Key, Cnt = g.Count() })
                .OrderByDescending(x => x.Cnt)
                .Take(8)
                .ToListAsync();

            var ids = favs.Select(x => x.BeerId).ToList();

            var map = await _db.LocalBeers
                .AsNoTracking()
                .Where(b => ids.Contains(b.Id))
                .Select(b => new { b.Id, b.Name, b.Province })
                .ToDictionaryAsync(b => b.Id, b => b);

            VM.MostFavoritedBeers = favs.Select(x => new BeerMini
            {
                Id = x.BeerId,
                Name = map.TryGetValue(x.BeerId, out var b) ? b.Name : $"Beer #{x.BeerId}",
                Province = map.TryGetValue(x.BeerId, out var bb) ? bb.Province : null,
                Favorites = x.Cnt
            }).ToList();

            // ---------- RECENT COMMENTS (join เดียว, no subquery) ----------
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

            // ---------- CHART DATA ----------
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
    }
}
