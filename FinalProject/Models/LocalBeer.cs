public class LocalBeer
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;          // ต้องมี
    public string Province { get; set; } = default!;      // ต้องมี

    public string? Description { get; set; }
    public string? District { get; set; }
    public string? Type { get; set; }
    public string? Address { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public string? ImageUrl { get; set; }
    public string? Website { get; set; }
    public string? FacebookPage { get; set; }
    public string? PhoneNumber { get; set; }
    public string? OpenHours { get; set; }

    public double AlcoholLevel { get; set; }
    public decimal Price { get; set; }

    public double Rating { get; set; }
    public int RatingCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public string? PlaceOfOrigin { get; set; }
    public string? Region { get; set; }
    public string? Creator { get; set; }
    public int? Volume { get; set; }
    public string? MainIngredients { get; set; }
    public string? ProductMethod { get; set; }
    public int? ProductYear { get; set; }
    public string? Rights { get; set; }
    public string? Distributor { get; set; }
    public string? DistributorChanel { get; set; }
    public string? Award { get; set; }
    public string? Notes { get; set; }
    public double? AverageRating { get; set; }
    public string? ProductId { get; set; }
    public string? TypeOfLiquor { get; set; }
}
