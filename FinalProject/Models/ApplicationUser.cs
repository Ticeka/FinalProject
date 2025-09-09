using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models
{
    // ผู้ใช้ของระบบ (สืบทอดจาก IdentityUser)
    public class ApplicationUser : IdentityUser
    {
        // ===== โปรไฟล์พื้นฐาน =====
        [MaxLength(100)]
        public string? DisplayName { get; set; }

        [MaxLength(120)]
        public string? Location { get; set; }

        [MaxLength(500)]
        public string? Bio { get; set; }

        [MaxLength(512)]
        public string? AvatarUrl { get; set; }

        public int? BirthYear { get; set; }

        // ใช้สำหรับ cache-busting รูป/โปรไฟล์
        public DateTime? ProfileUpdatedAt { get; set; }

        // ===== ความสัมพันธ์สถิติแบบ 1-ต่อ-1 =====
        public virtual UserStats? Stats { get; set; }

        // ===== Navigation Collections (ให้ตรงกับ OnModelCreating) =====
        // คอมเมนต์ที่ผู้ใช้นี้เขียน
        public virtual ICollection<BeerComment> Comments { get; set; } = new List<BeerComment>();
        public virtual ICollection<QuickRating> Ratings { get; set; } = new List<QuickRating>();
        public virtual ICollection<BeerFavorite> Favorites { get; set; } = new List<BeerFavorite>();

    }
}
