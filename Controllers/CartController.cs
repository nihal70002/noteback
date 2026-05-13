using Microsoft.AspNetCore.Mvc;
using Note.Backend.Models;
using Note.Backend.Services;

namespace Note.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet("{cartId}")]
    public async Task<ActionResult<Cart>> GetCart(string cartId)
    {
        var cart = await _cartService.GetCartAsync(cartId);
        return Ok(cart);
    }

    [HttpPost("{cartId}/items")]
    public async Task<ActionResult<Cart>> AddItemToCart(string cartId, [FromBody] AddCartItemRequest request)
    {
        var (cart, error) = await _cartService.AddItemToCartAsync(cartId, request.ProductId, request.Quantity, request.SelectedChoices);
        if (cart == null) return BadRequest(new { Message = error });
        return Ok(cart);
    }

    [HttpPut("{cartId}/items/{itemId}")]
    public async Task<ActionResult<Cart>> UpdateItemQuantity(string cartId, int itemId, [FromBody] UpdateCartItemRequest request)
    {
        var (cart, error) = await _cartService.UpdateItemQuantityAsync(cartId, itemId, request.Quantity);
        if (cart == null) return BadRequest(new { Message = error });
        return Ok(cart);
    }

    [HttpDelete("{cartId}/items/{itemId}")]
    public async Task<ActionResult<Cart>> RemoveItemFromCart(string cartId, int itemId)
    {
        var cart = await _cartService.RemoveItemFromCartAsync(cartId, itemId);
        return Ok(cart);
    }

    [HttpPost("{cartId}/replace-with-combo")]
    public async Task<ActionResult<Cart>> ReplaceWithCombo(string cartId, [FromBody] ReplaceWithComboRequest request)
    {
        var (cart, error) = await _cartService.ReplaceWithComboAsync(cartId, request.ComboProductId, request.SelectedChoices);
        if (cart == null) return BadRequest(new { Message = error });
        return Ok(cart);
    }
}

public class AddCartItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public List<string>? SelectedChoices { get; set; }
}

public class UpdateCartItemRequest
{
    public int Quantity { get; set; }
}

public class ReplaceWithComboRequest
{
    public string ComboProductId { get; set; } = string.Empty;
    public List<string>? SelectedChoices { get; set; }
}
