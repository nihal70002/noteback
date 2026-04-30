using Note.Backend.Models;

namespace Note.Backend.Services;

public interface IOrderService
{
    Task<(string? OrderId, string? Error)> CheckoutAsync(string cartId, string userId, ShippingDetails shippingDetails);
    Task<IEnumerable<Order>> GetUserOrdersAsync(string userId);
    Task<IEnumerable<Order>> GetAllOrdersAsync();
    Task<bool> UpdateOrderStatusAsync(int orderId, string status);
    Task<bool> CancelOrderAsync(int orderId, string userId);
}
