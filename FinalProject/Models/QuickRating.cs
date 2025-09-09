namespace FinalProject.Models
{
    public class QuickRating
    {
        public int Id { get; set; }
        public int LocalBeerId { get; set; }   // ✅ FK
        public LocalBeer? LocalBeer { get; set; } // ✅ Navigation

        public int Score { get; set; }         // 1..5
        public string? IpHash { get; set; }
        public string? Fingerprint { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }  // (ออปชัน) ผูกกับผู้ใช้ถ้ามีล็อกอิน
    }
}
