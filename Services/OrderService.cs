using Microsoft.EntityFrameworkCore;
using Note.Backend.Data;
using Note.Backend.Models;

namespace Note.Backend.Services;

public class OrderService : IOrderService
{
    private readonly NoteDbContext _context;

    public OrderService(NoteDbContext context)
    {
        _context = context;
    }

    public async Task<(string? OrderId, string? Error)> CheckoutAsync(string cartId, string userId, ShippingDetails shippingDetails)
    {
        if (string.IsNullOrWhiteSpace(shippingDetails.FullName) ||
            string.IsNullOrWhiteSpace(shippingDetails.PhoneNumber) ||
            string.IsNullOrWhiteSpace(shippingDetails.AddressLine1) ||
            string.IsNullOrWhiteSpace(shippingDetails.City) ||
            string.IsNullOrWhiteSpace(shippingDetails.State) ||
            string.IsNullOrWhiteSpace(shippingDetails.Pincode))
        {
            return (null, "Please fill all required shipping fields.");
        }

        var primaryPhone = shippingDetails.PhoneNumber.Trim();
        if (primaryPhone.Length < 10 || primaryPhone.Length > 15)
        {
            return (null, "Primary phone number is invalid.");
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
            return (null, "Cart is empty or not found.");
        }

        foreach (var item in cart.Items)
        {
            if (item.Product == null)
            {
                return (null, "A product in your cart is no longer available.");
            }

            if (item.Quantity > item.Product.Stock)
            {
                return (null, $"Only {item.Product.Stock} item(s) available for {item.Product.Name}.");
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
                return (null, "Coupon code is invalid.");
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

        return (order.Id.ToString(), null);
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
}
