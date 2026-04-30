using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Note.Backend.Services;
using System.Security.Claims;
using Note.Backend.Models;

namespace Note.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var orders = await _orderService.GetUserOrdersAsync(userId);
        return Ok(orders);
    }

    [HttpPost("checkout/{cartId}")]
    public async Task<IActionResult> Checkout(string cartId, [FromBody] ShippingDetails details)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var (orderId, error) = await _orderService.CheckoutAsync(cartId, userId, details);

        if (orderId == null)
        {
            return BadRequest(new { Message = error ?? "Cart is empty or not found" });
        }

        return Ok(new { Message = "Order placed successfully", OrderId = orderId });
    }
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _orderService.GetAllOrdersAsync();
        return Ok(orders);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var success = await _orderService.UpdateOrderStatusAsync(id, request.Status);
        if (!success)
        {
            return NotFound(new { Message = "Order not found" });
        }

        return Ok(new { Message = "Order status updated successfully" });
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var success = await _orderService.CancelOrderAsync(id, userId);
        if (!success)
        {
            return BadRequest(new { Message = "Only pending orders can be cancelled." });
        }

        return Ok(new { Message = "Order cancelled successfully" });
    }
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
