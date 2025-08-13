using Microsoft.AspNetCore.Identity;

namespace FinalProject.Models
{
    // เพิ่มฟิลด์ได้ตามต้องการ เช่น DisplayName
    public class ApplicationUser : IdentityUser
    {
        // ชื่อที่แสดงในเว็บ
        public string? DisplayName { get; set; }
        // เมือง/จังหวัด (แสดงในโปรไฟล์)
        public string? Location { get; set; }
        // คำอธิบายสั้น ๆ เกี่ยวกับตัวเอง
        public string? Bio { get; set; }
        // URL รูปโปรไฟล์ (เก็บเป็น path ใน wwwroot หรือ URL S3/Blob)
        public string? AvatarUrl { get; set; }
        // วันเกิด (เก็บเป็นปีพอเพื่อความเป็นส่วนตัว)
        public int? BirthYear { get; set; }
        public virtual UserStats? Stats { get; set; }
    }
}
