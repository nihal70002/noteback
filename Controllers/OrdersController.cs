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
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
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

        var (razorpayOrderId, amount, error) = await _orderService.CheckoutAsync(cartId, userId, details);

        if (razorpayOrderId == null)
        {
            return BadRequest(new { Message = error ?? "Cart is empty or not found" });
        }

        return Ok(new { 
            Message = "Razorpay order created successfully",
            RazorpayOrderId = razorpayOrderId,
            Amount = amount,
            Currency = "INR"
        });
    }

    [HttpPost("verify-payment")]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest request)
    {
        _logger.LogInformation("[VERIFY-PAYMENT-ENDPOINT] Received request - CartId: {CartId}, PaymentId: {PaymentId}", 
            request.CartId, request.RazorpayPaymentId);

        if (string.IsNullOrEmpty(request.CartId) ||
            string.IsNullOrEmpty(request.RazorpayPaymentId) || 
            string.IsNullOrEmpty(request.RazorpayOrderId) || 
            string.IsNullOrEmpty(request.RazorpaySignature))
        {
            _logger.LogWarning("[VERIFY-PAYMENT-ENDPOINT] Missing payment details");
            return BadRequest(new { Message = "Missing payment details" });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("[VERIFY-PAYMENT-ENDPOINT] User not authenticated");
            return Unauthorized();
        }

        _logger.LogInformation("[VERIFY-PAYMENT-ENDPOINT] User authenticated - UserId: {UserId}", userId);

        var result = await _orderService.VerifyPaymentAsync(userId, request);
        
        if (!result.Success)
        {
            _logger.LogError("[VERIFY-PAYMENT-ENDPOINT] Verification failed - Error: {Error}", result.Error);
            return BadRequest(new { Message = result.Error ?? "Payment verification failed" });
        }

        _logger.LogInformation("[VERIFY-PAYMENT-ENDPOINT] Payment verified successfully - OrderId: {OrderId}, UserId: {UserId}", 
            result.Order?.Id, userId);

        return Ok(new
        {
            Message = "Payment verified successfully",
            OrderId = result.Order?.Id
        });
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
