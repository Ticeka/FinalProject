using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinalProject.Pages
{
    public class FlavorMatchModel : PageModel
    {
        public List<SelectListItem> Flavors { get; set; } = new()
        {
            new("Citrus","Citrus"),
            new("Herb","Herb"),
            new("Sweet","Sweet"),
            new("Bitter","Bitter"),
            new("Smoke","Smoke"),
            new("Spice","Spice"),
            new("Malty","Malty"),
            new("Hoppy","Hoppy"),
            new("Fruity","Fruity"),
            new("Floral","Floral"),
            new("Woody","Woody"),
        };

        public void OnGet() { }
    }
}
