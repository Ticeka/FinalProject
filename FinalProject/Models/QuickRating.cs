using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    public class QuickRating
    {
        public int Id { get; set; }

        [Required]
        public int LocalBeerId { get; set; }

        [Range(1, 5)]
        public int Score { get; set; }

        [MaxLength(64)]
        public string? IpHash { get; set; }

        [MaxLength(128)]
        public string? Fingerprint { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ✅ เพิ่ม navigation property
        [ForeignKey(nameof(LocalBeerId))]
        public LocalBeer? LocalBeer { get; set; }
    }
}
