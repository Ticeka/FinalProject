namespace FinalProject.Models
{
    public class LocalBeer
    {
        public int Id { get; set; }             // Primary Key
        public string Name { get; set; }        // ชื่อเบียร์หรือชื่อร้าน
        public string Description { get; set; } // คำอธิบาย
        public string Province { get; set; }    // จังหวัดที่ตั้ง
        public string District { get; set; }    // อำเภอ (optional)
        public string Type { get; set; }        // ประเภท เช่น ร้านเบียร์, แหล่งผลิต, งานเทศกาล
        public string Address { get; set; }     // ที่อยู่โดยละเอียด

        public double Latitude { get; set; }    // ละติจูด (พิกัด)
        public double Longitude { get; set; }   // ลองจิจูด (พิกัด)

        public string ImageUrl { get; set; }    // ลิงก์รูปภาพ
        public string Website { get; set; }     // เว็บไซต์ทางการ
        public string FacebookPage { get; set; } // ลิงก์ Facebook Page
        public string PhoneNumber { get; set; } // เบอร์โทรติดต่อ
        public string OpenHours { get; set; }   // เวลาเปิดทำการ

        public double AlcoholLevel { get; set; } // ดีกรีแอลกอฮอล์ (%) เช่น 5.0
        public decimal Price { get; set; }       // ราคาโดยประมาณ (บาท)

        public double Rating { get; set; }      // ค่าเฉลี่ยคะแนนรีวิว (เช่น 4.5)
        public int RatingCount { get; set; }    // จำนวนคนที่รีวิว

        public DateTime CreatedAt { get; set; } = DateTime.Now; // เวลาที่เพิ่มเข้า DB
        public DateTime? UpdatedAt { get; set; }                // เวลาที่อัปเดตล่าสุด (nullable)

        // ===== ฟิลด์ใหม่จากหน้า Detail =====
        public string PlaceOfOrigin { get; set; }      // แหล่งกำเนิด
        public string Region { get; set; }             // ภูมิภาค
        public string Creator { get; set; }            // ผู้ผลิต/ผู้สร้างสรรค์
        public int? Volume { get; set; }               // ปริมาตร (ml)
        public string MainIngredients { get; set; }    // วัตถุดิบหลัก
        public string ProductMethod { get; set; }      // วิธีการผลิต
        public int? ProductYear { get; set; }          // ปีผลิต
        public string Rights { get; set; }            // ลิขสิทธิ์/สิทธิ์
        public string Distributor { get; set; }        // ผู้จัดจำหน่าย
        public string DistributorChanel { get; set; }  // ช่องทางจัดจำหน่าย
        public string Award { get; set; }              // รางวัล
        public string Notes { get; set; }              // บันทึก
        public double? AverageRating { get; set; }     // คะแนนเฉลี่ย (ช่องเฉพาะ)
        public string ProductId { get; set; }          // รหัสสินค้า/ล็อต
        public string TypeOfLiquor { get; set; }       // ประเภทสุรา
    }
}
