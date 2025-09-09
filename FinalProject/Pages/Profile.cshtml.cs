using System.ComponentModel.DataAnnotations;
using FinalProject.Data;                   // AppDbContext
using FinalProject.Models;                 // ApplicationUser, UserStats
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
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;
        public List<string> RecentActivities { get; set; } = new();
        public UserStats Stats { get; set; } = new();

        public string? Location { get; set; }
        public string? Bio { get; set; }
        public int? BirthYear { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return;

            // 1) โปรไฟล์พื้นฐาน
            DisplayName = string.IsNullOrWhiteSpace(user.DisplayName)
                ? (User.Identity?.Name ?? "User")
                : user.DisplayName;
            Email = user.Email ?? "";
            AvatarUrl = string.IsNullOrWhiteSpace(user.AvatarUrl) ? "/img/avatar-default.png" : user.AvatarUrl!;
            Location = user.Location;
            Bio = user.Bio;
            BirthYear = user.BirthYear;

            // 2) JoinDate (ถ้าต้องการจริงจัง แนะนำเพิ่ม CreatedAt ใน ApplicationUser แล้ว map)
            JoinDate = DateTime.UtcNow.AddMonths(-3);

            var userId = user.Id;
            var userName = user.UserName ?? "";

            // ---------- 🔧 Backfill เรตติ้ง guest → ของผู้ใช้ ----------
            var fp = GetFingerprintFromCookies(Request.Cookies);
            if (!string.IsNullOrWhiteSpace(fp))
            {
                await BackfillRatingsToUserAsync(fp!, userId);
            }

            // ---------- ดึง "จำนวน" ปัจจุบัน ----------
            // Reviews (QuickRating): นับของผู้ใช้ + เผื่อแถวเก่าที่ยังไม่ได้ backfill แต่ fingerprint ตรง
            var reviewsQuery = _db.QuickRatings.AsQueryable().Where(r => r.UserId == userId);
            if (!string.IsNullOrWhiteSpace(fp))
                reviewsQuery = reviewsQuery.Concat(
                    _db.QuickRatings.Where(r => r.UserId == null && r.Fingerprint == fp)
                );
            var reviewsCount = await reviewsQuery.CountAsync();

            // Comments (BeerComment): UserId เป็นหลัก + fallback ด้วย UserName
            var commentsCount = await _db.BeerComments
                .Where(c => c.UserId == userId || (c.UserId == null && c.UserName == userName))
                .CountAsync();

            // Favorites
            var favoritesCount = await _db.BeerFavorites
                .Where(f => f.UserId == userId)
                .CountAsync();

            // ---------- Sync กับ UserStats ----------
            var stats = await _db.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
            if (stats == null)
            {
                stats = new UserStats
                {
                    UserId = userId,
                    Reviews = reviewsCount,
                    Comments = commentsCount,
                    Favorites = favoritesCount,
                    Badges = 0
                };
                _db.UserStats.Add(stats);
                await _db.SaveChangesAsync();
            }
            else
            {
                bool changed = false;
                if (stats.Reviews != reviewsCount) { stats.Reviews = reviewsCount; changed = true; }
                if (stats.Comments != commentsCount) { stats.Comments = commentsCount; changed = true; }
                if (stats.Favorites != favoritesCount) { stats.Favorites = favoritesCount; changed = true; }
                if (changed) await _db.SaveChangesAsync();
            }
            Stats = stats;

            // ---------- กิจกรรมล่าสุด: รวม "คอมเมนต์" + "รีวิว" ----------
            var commentActs = await _db.BeerComments
                .Where(c => c.UserId == userId || (c.UserId == null && c.UserName == userName))
                .OrderByDescending(c => c.CreatedAt)
                .Take(20)
                .Join(_db.LocalBeers,
                      c => c.LocalBeerId,
                      b => b.Id,
                      (c, b) => new ActivityItem { When = c.CreatedAt, Text = $"คอมเมนต์ที่ “{b.Name}”: {c.Body}" })
                .ToListAsync();

            var ratingFilter = _db.QuickRatings.Where(r => r.UserId == userId);
            if (!string.IsNullOrWhiteSpace(fp))
            {
                ratingFilter = ratingFilter.Concat(
                    _db.QuickRatings.Where(r => r.UserId == null && r.Fingerprint == fp)
                );
            }

            var ratingActs = await ratingFilter
                .OrderByDescending(r => r.CreatedAt)
                .Take(20)
                .Join(_db.LocalBeers,
                      r => r.LocalBeerId,
                      b => b.Id,
                      (r, b) => new ActivityItem { When = r.CreatedAt, Text = $"ให้คะแนน “{b.Name}” = {r.Score:0.0}/5" })
                .ToListAsync();

            RecentActivities = commentActs
                .Concat(ratingActs)
                .OrderByDescending(a => a.When)
                .Take(10)
                .Select(a => a.Text)
                .ToList();
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

            var userId = user.Id;
            var userName = user.UserName ?? "";
            var fp = GetFingerprintFromCookies(Request.Cookies);

            // รวมกิจกรรมคอมเมนต์ + รีวิว (เหมือน OnGet)
            var commentActs = await _db.BeerComments
                .Where(c => c.UserId == userId || (c.UserId == null && c.UserName == userName))
                .OrderByDescending(c => c.CreatedAt)
                .Take(20)
                .Join(_db.LocalBeers, c => c.LocalBeerId, b => b.Id,
                    (c, b) => new ActivityItem { When = c.CreatedAt, Text = $"คอมเมนต์ที่ “{b.Name}”: {c.Body}" })
                .ToListAsync();

            var ratingFilter = _db.QuickRatings.Where(r => r.UserId == userId);
            if (!string.IsNullOrWhiteSpace(fp))
            {
                ratingFilter = ratingFilter.Concat(
                    _db.QuickRatings.Where(r => r.UserId == null && r.Fingerprint == fp)
                );
            }

            var ratingActs = await ratingFilter
                .OrderByDescending(r => r.CreatedAt)
                .Take(20)
                .Join(_db.LocalBeers, r => r.LocalBeerId, b => b.Id,
                    (r, b) => new ActivityItem { When = r.CreatedAt, Text = $"ให้คะแนน “{b.Name}” = {r.Score:0.0}/5" })
                .ToListAsync();

            var items = commentActs
                .Concat(ratingActs)
                .OrderByDescending(a => a.When)
                .Take(10)
                .Select(a => a.Text)
                .ToList();

            return new JsonResult(items);
        }

        // ===== Helpers =====
        private static string? GetFingerprintFromCookies(IRequestCookieCollection cookies)
        {
            // ปรับ key ให้ตรงกับที่โค้ดส่วนให้เรตตั้งไว้ (เติม key ได้ตามโปรเจกต์)
            string[] keys = { "qr_fp", "rating_fp", "fingerprint", "st_fp" };
            foreach (var k in keys)
                if (cookies.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                    return v.Trim();
            return null;
        }

        private async Task BackfillRatingsToUserAsync(string fingerprint, string userId)
        {
            // ถ้าใช้ EF Core 7+ ใช้ ExecuteUpdateAsync จะไวกว่า:
            // await _db.QuickRatings
            //   .Where(r => r.UserId == null && r.Fingerprint == fingerprint)
            //   .ExecuteUpdateAsync(s => s.SetProperty(r => r.UserId, userId));

            var orphan = await _db.QuickRatings
                .Where(r => r.UserId == null && r.Fingerprint == fingerprint)
                .ToListAsync();

            if (orphan.Count == 0) return;

            foreach (var r in orphan)
                r.UserId = userId;

            await _db.SaveChangesAsync();
        }

        private sealed class ActivityItem
        {
            public DateTime When { get; set; }
            public string Text { get; set; } = "";
        }
    }
}
