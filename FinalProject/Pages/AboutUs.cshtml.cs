using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FinalProject.Pages
{
    public class AboutModel : PageModel
    {
        private readonly AppDbContext? _db;

        public AboutModel(AppDbContext? db = null)
        {
            _db = db;
        }

        public int StatBreweries { get; private set; } = 0;
        public int StatDrinks { get; private set; } = 0;
        public int StatCities { get; private set; } = 0;
        public int StatReviews { get; private set; } = 0;

        public async Task OnGet()
        {
            if (_db == null)
            {
                // Fallback (no DB resolved). Keep zeros so UI still renders.
                return;
            }

            try
            {
                var beers = _db.LocalBeers.AsNoTracking();

                StatDrinks = await beers.CountAsync();

                // Distinct provinces / areas (ignore null/empty)
                StatCities = await beers
                    .Select(b => b.Province)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct()
                    .CountAsync();

                // Distinct creators/brands (fallback to distributor if creator is empty)
                StatBreweries = await beers
                    .Select(b => string.IsNullOrWhiteSpace(b.Creator) ? b.Distributor : b.Creator)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct()
                    .CountAsync();

                // Sum of rating counts (null -> 0)
                StatReviews = (await beers.Select(b => (int?)b.RatingCount).SumAsync()) ?? 0;
            }
            catch
            {
                // Swallow to avoid breaking About page if schema changes; UI will show zeros.
            }
        }
    }
}
