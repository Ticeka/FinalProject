using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    // เก็บสถิติของผู้ใช้ 1 คน = 1 แถว (Primary Key = UserId)
    public class UserStats
    {
        [Key]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = default!;

        // จำนวนรีวิวที่ผู้ใช้เขียน
        public int Reviews { get; set; }

        // จำนวนรายการที่ผู้ใช้กดเป็นรายการโปรด
        public int Favorites { get; set; }

        // จำนวนแบดจ์/เหรียญรางวัลที่ผู้ใช้ได้รับ
        public int Badges { get; set; }

        // นำทางกลับไปยังผู้ใช้ (ไม่จำเป็นต้องใส่ก็ได้ แต่ช่วยเวลา include)
        public virtual ApplicationUser User { get; set; } = default!;
    }
}
