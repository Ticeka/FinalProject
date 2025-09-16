using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;
using FinalProject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var beer = await _context.LocalBeers.FindAsync(id);
            if (beer == null) return NotFound();

            _context.LocalBeers.Remove(beer);
            await _context.SaveChangesAsync();

            return RedirectToPage(); // กลับมาหน้า List เดิม
        }
    }
}
