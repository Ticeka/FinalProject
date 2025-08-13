// Models/CommentDtos.cs
using System;

namespace FinalProject.Models
{
    // ใช้ตอน POST คอมเมนต์ (Minimal API หรือ Controller ก็ใช้ตัวนี้ได้)
    public record CommentCreateDto(string Body, string? DisplayName, int? UserRating = null);
    public record NewCommentDto(string Body, string? DisplayName, int? UserRating = null);

    // ใช้ตอนส่งกลับให้หน้าบ้าน (GET/POST)
    public record CommentOutDto(
        int Id,
        string DisplayName,
        string Body,
        DateTime CreatedAt,
        bool CanDelete,
        string? AvatarUrl,
        int? Rating,
        string? ProfileUrl   // <— ใหม่
    );

    // สำหรับ Minimal API ที่เดิมใช้ CommentView (ใส่ฟิลด์ใหม่เป็น optional)
    public record CommentView(
        int Id,
        string Body,
        string Author,
        DateTime CreatedAt,
        bool CanDelete,
        string? DisplayName = null,
        string? AvatarUrl = null,
        int? Rating = null,
        string? ProfileUrl = null // <— ใหม่
    );
}
