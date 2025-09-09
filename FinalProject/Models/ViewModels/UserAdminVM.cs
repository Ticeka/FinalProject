using System;
using System.Collections.Generic;

namespace FinalProject.ViewModels
{
    /// <summary>
    /// 1 แถวในตาราง Users (Admin)
    /// </summary>
    public class UserRowVM
    {
        public string Id { get; set; } = default!;
        public string? DisplayName { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string? PhoneNumber { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }

        // ===== Stats =====
        public int Reviews { get; set; }
        public int Comments { get; set; }
        public int Favorites { get; set; }
        public int Badges { get; set; }

        public List<string> Roles { get; set; } = new();

        // ===== Fields เผื่ออนาคต/การเรียง-กรองเพิ่มเติม =====
        /// <summary>วันที่สร้างยูส (ถ้ามี map จาก AspNetUsers.CreatedAt)</summary>
        public DateTime? CreatedAtUtc { get; set; }

        /// <summary>ล็อกอินล่าสุด (ถ้าคุณเก็บในตาราง audit/claims)</summary>
        public DateTime? LastLoginUtc { get; set; }

        /// <summary>มีรูปโปรไฟล์ไหม (ถ้าหน้า admin จะโชว์ avatar เล็กๆ)</summary>
        public bool HasAvatar { get; set; }

        // ===== Computed helpers (ใช้ใน View ได้สะดวก) =====
        /// <summary>สถานะล็อกตอนนี้</summary>
        public bool IsLocked => LockoutEnd != null && LockoutEnd > DateTimeOffset.UtcNow;

        /// <summary>ชื่อที่ใช้แสดงในตาราง (DisplayName > UserName > Email > Id)</summary>
        public string Display =>
            !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName! :
            !string.IsNullOrWhiteSpace(UserName) ? UserName! :
            !string.IsNullOrWhiteSpace(Email) ? Email! : Id;
    }

    /// <summary>
    /// ViewModel หน้ารวม Users (Admin)
    /// </summary>
    public class UsersIndexVM
    {
        public List<UserRowVM> Items { get; set; } = new();

        // ===== Filters/Sorting/Paging (ค่าเงียบไว้ที่ VM ด้วยก็ได้) =====
        public string? Q { get; set; }          // คำค้น
        public string? Role { get; set; }       // กรอง role
        public string? Status { get; set; }     // active/locked/2fa/unconfirmed
        public string? Sort { get; set; }       // name/email/lock

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalItems { get; set; }

        public int TotalPages => (int)Math.Ceiling(Math.Max(TotalItems, 1) / (double)Math.Max(PageSize, 1));

        // รายการบทบาททั้งหมด (ไว้ใช้ใน dropdown + modal)
        public List<string> AllRoles { get; set; } = new();
    }
}
