using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models
{
    // เพิ่มฟิลด์ได้ตามต้องการ เช่น DisplayName
    public class ApplicationUser : IdentityUser
    {
        // ชื่อที่แสดงในเว็บ
        [MaxLength(100)]
        public string? DisplayName { get; set; }

        // เมือง/จังหวัด (แสดงในโปรไฟล์)
        [MaxLength(120)]
        public string? Location { get; set; }

        // คำอธิบายสั้น ๆ เกี่ยวกับตัวเอง
        [MaxLength(500)]
        public string? Bio { get; set; }

        // URL รูปโปรไฟล์ (เก็บเป็น path ใน wwwroot หรือ URL S3/Blob)
        [MaxLength(512)]
        public string? AvatarUrl { get; set; }

        // วันเกิด (เก็บเป็นปีพอเพื่อความเป็นส่วนตัว)
        public int? BirthYear { get; set; }

        // เวลาอัปเดตโปรไฟล์ล่าสุด (ใช้ทำ cache-busting ให้รูป)
        public DateTime? ProfileUpdatedAt { get; set; }

        public virtual UserStats? Stats { get; set; }
    }
}
