using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;
using FinalProject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace FinalProject.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public List<LocalBeer> LocalBeers { get; set; } = new List<LocalBeer>();

        public async Task OnGetAsync()
        {
            LocalBeers = await _context.LocalBeers.ToListAsync();
        }
    }
}
