using Note.Backend.Models;

namespace Note.Backend.Services;

public interface IOrderService
{
    Task<(string? RazorpayOrderId, decimal Amount, string? Error)> CheckoutAsync(string cartId, string userId, ShippingDetails shippingDetails);
    Task<IEnumerable<Order>> GetUserOrdersAsync(string userId);
    Task<IEnumerable<Order>> GetAllOrdersAsync();
    Task<bool> UpdateOrderStatusAsync(int orderId, string status);
    Task<bool> CancelOrderAsync(int orderId, string userId);
    Task<VerifyPaymentResult> VerifyPaymentAsync(string userId, VerifyPaymentRequest request);
}

public record VerifyPaymentResult(bool Success, Order? Order = null, string? Error = null);
