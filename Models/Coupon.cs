namespace Note.Backend.Models;

public class Coupon
{
    public string Code { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public bool IsActive { get; set; } = true;
}
