using System.Text.Json;
using FinalProject.Data;
using FinalProject.Models;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Services
{
    public class ActivityLogger : IActivityLogger
    {
        private readonly AppDbContext _db;
        public ActivityLogger(AppDbContext db) { _db = db; }

        public async Task LogAsync(string? userId, string action, string? subjectType, string? subjectId,
                                   string message, object? meta = null, string? ipHash = null, string? userAgent = null)
        {
            var log = new ActivityLog
            {
                UserId = userId,
                Action = action,
                SubjectType = subjectType,
                SubjectId = subjectId,
                Message = message,
                MetaJson = meta == null ? null : JsonSerializer.Serialize(meta),
                IpHash = ipHash,
                UserAgent = userAgent
            };
            _db.ActivityLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        public Task RatingAddedAsync(string? userId, int beerId, string beerName, double score) =>
            LogAsync(userId, "rating.add", "LocalBeer", beerId.ToString(),
                     $"ให้คะแนน “{beerName}” = {score:0.0}/5",
                     new { beerId, score });

        public Task CommentAddedAsync(string? userId, int beerId, string beerName, string body) =>
            LogAsync(userId, "comment.add", "LocalBeer", beerId.ToString(),
                     $"คอมเมนต์ที่ “{beerName}”: {body}", new { beerId });

        public Task FavoriteToggledAsync(string? userId, int beerId, string beerName, bool isFav) =>
            LogAsync(userId, isFav ? "favorite.add" : "favorite.remove", "LocalBeer", beerId.ToString(),
                     $"{(isFav ? "เพิ่ม" : "เอาออก")}รายการถูกใจ “{beerName}”",
                     new { beerId, isFav });

        public Task ProfileEditedAsync(string userId) =>
            LogAsync(userId, "profile.edit", "User", userId, "แก้ไขโปรไฟล์");

        public Task AvatarChangedAsync(string userId, bool removed) =>
            LogAsync(userId, removed ? "avatar.remove" : "avatar.upload", "User", userId,
                     removed ? "ลบรูปโปรไฟล์" : "อัปโหลดรูปโปรไฟล์");

        public Task AdminUserLockedAsync(string adminId, string targetUserId, string targetUserName, int days) =>
            LogAsync(adminId, "admin.lock", "User", targetUserId,
                     $"ล็อก {targetUserName} {days} วัน", new { targetUserId, days });

        public Task AdminUserUnlockedAsync(string adminId, string targetUserId, string targetUserName) =>
            LogAsync(adminId, "admin.unlock", "User", targetUserId,
                     $"ปลดล็อก {targetUserName}", new { targetUserId });

        public Task Admin2faToggledAsync(string adminId, string targetUserId, string targetUserName, bool enable) =>
            LogAsync(adminId, enable ? "admin.2fa.enable" : "admin.2fa.disable", "User", targetUserId,
                     $"{(enable ? "เปิด" : "ปิด")} 2FA สำหรับ {targetUserName}",
                     new { targetUserId, enable });

        public Task AdminRolesUpdatedAsync(string adminId, string targetUserId, string targetUserName, string rolesCsv) =>
            LogAsync(adminId, "admin.roles.update", "User", targetUserId,
                     $"อัปเดตรายชื่อบทบาทของ {targetUserName}: {rolesCsv}", new { targetUserId, rolesCsv });
    }
}
