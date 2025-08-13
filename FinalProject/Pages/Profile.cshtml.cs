using System.ComponentModel.DataAnnotations;
using FinalProject.Data;                   // <-- AppDbContext
using FinalProject.Models;                 // <-- ApplicationUser, UserStats
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Pages
{
    [Authorize]
    [IgnoreAntiforgeryToken] // ถ้าจะเปิด CSRF ให้เปลี่ยนเป็น [ValidateAntiForgeryToken] และส่ง token มากับ fetch
    public class ProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _db;

        public ProfileModel(
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env,
            AppDbContext db)
        {
            _userManager = userManager;
            _env = env;
            _db = db;
        }

        // ===== Properties for View =====
        public string DisplayName { get; set; } = "";
        public string Email { get; set; } = "";
        public string AvatarUrl { get; set; } = "";
        public DateTime JoinDate { get; set; } = DateTime.UtcNow; // ถ้าอยากดึงจริง ให้เพิ่ม CreatedAt ใน ApplicationUser
        public List<string> RecentActivities { get; set; } = new();
        public UserStats Stats { get; set; } = new();

        public string? Location { get; set; }
        public string? Bio { get; set; }
        public int? BirthYear { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) { return; }

            // 1) ข้อมูลโปรไฟล์จาก AspNetUsers (ApplicationUser)
            DisplayName = string.IsNullOrWhiteSpace(user.DisplayName)
                ? (User.Identity?.Name ?? "User")
                : user.DisplayName;
            Email = user.Email ?? "";
            AvatarUrl = string.IsNullOrWhiteSpace(user.AvatarUrl) ? "/img/avatar-default.png" : user.AvatarUrl!;
            Location = user.Location;
            Bio = user.Bio;
            BirthYear = user.BirthYear;

            // 2) JoinDate: ถ้ายังไม่มีฟิลด์ใน AspNetUsers ให้เพิ่ม CreatedAt เองในภายหลัง
            // ตอนนี้ fallback เป็น 3 เดือนที่ผ่านมา
            JoinDate = DateTime.UtcNow.AddMonths(-3);

            // 3) สถิติจากตาราง UserStats (1-1 กับผู้ใช้)
            var stats = await _db.UserStats.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (stats == null)
            {
                // สร้างเริ่มต้นถ้ายังไม่มี
                stats = new UserStats { UserId = user.Id, Reviews = 0, Favorites = 0, Badges = 0 };
                _db.UserStats.Add(stats);
                await _db.SaveChangesAsync();
            }
            Stats = stats;

            // 4) กิจกรรมล่าสุด: ดึงจาก BeerComments ของผู้ใช้ (ตาม UserName) และใส่ชื่อเบียร์
            RecentActivities = await _db.BeerComments
                .Where(c => c.UserName == user.UserName)                     // ถ้ามี UserId ให้เปลี่ยนเป็น c.UserId == user.Id
                .OrderByDescending(c => c.CreatedAt)
                .Take(10)
                .Join(_db.LocalBeers,
                      c => c.LocalBeerId,
                      b => b.Id,
                      (c, b) => new { c, b })
                .Select(x => $"คอมเมนต์ที่ “{x.b.Name}”: {x.c.Body}")
                .ToListAsync();
        }

        // ========= Handlers =========

        // POST: /Profile?handler=UploadAvatar
        public async Task<IActionResult> OnPostUploadAvatarAsync(IFormFile file)
        {
            if (file is null || file.Length == 0) return BadRequest("No file");
            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return BadRequest("Invalid file type");
            if (file.Length > 3 * 1024 * 1024) return BadRequest("File too large (>3MB)");

            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var uploadsRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "avatars");
            Directory.CreateDirectory(uploadsRoot);

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";
            var name = $"{user.Id}_{DateTimeOffset.UtcNow.Ticks}{ext}";
            var path = Path.Combine(uploadsRoot, name);

            await using (var fs = System.IO.File.Create(path))
                await file.CopyToAsync(fs);

            var url = $"/uploads/avatars/{name}";
            user.AvatarUrl = url;

            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded) return StatusCode(500, "Update user failed");

            return new JsonResult(new { avatarUrl = url });
        }

        // POST: /Profile?handler=RemoveAvatar
        public async Task<IActionResult> OnPostRemoveAvatarAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            user.AvatarUrl = null;
            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded) return StatusCode(500, "Update user failed");

            return new JsonResult(new { ok = true });
        }

        public class EditProfileRequest
        {
            [Required, StringLength(100)]
            public string DisplayName { get; set; } = "";

            [Required, EmailAddress, StringLength(256)]
            public string Email { get; set; } = "";

            [StringLength(100)]
            public string? Location { get; set; }

            [StringLength(500)]
            public string? Bio { get; set; }

            [Range(1900, 2100)]
            public int? BirthYear { get; set; }
        }

        // POST: /Profile?handler=Edit
        public async Task<IActionResult> OnPostEditAsync([FromBody] EditProfileRequest input)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            user.DisplayName = input.DisplayName.Trim();
            user.Location = input.Location?.Trim();
            user.Bio = input.Bio?.Trim();
            user.BirthYear = input.BirthYear;

            if (!string.Equals(user.Email, input.Email, StringComparison.OrdinalIgnoreCase))
            {
                var setEmail = await _userManager.SetEmailAsync(user, input.Email);
                if (!setEmail.Succeeded) return StatusCode(500, "SetEmail failed");
                // ถ้าต้อง sync UserName กับ Email:
                // await _userManager.SetUserNameAsync(user, input.Email);
            }

            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded) return StatusCode(500, "Update user failed");

            return new JsonResult(new { ok = true });
        }

        // GET: /Profile?handler=Activities
        public async Task<IActionResult> OnGetActivities()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            // ดึงกิจกรรมล่าสุดจากฐานข้อมูล (เหมือนใน OnGet แต่แยก endpoint)
            var items = await _db.BeerComments
                .Where(c => c.UserName == user.UserName)                     // ถ้ามี UserId ให้เปลี่ยนเงื่อนไขนี้
                .OrderByDescending(c => c.CreatedAt)
                .Take(10)
                .Join(_db.LocalBeers,
                      c => c.LocalBeerId,
                      b => b.Id,
                      (c, b) => new { c, b })
                .Select(x => $"คอมเมนต์ที่ “{x.b.Name}”: {x.c.Body}")
                .ToListAsync();

            return new JsonResult(items);
        }
    }
}
