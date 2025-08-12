namespace FinalProject.Models;   // หรือ FinalProject.DTOs ก็ได้ แต่ต้องใช้ให้ตรงกัน

public record NewCommentDto(string Body, string? DisplayName);

public record CommentView(
    int Id,
    string Body,
    string Author,
    DateTime CreatedAt,
    bool CanDelete   // ใช้ให้ JS รู้ว่าแสดงปุ่มลบได้ไหม
);
