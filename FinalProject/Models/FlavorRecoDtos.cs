using System;
using System.Collections.Generic;

namespace FinalProject.Models
{
    /// <summary>
    /// Request จากผู้ใช้ที่ส่งเข้ามาเพื่อขอคำแนะนำ
    /// </summary>
    public class FlavorRecoRequest
    {
        public string Base { get; set; } = "";               // Beer | Wine | Whisky | Rum | Gin | Thai Craft | Mocktail
        public List<string> Flavors { get; set; } = new();   // Citrus, Herb, Sweet, Bitter, Smoke, Spice, Malty, Hoppy, Fruity, Floral, Woody
        public int Take { get; set; } = 6;                   // จำนวนแนะนำที่ต้องการ
    }

    /// <summary>
    /// Item ที่ถูกแนะนำ
    /// </summary>
    public class FlavorRecoItem
    {
        public int? Id { get; set; }                         // มีถ้า match จาก DB
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Province { get; set; } = "";
        public string ImageUrl { get; set; } = "";

        public double Rating { get; set; }
        public int RatingCount { get; set; }
        public decimal? Price { get; set; }

        public string Why { get; set; } = "";                // เหตุผลที่แนะนำ
        public double Score { get; set; }                    // คะแนนความเข้ากัน
    }

    /// <summary>
    /// Response ที่ส่งกลับไปยังผู้ใช้
    /// </summary>
    public class FlavorRecoResponse
    {
        public string Base { get; set; } = "";
        public string[] Flavors { get; set; } = Array.Empty<string>();
        public List<FlavorRecoItem> Items { get; set; } = new();
    }
}
