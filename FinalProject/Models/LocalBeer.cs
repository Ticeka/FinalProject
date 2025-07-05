namespace FinalProject.Models
{
    public class LocalBeer
    {
        public int Id { get; set; }            // Primary Key
        public string Name { get; set; }       // ชื่อเบียร์หรือร้าน
        public string Description { get; set; } // คำอธิบาย
        public string Province { get; set; }    // จังหวัด
        public string Type { get; set; }        // ประเภท เช่น ร้านเบียร์, แหล่งผลิต
        public double Latitude { get; set; }    // ละติจูด
        public double Longitude { get; set; }   // ลองจิจูด
    }
}
