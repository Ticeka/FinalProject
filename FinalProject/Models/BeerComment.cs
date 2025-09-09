using System;
using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models
{
    public class BeerComment
    {
        public int Id { get; set; }

        [Required]
        public int LocalBeerId { get; set; }      // ← ใช้ชื่อนี้ตามสคีมาของคุณ

        [Required, MaxLength(1000)]
        public string Body { get; set; } = string.Empty;

        // ชื่อโชว์ของ Guest (ถ้าล็อกอินจะไม่ใช้ฟิลด์นี้)
        [MaxLength(100)]
        public string? DisplayName { get; set; }

        // ข้อมูลผู้ใช้ที่ล็อกอิน
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        [MaxLength(100)]
        public string? UserName { get; set; }

        [MaxLength(128)]
        public string? IpHash { get; set; }

        // ถ้าต้องการผูกคะแนนของผู้คอมเมนต์กับคอมเมนต์เลย (แสดงใต้ชื่อ)
        [Range(1, 5)]
        public int? UserRating { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;

    }
}
