namespace Note.Backend.Models;

public class ProductReview
{
    public int Id { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public Product? Product { get; set; }
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string Images { get; set; } = string.Empty; // JSON array of image URLs
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
