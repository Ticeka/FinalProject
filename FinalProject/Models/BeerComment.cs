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

        [MaxLength(100)]
        public string? DisplayName { get; set; }   // ชื่อโชว์ (ถ้าไม่ล็อกอิน)

        public string? UserId { get; set; }       // ถ้าล็อกอินจะเก็บ
        [MaxLength(100)]
        public string? UserName { get; set; }     // ชื่อผู้ใช้ตอนคอมเมนต์

        [MaxLength(128)]
        public string? IpHash { get; set; }       // ไว้วิเคราะห์/กันสแปมภายหลัง

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
    }
}
