using System;
using System.Collections.Generic;

namespace FinalProject.ViewModels
{
    // ====== View Models ======
    public class DashboardVM
    {
        // KPI cards
        public int TotalBeers { get; set; }
        public int TotalUsers { get; set; }
        public int TotalComments { get; set; }
        public int TotalFavorites { get; set; }
        public int TotalReviews { get; set; }           // sum(UserStats.Reviews)
        public int TotalUserComments { get; set; }      // sum(UserStats.Comments)
        public int TotalUserBadges { get; set; }        // sum(UserStats.Badges)
        public int RatingsCount { get; set; }           // count(QuickRating)
        public double RatingsAvg { get; set; }          // avg(QuickRating.Score)

        // Tables
        public List<BeerMini> TopRatedBeers { get; set; } = new();
        public List<BeerMini> MostFavoritedBeers { get; set; } = new();
        public List<RecentCommentVM> RecentComments { get; set; } = new();

        // Charts
        public List<DailyRatingPoint> RatingsOverTime { get; set; } = new();
        public List<ProvinceCount> BeersByProvince { get; set; } = new();
    }

    public class BeerMini
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Province { get; set; }
        public double Avg { get; set; }
        public int Count { get; set; }
        public int Favorites { get; set; }
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

    public class DailyRatingPoint
    {
        public DateTime Day { get; set; }
        public int Count { get; set; }
        public double Avg { get; set; }
    }

    public class ProvinceCount
    {
        public string? Province { get; set; }
        public int Count { get; set; }
    }
}
