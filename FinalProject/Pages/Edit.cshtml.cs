using System.ComponentModel.DataAnnotations;
using FinalProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinalProject.Pages
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public EditModel(UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _env = env;
        }

        // สำหรับแสดงผลหัวข้อ/อวตารปัจจุบัน
        public string CurrentAvatarUrl { get; set; } = "";
        public string Initials { get; set; } = "?";

        // ข้อมูลฟอร์ม
        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Display(Name = "ชื่อที่แสดง")]
            [Required, StringLength(100)]
            public string DisplayName { get; set; } = "";

            [Display(Name = "อีเมล")]
            [Required, EmailAddress, StringLength(256)]
            public string Email { get; set; } = "";

            [Display(Name = "ที่อยู่/จังหวัด")]
            [StringLength(100)]
            public string? Location { get; set; }

            [Display(Name = "แนะนำตัวสั้น ๆ")]
            [StringLength(500)]
            public string? Bio { get; set; }

            [Display(Name = "ปีเกิด")]
            [Range(1900, 2100)]
            public int? BirthYear { get; set; }

            [Display(Name = "ไฟล์รูปโปรไฟล์")]
            public IFormFile? AvatarFile { get; set; }

            [Display(Name = "ลบรูปโปรไฟล์ปัจจุบัน")]
            public bool RemoveAvatar { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            Input = new InputModel
            {
                DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? (User.Identity?.Name ?? "User") : user.DisplayName,
                Email = user.Email ?? "",
                Location = user.Location,
                Bio = user.Bio,
                BirthYear = user.BirthYear
            };

            CurrentAvatarUrl = string.IsNullOrWhiteSpace(user.AvatarUrl) ? "" : user.AvatarUrl;
            Initials = MakeInitials(Input.DisplayName ?? Input.Email ?? "User");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                CurrentAvatarUrl = CurrentAvatarUrl ?? "";
                Initials = MakeInitials(Input.DisplayName ?? Input.Email ?? "User");
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            // อัปเดตข้อมูลพื้นฐาน
            user.DisplayName = Input.DisplayName.Trim();
            user.Location = Input.Location?.Trim();
            user.Bio = Input.Bio?.Trim();
            user.BirthYear = Input.BirthYear;

            // เปลี่ยนอีเมลถ้าไม่ตรง
            if (!string.Equals(user.Email, Input.Email, StringComparison.OrdinalIgnoreCase))
            {
                var setEmail = await _userManager.SetEmailAsync(user, Input.Email);
                if (!setEmail.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "ไม่สามารถเปลี่ยนอีเมลได้");
                    return Page();
                }
                // ถ้าอยาก sync UserName กับ Email:
                // await _userManager.SetUserNameAsync(user, Input.Email);
            }

            // ลบรูปโปรไฟล์ถ้าติ๊ก
            if (Input.RemoveAvatar)
            {
                user.AvatarUrl = null;
            }

            // อัปโหลดรูปใหม่ถ้ามี
            if (Input.AvatarFile is { Length: > 0 })
            {
                if (!Input.AvatarFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("Input.AvatarFile", "ไฟล์ภาพไม่ถูกต้อง");
                    return Page();
                }
                if (Input.AvatarFile.Length > 3 * 1024 * 1024)
                {
                    ModelState.AddModelError("Input.AvatarFile", "ไฟล์ใหญ่เกิน 3 MB");
                    return Page();
                }

                var uploadsRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "avatars");
                Directory.CreateDirectory(uploadsRoot);

                var ext = Path.GetExtension(Input.AvatarFile.FileName);
                if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                var name = $"{user.Id}_{DateTimeOffset.UtcNow.Ticks}{ext}";
                var path = Path.Combine(uploadsRoot, name);

                await using (var fs = System.IO.File.Create(path))
                    await Input.AvatarFile.CopyToAsync(fs);

                user.AvatarUrl = $"/uploads/avatars/{name}";
            }

            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "บันทึกโปรไฟล์ไม่สำเร็จ");
                return Page();
            }

            TempData["ProfileSaved"] = "บันทึกโปรไฟล์เรียบร้อยแล้ว";
            return RedirectToPage("/Profile"); // กลับไปหน้าโปรไฟล์
        }

        // ===== Helpers =====
        private static string MakeInitials(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpperInvariant();
            return (parts[0][0].ToString() + parts[^1][0]).ToUpperInvariant();
        }
    }
}
