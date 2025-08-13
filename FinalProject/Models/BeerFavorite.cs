using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    public class BeerFavorite
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = default!;

        [Required]
        public int LocalBeerId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // (optional) Navigation
        public virtual ApplicationUser? User { get; set; }
        public virtual LocalBeer? LocalBeer { get; set; }
    }
}
