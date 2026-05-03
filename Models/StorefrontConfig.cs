namespace Note.Backend.Models;

public class StorefrontConfig
{
    public int Id { get; set; }
    
    // Hero Section
    public string? HeroImageUrl { get; set; }
    public string? HeroTitle { get; set; }
    public string? HeroSubtitle { get; set; }
    public string? HeroLink { get; set; }

    // Category 1
    public string? Category1ImageUrl { get; set; }
    public string? Category1Title { get; set; }
    public string? Category1Link { get; set; }

    // Category 2
    public string? Category2ImageUrl { get; set; }
    public string? Category2Title { get; set; }
    public string? Category2Link { get; set; }
}
