using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    public class QuickRating
    {
        public int Id { get; set; }

        [Required] public int LocalBeerId { get; set; }
        [Range(1, 5)] public int Score { get; set; }

        [Required, MaxLength(64)] public string DeviceId { get; set; } = default!;
        [Required, MaxLength(128)] public string IpHash { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(LocalBeerId))]
        public LocalBeer? LocalBeer { get; set; }
    }
}
