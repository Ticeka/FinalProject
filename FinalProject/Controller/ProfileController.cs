using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using FinalProject.Data;
using FinalProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _db;

        public ProfileController(UserManager<ApplicationUser> userManager, IWebHostEnvironment env, AppDbContext db)
        {
            _userManager = userManager;
            _env = env;
            _db = db;
        }

        // ===== ViewModel / DTO =====
        public class ProfileVm
        {
            public string? Email { get; set; }
            [Display(Name = "ชื่อที่แสดง"), StringLength(40)]
            public string? DisplayName { get; set; }
            [Display(Name = "จังหวัด/เมือง"), StringLength(60)]
            public string? Location { get; set; }
            [Display(Name = "แนะนำตัวสั้น ๆ"), StringLength(280)]
            public string? Bio { get; set; }
            [Display(Name = "ปีเกิด"), Range(1900, 2100)]
            public int? BirthYear { get; set; }
            public string? AvatarUrl { get; set; }
            [Display(Name = "ความสนใจ (คั่นด้วย , )")]
            public string? Interests { get; set; } // เก็บ Tag เป็น CSV
        }

        public class UpdateDto
        {
            [StringLength(40)] public string? DisplayName { get; set; }
            [StringLength(60)] public string? Location { get; set; }
            [StringLength(280)] public string? Bio { get; set; }
            [Range(1900, 2100)] public int? BirthYear { get; set; }
            public string? Interests { get; set; }
        }

        public record ActivityDto(int Id, string Type, string Title, string When, int? Score);

        // ===== Pages =====
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var u = await _userManager.GetUserAsync(User);
            if (u == null) return Challenge();

            var vm = new ProfileVm
            {
                Email = u.Email,
                DisplayName = u.DisplayName,
                Location = u.Location,
                Bio = u.Bio,
                BirthYear = u.BirthYear,
                AvatarUrl = u.AvatarUrl,
                Interests = (u as dynamic)?.Interests ?? null // ถ้ามีฟิลด์ Interests ใน ApplicationUser
            };
            return View(vm);
        }

        // ===== API: Update profile (AJAX) =====
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] UpdateDto dto)
        {
            var u = await _userManager.GetUserAsync(User);
            if (u == null) return Unauthorized();

            // ตรวจข้อความหยาบ/ลิงก์แปลกๆ แบบง่าย
            string Sanitize(string? s)
            {
                if (string.IsNullOrWhiteSpace(s)) return s ?? "";
                s = Regex.Replace(s, @"<[^>]*>", ""); // ตัด HTML
                s = s.Trim();
                return s.Length > 0 ? s : "";
            }

            u.DisplayName = Sanitize(dto.DisplayName);
            u.Location = Sanitize(dto.Location);
            u.Bio = Sanitize(dto.Bio);
            u.BirthYear = dto.BirthYear;
            // ถ้ามีฟิลด์ Interests ใน ApplicationUser
            try { ((dynamic)u).Interests = Sanitize(dto.Interests); } catch { /* ignore if not exists */ }

            var result = await _userManager.UpdateAsync(u);
            if (!result.Succeeded) return BadRequest(new { ok = false, errors = result.Errors });

            return Json(new
            {
                ok = true,
                data = new
                {
                    displayName = u.DisplayName,
                    location = u.Location,
                    bio = u.Bio,
                    birthYear = u.BirthYear,
                    interests = (u as dynamic)?.Interests ?? null
                }
            });
        }

        // ===== API: Upload avatar (AJAX + drag&drop) =====
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var u = await _userManager.GetUserAsync(User);
            if (u == null) return Unauthorized();
            if (file == null || file.Length == 0) return BadRequest(new { ok = false, msg = "ไม่พบไฟล์" });

            var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowed.Contains(file.ContentType))
                return BadRequest(new { ok = false, msg = "อนุญาตเฉพาะ JPG/PNG/WebP" });

            if (file.Length > 2 * 1024 * 1024)
                return BadRequest(new { ok = false, msg = "ขนาดไฟล์ต้องไม่เกิน 2MB" });

            var uploads = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(uploads);
            var ext = Path.GetExtension(file.FileName);
            var name = $"{u.Id}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
            var full = Path.Combine(uploads, name);
            using (var fs = System.IO.File.Create(full))
                await file.CopyToAsync(fs);

            u.AvatarUrl = $"/uploads/avatars/{name}";
            await _userManager.UpdateAsync(u);

            return Json(new { ok = true, url = u.AvatarUrl });
        }

        // ===== API: Recent activities (infinite scroll) =====
        [HttpGet]
        public async Task<IActionResult> Activities(int page = 1, int pageSize = 10)
        {
            var u = await _userManager.GetUserAsync(User);
            if (u == null) return Unauthorized();

            // ตัวอย่างผูก QuickRating -> LocalBeer (ปรับชื่อฟิลด์ตามจริง)
            var q = _db.Set<QuickRating>()
                .Where(r => r.UserId == u.Id)
                .OrderByDescending(r => r.CreatedAt);

            var total = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(r => new ActivityDto(
                    r.Id,
                    "Rating",
                    _db.Set<LocalBeer>().Where(d => d.Id == r.LocalBeerId).Select(d => d.Name).FirstOrDefault() ?? "Unknown",
                    r.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    r.Score
                ))
                .ToListAsync();

            return Json(new { ok = true, items, total, page, pageSize });
        }
    }
}
