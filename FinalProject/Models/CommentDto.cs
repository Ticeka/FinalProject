using System;
using System.Collections.Generic;

namespace FinalProject.Models
{
    // ข้อมูลรับเข้า (รองรับ reply ผ่าน ParentId)
    public record CommentCreateDto(string Body, int? ParentId);

    // ข้อมูลส่งออก (เป็นโครงสร้าง tree)
    public record CommentOutDto(
        int Id,
        string DisplayName,
        string Body,
        DateTime CreatedAt,
        bool CanDelete,
        string? AvatarUrl,
        int? Rating,
        string? ProfileUrl,
        int? ParentId,
        List<CommentOutDto> Replies
    );

    // คง type เก่าไว้เพื่อ Compatibility หากที่อื่นยังอ้างถึง
    public record CommentView(
        int Id,
        string Body,
        string Author,
        DateTime CreatedAt,
        bool CanDelete,
        string? DisplayName = null,
        string? AvatarUrl = null,
        int? Rating = null,
        string? ProfileUrl = null
    );
}
