namespace Note.Backend.Models;

public class ShippingSettings
{
    public int Id { get; set; }
    public bool Enabled { get; set; } = true;
    public decimal StandardShippingFee { get; set; } = 5m;
    public int FreeShippingThreshold { get; set; } = 3; // for quantity-based
    public decimal FreeShippingAmount { get; set; } = 50m; // for amount-based
    public string FreeShippingType { get; set; } = "quantity"; // "quantity" or "amount"
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
