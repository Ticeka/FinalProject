using System;
using System.Collections.Generic;

namespace FinalProject.ViewModels
{
    // ====== View Models (Root) ======
    public class DashboardVM
    {
        // ===== KPI cards (รวม + ช่วงเวลา) =====
        public int TotalBeers { get; set; }
        public int TotalUsers { get; set; }
        public int TotalComments { get; set; }
        public int TotalFavorites { get; set; }
        public int TotalReviews { get; set; }            // sum(UserStats.Reviews)
        public int TotalUserComments { get; set; }       // sum(UserStats.Comments)
        public int TotalUserBadges { get; set; }         // sum(UserStats.Badges)
        public int RatingsCount { get; set; }            // count(QuickRating)
        public double RatingsAvg { get; set; }           // avg(QuickRating.Score)

        // รวมจำนวนการถูกดูทั้งหมดของเบียร์ (Sum(LocalBeer.ViewCount))
        public int BeerViewcount { get; set; }

        // Activity KPIs (เช่นช่วง 1/7/30 วันล่าสุด)
        public int ActiveUsers1d { get; set; }           // distinct(ActivityLog.UserId) last 1d
        public int ActiveUsers7d { get; set; }           // distinct(ActivityLog.UserId) last 7d
        public int RatingsLast7d { get; set; }
        public int CommentsLast7d { get; set; }
        public int FavoritesLast7d { get; set; }
        public int NewBeersLast30d { get; set; }         // count(LocalBeer.CreatedAt within 30d)

        // Data Quality / Catalog KPIs
        public int BeersWithImage { get; set; }
        public int BeersWithWebsite { get; set; }
        public int BeersWithPrice { get; set; }
        public int BeersWithGeo { get; set; }            // lat/long filled
        public double AvgPrice { get; set; }
        public double AvgAlcoholLevel { get; set; }

        // ===== Tables =====
        public List<BeerMini> TopRatedBeers { get; set; } = new();
        public List<BeerMini> MostFavoritedBeers { get; set; } = new();
        public List<BeerMini> MostViewedBeers { get; set; } = new();   // มี ViewCount ต่อแถว

        public List<BeerWithCounts> TopCommentedBeers { get; set; } = new();
        public List<UserMini> TopActiveUsers { get; set; } = new();    // จาก UserStats/Activity
        public List<RecentCommentVM> RecentComments { get; set; } = new();
        public List<RecentFavoriteVM> RecentFavorites { get; set; } = new();
        public List<ActivityItemVM> RecentActivities { get; set; } = new();

        // ===== Charts / Distributions =====
        public List<DailyRatingPoint> RatingsOverTime { get; set; } = new();
        public List<DailyCountPoint> CommentsOverTime { get; set; } = new();
        public List<DailyCountPoint> FavoritesOverTime { get; set; } = new();
        public List<DailyCountPoint> NewBeersOverTime { get; set; } = new();

        public List<ProvinceCount> BeersByProvince { get; set; } = new();
        public List<ProvinceMetric> RatingsByProvince { get; set; } = new();   // avg + count
        public List<ProvinceCount> FavoritesByProvince { get; set; } = new();

        public List<ScoreBucket> ScoreDistribution { get; set; } = new();      // 1..5
        public List<BucketCount> PriceDistribution { get; set; } = new();      // e.g. <100, 100–199, ...
        public List<BucketCount> AlcoholDistribution { get; set; } = new();    // e.g. <5%, 5–6.9%, ...

        // Pairing / Tag analytics
        public List<TagCount> TopFlavors { get; set; } = new();
        public List<TagCount> TopFoodPairings { get; set; } = new();
        public List<TagCount> TopMoodPairings { get; set; } = new();

        // Value-for-money scatter (ราคา vs เรตติ้งเฉลี่ย)
        public List<ValuePoint> RatingVsPrice { get; set; } = new();

        // Data quality summary (สรุป missings)
        public DataQualitySummary DataQuality { get; set; } = new();

        // ===== Pagination Info (สำหรับปุ่มก่อนหน้า/ถัดไป) =====
        public PageInfo TopRatedPage { get; set; } = PageInfo.Empty;
        public PageInfo MostFavPage { get; set; } = PageInfo.Empty;
        public PageInfo MostViewPage { get; set; } = PageInfo.Empty;
    }

    // ====== Small VMs / Records ======
    public class BeerMini
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Province { get; set; }
        public double Avg { get; set; }      // average rating
        public int Count { get; set; }       // rating count
        public int Favorites { get; set; }   // favorite count (ถ้ามี)
        public int ViewCount { get; set; }   // จำนวนวิวของเบียร์แต่ละรายการ
    }

    public class BeerWithCounts : BeerMini
    {
        public int Comments { get; set; }
        public int Ratings { get; set; }
    }

    public class UserMini
    {
        public string UserId { get; set; } = "";
        public string? DisplayName { get; set; }
        public int Reviews { get; set; }
        public int Comments { get; set; }
        public int Favorites { get; set; }
        public int Badges { get; set; }
    }

    public class RecentCommentVM
    {
        public int Id { get; set; }
        public int BeerId { get; set; }
        public string? BeerName { get; set; }
        public string Body { get; set; } = string.Empty;
        public string UserName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class RecentFavoriteVM
    {
        public int Id { get; set; }
        public int BeerId { get; set; }
        public string? BeerName { get; set; }
        public string UserName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class ActivityItemVM
    {
        public long Id { get; set; }
        public string Action { get; set; } = "";
        public string? UserId { get; set; }
        public string Message { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class DailyRatingPoint
    {
        public DateTime Day { get; set; }
        public int Count { get; set; }
        public double Avg { get; set; }
    }

    public class DailyCountPoint
    {
        public DateTime Day { get; set; }
        public int Count { get; set; }
    }

    public class ProvinceCount
    {
        public string? Province { get; set; }
        public int Count { get; set; }
    }

    public class ProvinceMetric
    {
        public string? Province { get; set; }
        public int Count { get; set; }
        public double Avg { get; set; }
    }

    public class ScoreBucket
    {
        public int Score { get; set; }   // 1..5
        public int Count { get; set; }
    }

    public class BucketCount
    {
        public string Bucket { get; set; } = "";
        public int Count { get; set; }
    }

    public class TagCount
    {
        public string Tag { get; set; } = "";
        public int Count { get; set; }
        public double? AvgIntensity { get; set; }
    }

    public class ValuePoint
    {
        public int BeerId { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public double AvgRating { get; set; }
    }

    public class DataQualitySummary
    {
        public int MissingImage { get; set; }
        public int MissingWebsite { get; set; }
        public int MissingPrice { get; set; }
        public int MissingGeo { get; set; }
        public int MissingDescription { get; set; }
    }

    // ===== PageInfo สำหรับควบคุมปุ่มก่อนหน้า/ถัดไป =====
    public readonly record struct PageInfo(
        int Page,
        int PageSize,
        int Total,
        bool HasPrev,
        bool HasNext,
        string? PrevUrl,
        string? NextUrl
    )
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
}
