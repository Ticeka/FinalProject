using System;
using System.ComponentModel.DataAnnotations;
using FinalProject.Models;

namespace FinalProject.Models
{
    public class ActivityLog
    {
        public long Id { get; set; }

        // ใครเป็นคนทำ (optional ถ้าเป็น guest ก็ปล่อยว่างได้)
        [MaxLength(450)]
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        // อะไร: "rating.add", "comment.add", "favorite.toggle", "profile.edit", "admin.lock", ...
        [MaxLength(64)]
        public string Action { get; set; } = default!;

        // โดนกับ resource อะไร (optional): "LocalBeer", "User", ...
        [MaxLength(64)]
        public string? SubjectType { get; set; }

        // ไอดี resource (optional) เช่น LocalBeer.Id
        public string? SubjectId { get; set; }

        // สรุปอ่านง่าย (“ให้คะแนน ‘ChiangMai IPA’ = 4/5”)
        [MaxLength(300)]
        public string Message { get; set; } = default!;

        // meta อื่น ๆ (json) เช่น { "score": 4, "beerId": 12 }
        public string? MetaJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // สำหรับ forensic (optional)
        [MaxLength(128)] public string? IpHash { get; set; }
        [MaxLength(256)] public string? UserAgent { get; set; }
    }
}
