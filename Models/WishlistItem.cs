namespace Note.Backend.Models;

public class WishlistItem
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public Product? Product { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
