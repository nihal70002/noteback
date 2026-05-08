namespace Note.Backend.Models;

public class Product
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Image { get; set; } = string.Empty;
    public string? Image2 { get; set; }
    public string? Image3 { get; set; }
    public string? Image4 { get; set; }
    public string? Image5 { get; set; }
    public string? VideoUrl { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsNew { get; set; }
    public string? Description { get; set; }
    public int Stock { get; set; } = 25;
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsPack { get; set; }
    public int? PackSize { get; set; }
}
