using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Note.Backend.Data;
using Note.Backend.Models;

namespace Note.Backend.Services;

public class OrderService : IOrderService
{
    private readonly NoteDbContext _context;
    private readonly IConfiguration _configuration;
    private static readonly HttpClient _httpClient = new HttpClient();

    public OrderService(NoteDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<(string? OrderId, string? RazorpayOrderId, decimal Amount, string? Error)> CheckoutAsync(string cartId, string userId, ShippingDetails shippingDetails)
    {
        if (string.IsNullOrWhiteSpace(shippingDetails.FullName) ||
            string.IsNullOrWhiteSpace(shippingDetails.PhoneNumber) ||
            string.IsNullOrWhiteSpace(shippingDetails.AddressLine1) ||
            string.IsNullOrWhiteSpace(shippingDetails.City) ||
            string.IsNullOrWhiteSpace(shippingDetails.State) ||
            string.IsNullOrWhiteSpace(shippingDetails.Pincode))
        {
            return (null, null, 0, "Please fill all required shipping fields.");
        }

        var primaryPhone = shippingDetails.PhoneNumber.Trim();
        if (primaryPhone.Length < 10 || primaryPhone.Length > 15)
        {
            return (null, null, 0, "Primary phone number is invalid.");
        }

        var fullAddress = string.Join(", ", new[]
        {
            shippingDetails.AddressLine1?.Trim(),
            shippingDetails.AddressLine2?.Trim(),
            shippingDetails.City?.Trim(),
            shippingDetails.State?.Trim(),
            shippingDetails.Pincode?.Trim()
        }.Where(part => !string.IsNullOrWhiteSpace(part)));

        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart == null || !cart.Items.Any())
        {
            return (null, null, 0, "Cart is empty or not found.");
        }

        foreach (var item in cart.Items)
        {
            if (item.Product == null)
            {
                return (null, null, 0, "A product in your cart is no longer available.");
            }

            if (item.Quantity > item.Product.Stock)
            {
                return (null, null, 0, $"Only {item.Product.Stock} item(s) available for {item.Product.Name}.");
            }
        }

        var subtotal = cart.Items.Sum(i => i.Quantity * (i.Product?.Price ?? 0));
        var couponCode = shippingDetails.CouponCode?.Trim().ToUpper();
        var discountAmount = 0m;

        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode && c.IsActive);
            if (coupon == null)
            {
                return (null, null, 0, "Coupon code is invalid.");
            }

            discountAmount = Math.Round(subtotal * (coupon.DiscountPercent / 100m), 2);
        }

        var shippingFee = subtotal - discountAmount >= 50m ? 0m : 5m;
        var totalAmount = subtotal - discountAmount + shippingFee;

        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Subtotal = subtotal,
            DiscountAmount = discountAmount,
            ShippingFee = shippingFee,
            CouponCode = couponCode,
            TotalAmount = totalAmount,
            FullName = (shippingDetails.FullName ?? string.Empty).Trim(),
            PhoneNumber = primaryPhone,
            AlternatePhoneNumber = shippingDetails.AlternatePhoneNumber?.Trim() ?? string.Empty,
            AddressLine1 = (shippingDetails.AddressLine1 ?? string.Empty).Trim(),
            AddressLine2 = shippingDetails.AddressLine2?.Trim() ?? string.Empty,
            City = (shippingDetails.City ?? string.Empty).Trim(),
            State = (shippingDetails.State ?? string.Empty).Trim(),
            DeliveryAddress = string.IsNullOrWhiteSpace(shippingDetails.DeliveryAddress)
                ? fullAddress
                : shippingDetails.DeliveryAddress.Trim(),
            Landmark = shippingDetails.Landmark?.Trim() ?? string.Empty,
            Pincode = (shippingDetails.Pincode ?? string.Empty).Trim(),
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.Product?.Price ?? 0
            }).ToList()
        };

        // Razorpay API Call
        var keyId = (_configuration["RAZORPAY_KEY_ID"] ?? Environment.GetEnvironmentVariable("RAZORPAY_KEY_ID"))?.Trim();
        var keySecret = (_configuration["RAZORPAY_KEY_SECRET"] ?? Environment.GetEnvironmentVariable("RAZORPAY_KEY_SECRET"))?.Trim();

        if (string.IsNullOrEmpty(keyId) || string.IsNullOrEmpty(keySecret))
        {
            return (null, null, 0, "Payment gateway configuration is missing.");
        }

        var amountInPaise = (int)Math.Round(totalAmount * 100);
        if (amountInPaise < 100)
        {
            return (null, null, 0, "Amount must be at least ₹1.00");
        }

        try
        {
            var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{keyId}:{keySecret}"));
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.razorpay.com/v1/orders")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Basic", authHeader) },
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    amount = amountInPaise,
                    currency = "INR",
                    receipt = Guid.NewGuid().ToString().Substring(0, 40)
                }), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return (null, null, 0, $"Failed to create payment order: {err}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var razorpayData = JsonSerializer.Deserialize<JsonElement>(responseBody);
            var razorpayOrderId = razorpayData.GetProperty("id").GetString();
            
            order.RazorpayOrderId = razorpayOrderId;

            _context.Orders.Add(order);

            foreach (var item in cart.Items)
            {
                if (item.Product != null)
                {
                    item.Product.Stock -= item.Quantity;
                }
            }
            
            // Clear the cart
            _context.Carts.Remove(cart);
            
            await _context.SaveChangesAsync();

            return (order.Id.ToString(), razorpayOrderId, totalAmount, null);
        }
        catch (DbUpdateException dbEx)
        {
            return (null, null, 0, $"Database Error: {dbEx.InnerException?.Message ?? dbEx.Message}");
        }
        catch (Exception ex)
        {
            return (null, null, 0, $"Server Error: {ex.Message}");
        }
    }

    public async Task<IEnumerable<Order>> GetUserOrdersAsync(string userId)
    {
        return await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetAllOrdersAsync()
    {
        return await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return false;

        order.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelOrderAsync(int orderId, string userId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null || order.Status != "Pending") return false;

        order.Status = "Cancelled";
        foreach (var item in order.Items)
        {
            if (item.Product != null)
            {
                item.Product.Stock += item.Quantity;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> VerifyPaymentAsync(int orderId, string paymentId, string signature)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null || string.IsNullOrEmpty(order.RazorpayOrderId))
            return false;

        var keySecret = _configuration["RAZORPAY_KEY_SECRET"] ?? Environment.GetEnvironmentVariable("RAZORPAY_KEY_SECRET");
        if (string.IsNullOrEmpty(keySecret))
            return false;

        var payload = $"{order.RazorpayOrderId}|{paymentId}";
        var secretBytes = Encoding.UTF8.GetBytes(keySecret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using (var hmac = new HMACSHA256(secretBytes))
        {
            byte[] hash = hmac.ComputeHash(payloadBytes);
            string expectedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();

            if (expectedSignature == signature)
            {
                order.Status = "Processing";
                order.RazorpayPaymentId = paymentId;
                await _context.SaveChangesAsync();
                return true;
            }
        }
        return false;
    }
}
