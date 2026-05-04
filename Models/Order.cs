namespace Note.Backend.Models;

public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public string? CouponCode { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Delivered
    
    // Shipping Details
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AlternatePhoneNumber { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string Landmark { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    
    public List<OrderItem> Items { get; set; } = new();
    
    // Razorpay Integration
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    
    public string ProductId { get; set; } = string.Empty;
    public Product? Product { get; set; }
    
    public int Quantity { get; set; }
    public decimal Price { get; set; } // Price at the time of purchase
}

public class ShippingDetails
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AlternatePhoneNumber { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string Landmark { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string? CouponCode { get; set; }
}
