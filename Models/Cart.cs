using System.Collections.Generic;

namespace Note.Backend.Models;

public class Cart
{
    public string Id { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Active";
    public bool IsOrdered { get; set; }
    public DateTime? OrderedAt { get; set; }
    public List<CartItem> Items { get; set; } = new();
}
