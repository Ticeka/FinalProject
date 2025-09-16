using System;
using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models
{
    public class BeerComment
    {
        public int Id { get; set; }

        [Required]
        public int LocalBeerId { get; set; }

        [Required, MaxLength(1000)]
        public string Body { get; set; } = string.Empty;

        // ผู้คอมเมนต์ (ถ้าล็อกอิน)
        public string? UserId { get; set; }
        public string? UserName { get; set; }

        // รองรับโหมดใส่ชื่อเล่น (เผื่อโค้ดเก่าอ้างถึง) – จะไม่ใช้เมื่อบังคับล็อกอินคอมเมนต์
        [MaxLength(100)]
        public string? DisplayName { get; set; }

        // meta
        [MaxLength(128)]
        public string? IpHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ตอบกลับ (thread)
        public int? ParentId { get; set; }

        // ลบแบบนิ่ม
        public bool IsDeleted { get; set; } = false;
    }
}
