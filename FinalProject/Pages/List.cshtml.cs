using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;
using FinalProject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinalProject.Pages
{
    public class ListModel : PageModel
    {
        private readonly AppDbContext _context;

        public ListModel(AppDbContext context)
        {
            _context = context;
        }

        public List<LocalBeer> LocalBeers { get; set; }

        public async Task OnGetAsync()
        {
            LocalBeers = await _context.LocalBeers.ToListAsync();
        }
    }
}
