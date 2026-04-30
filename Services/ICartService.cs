using Note.Backend.Models;

namespace Note.Backend.Services;

public interface ICartService
{
    Task<Cart> GetCartAsync(string cartId);
    Task<(Cart? Cart, string? Error)> AddItemToCartAsync(string cartId, string productId, int quantity);
    Task<(Cart? Cart, string? Error)> UpdateItemQuantityAsync(string cartId, int itemId, int quantity);
    Task<Cart> RemoveItemFromCartAsync(string cartId, int itemId);
}
