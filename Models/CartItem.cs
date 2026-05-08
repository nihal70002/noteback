namespace Note.Backend.Models;

public class CartItem
{
    public int Id { get; set; }
    public string CartId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? SelectedChoicesJson { get; set; }

    public Product? Product { get; set; }
}
