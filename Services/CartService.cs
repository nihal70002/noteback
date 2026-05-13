using Microsoft.EntityFrameworkCore;
using Note.Backend.Data;
using Note.Backend.Models;

namespace Note.Backend.Services;

public class CartService : ICartService
{
    private readonly NoteDbContext _context;

    public CartService(NoteDbContext context)
    {
        _context = context;
    }

    public async Task<Cart> GetCartAsync(string cartId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart == null)
        {
            cart = new Cart { Id = cartId };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
        }

        return cart;
    }

    public async Task<(Cart? Cart, string? Error)> AddItemToCartAsync(string cartId, string productId, int quantity, List<string>? selectedChoices = null)
    {
        if (quantity < 1) quantity = 1;

        var product = await _context.Products.FindAsync(productId);
        if (product == null) return (null, "Product not found.");
        if (product.Stock <= 0) return (null, "This product is out of stock.");

        if (product.IsPack && product.PackSize.HasValue)
        {
            var required = product.PackSize.Value;
            if (selectedChoices == null || selectedChoices.Count != required)
            {
                return (null, $"Please select exactly {required} item(s) for this pack.");
            }

            var validChoiceIds = await _context.PackChoices
                .Where(pc => pc.PackProductId == productId)
                .Select(pc => pc.ChoiceProductId)
                .ToListAsync();

            foreach (var choiceId in selectedChoices)
            {
                if (!validChoiceIds.Contains(choiceId))
                {
                    return (null, "One or more selected pack items are invalid.");
                }

                var choiceProduct = await _context.Products.FindAsync(choiceId);
                if (choiceProduct == null || choiceProduct.Stock <= 0)
                {
                    return (null, $"Selected item '{choiceProduct?.Name ?? choiceId}' is out of stock.");
                }
            }
        }

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart == null)
        {
            cart = new Cart { Id = cartId };
            _context.Carts.Add(cart);
        }

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        var nextQuantity = quantity + (existingItem?.Quantity ?? 0);
        if (nextQuantity > product.Stock)
        {
            return (null, $"Only {product.Stock} item(s) available.");
        }

        var choicesJson = selectedChoices != null ? System.Text.Json.JsonSerializer.Serialize(selectedChoices) : null;

        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
            existingItem.SelectedChoicesJson = choicesJson ?? existingItem.SelectedChoicesJson;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                ProductId = productId,
                Quantity = quantity,
                SelectedChoicesJson = choicesJson
            });
        }

        await _context.SaveChangesAsync();
        return (await GetCartAsync(cartId), null);
    }

    public async Task<(Cart? Cart, string? Error)> UpdateItemQuantityAsync(string cartId, int itemId, int quantity)
    {
        if (quantity < 1) quantity = 1;

        var item = await _context.CartItems
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.CartId == cartId);
        if (item == null) return (null, "Cart item not found.");
        if (item.Product != null && quantity > item.Product.Stock)
        {
            return (null, $"Only {item.Product.Stock} item(s) available.");
        }

        item.Quantity = quantity;
        await _context.SaveChangesAsync();

        return (await GetCartAsync(cartId), null);
    }

    public async Task<Cart> RemoveItemFromCartAsync(string cartId, int itemId)
    {
        var item = await _context.CartItems.FirstOrDefaultAsync(i => i.Id == itemId && i.CartId == cartId);
        if (item != null)
        {
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        return await GetCartAsync(cartId);
    }

    public async Task<(Cart? Cart, string? Error)> ReplaceWithComboAsync(string cartId, string comboProductId, List<string>? selectedChoices = null)
    {
        // Clear all existing items in the cart
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart == null)
        {
            cart = new Cart { Id = cartId };
            _context.Carts.Add(cart);
        }

        // Remove all existing items
        _context.CartItems.RemoveRange(cart.Items);

        // Add the combo product
        var (result, error) = await AddItemToCartAsync(cartId, comboProductId, 1, selectedChoices);
        
        if (error != null)
        {
            return (null, error);
        }

        return (result, null);
    }
}
