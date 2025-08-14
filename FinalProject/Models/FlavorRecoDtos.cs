namespace FinalProject.Models
{
    public class FlavorRecoRequest
    {
        public string Base { get; set; } = "";       // Beer | Wine | Whisky | Rum | Gin | Thai Craft | Mocktail
        public List<string> Flavors { get; set; } = new(); // Citrus, Herb, Sweet, Bitter, Smoke, Spice, Malty, Hoppy, Fruity, Floral, Woody
        public int Take { get; set; } = 6;           // จำนวนแนะนำที่ต้องการ
    }

    public class FlavorRecoItem
    {
        public int? Id { get; set; }                 // มีถ้าแมตช์จาก DB
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Province { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public double Rating { get; set; }
        public int RatingCount { get; set; }
        public decimal? Price { get; set; }
        public string Why { get; set; } = "";        // เหตุผล/คำอธิบายว่าทำไมเหมาะ
        public double Score { get; set; }            // คะแนนความเข้ากัน
    }

    public class FlavorRecoResponse
    {
        public string Base { get; set; } = "";
        public string[] Flavors { get; set; } = Array.Empty<string>();
        public List<FlavorRecoItem> Items { get; set; } = new();
    }
}
