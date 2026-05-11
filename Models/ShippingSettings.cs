using System.ComponentModel.DataAnnotations;

namespace Note.Backend.Models;

public class ShippingSettings
{
    public int Id { get; set; }
    
    [Range(0, 1000)]
    public decimal StandardShippingFee { get; set; } = 5m;
    
    [Range(1, 100)]
    public int FreeShippingThreshold { get; set; } = 3; // for quantity-based
    
    [Range(0, 10000)]
    public decimal FreeShippingAmount { get; set; } = 500m; // for amount-based
    
    [Required]
    [RegularExpression("^(quantity|amount)$", ErrorMessage = "Free shipping type must be 'quantity' or 'amount'.")]
    public string FreeShippingType { get; set; } = "quantity"; // "quantity" or "amount"
    
    public bool Enabled { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
