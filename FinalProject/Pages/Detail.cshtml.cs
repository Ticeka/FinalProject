using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;
using FinalProject.Models;
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

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public LocalBeer LocalBeer { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (Id == 0)
            {
                return NotFound();
            }

            LocalBeer = await _context.LocalBeers.FirstOrDefaultAsync(b => b.Id == Id);

            if (LocalBeer == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}
