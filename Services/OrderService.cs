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
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<OrderService> _logger;
    private static readonly HttpClient _httpClient = new();

    public OrderService(
        NoteDbContext context,
        IConfiguration configuration,
        IWhatsAppService whatsAppService,
        ILogger<OrderService> _logger)
    {
        _context = context;
        _configuration = configuration;
        _whatsAppService = whatsAppService;
        this._logger = _logger;
    }

    public async Task<(string? RazorpayOrderId, decimal Amount, string? Error)> CheckoutAsync(string cartId, string userId, ShippingDetails shippingDetails)
    {
        var validationError = ValidateShippingDetails(shippingDetails);
        if (validationError != null)
        {
            return (null, 0, validationError);
        }

        var cart = await GetCartAsync(cartId);
        if (cart == null || !cart.Items.Any())
        {
            return (null, 0, "Cart is empty or not found.");
        }

        var pricing = await CalculateCartPricingAsync(cart, shippingDetails.CouponCode);
        if (pricing.Error != null)
        {
            return (null, 0, pricing.Error);
        }

        var amountInPaise = (int)Math.Round(pricing.TotalAmount * 100);
        if (amountInPaise < 100)
        {
            return (null, 0, "Amount must be at least INR 1.00");
        }

        var credentials = GetRazorpayCredentials();
        if (credentials == null)
        {
            return (null, 0, "Payment gateway configuration is missing.");
        }

        try
        {
            var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{credentials.Value.KeyId}:{credentials.Value.KeySecret}"));
            var receipt = $"cart_{cartId}_{Guid.NewGuid():N}";
            if (receipt.Length > 40)
            {
                receipt = receipt[..40];
            }

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.razorpay.com/v1/orders")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Basic", authHeader) },
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    amount = amountInPaise,
                    currency = "INR",
                    receipt,
                    notes = new
                    {
                        cartId,
                        userId
                    }
                }), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return (null, 0, $"Failed to create payment order: {err}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var razorpayData = JsonSerializer.Deserialize<JsonElement>(responseBody);
            var razorpayOrderId = razorpayData.GetProperty("id").GetString();

            return (razorpayOrderId, pricing.TotalAmount, null);
        }
        catch (Exception ex)
        {
            return (null, 0, $"Server Error: {ex.Message}");
        }
    }

    public async Task<IEnumerable<Order>> GetUserOrdersAsync(string userId)
    {
        return await _context.Orders
            .Where(o => o.UserId == userId && o.PaymentStatus == "Paid")
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetAllOrdersAsync()
    {
        return await _context.Orders
            .Where(o => o.PaymentStatus == "Paid")
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
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId && o.PaymentStatus == "Paid");

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

    public async Task<VerifyPaymentResult> VerifyPaymentAsync(string userId, VerifyPaymentRequest request)
    {
        _logger.LogInformation("[VERIFY-PAYMENT] Incoming request - UserId: {UserId}, CartId: {CartId}, PaymentId: {PaymentId}", 
            userId, request.CartId, request.RazorpayPaymentId);

        // Check for existing order with this payment ID
        var existingOrder = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.RazorpayPaymentId == request.RazorpayPaymentId);

        if (existingOrder != null)
        {
            _logger.LogInformation("[VERIFY-PAYMENT] Existing order found - OrderId: {OrderId}, UserId: {UserId}, ExistingUserId: {ExistingUserId}", 
                existingOrder.Id, userId, existingOrder.UserId);
            
            return existingOrder.UserId == userId
                ? new VerifyPaymentResult(true, existingOrder)
                : new VerifyPaymentResult(false, Error: "Payment has already been used.");
        }

        // Validate signature
        if (!IsValidRazorpaySignature(request.RazorpayOrderId, request.RazorpayPaymentId, request.RazorpaySignature))
        {
            _logger.LogWarning("[VERIFY-PAYMENT] Invalid signature - OrderId: {OrderId}, PaymentId: {PaymentId}", 
                request.RazorpayOrderId, request.RazorpayPaymentId);
            return new VerifyPaymentResult(false, Error: "Payment verification failed.");
        }

        // Validate shipping details
        if (request.ShippingDetails is null)
        {
            _logger.LogWarning("[VERIFY-PAYMENT] Missing shipping details - CartId: {CartId}", request.CartId);
            return new VerifyPaymentResult(false, Error: "Shipping details are required.");
        }

        var validationError = ValidateShippingDetails(request.ShippingDetails);
        if (validationError != null)
        {
            _logger.LogWarning("[VERIFY-PAYMENT] Invalid shipping details - CartId: {CartId}, Error: {Error}", 
                request.CartId, validationError);
            return new VerifyPaymentResult(false, Error: validationError);
        }

        // Validate cart ID
        if (string.IsNullOrWhiteSpace(request.CartId))
        {
            _logger.LogWarning("[VERIFY-PAYMENT] Cart ID is null or empty");
            return new VerifyPaymentResult(false, Error: "Cart is required.");
        }

        // Fetch and validate cart
        _logger.LogInformation("[VERIFY-PAYMENT] Looking up cart - CartId: {CartId}", request.CartId);
        var cart = await GetCartAsync(request.CartId);
        
        if (cart == null)
        {
            _logger.LogError("[VERIFY-PAYMENT] Cart not found - CartId: {CartId}, UserId: {UserId}", 
                request.CartId, userId);
            return new VerifyPaymentResult(false, Error: "Cart not found.");
        }

        if (!cart.Items.Any())
        {
            _logger.LogError("[VERIFY-PAYMENT] Cart is empty - CartId: {CartId}, UserId: {UserId}", 
                request.CartId, userId);
            return new VerifyPaymentResult(false, Error: "Cart is empty.");
        }

        _logger.LogInformation("[VERIFY-PAYMENT] Cart loaded successfully - CartId: {CartId}, ItemCount: {ItemCount}", 
            request.CartId, cart.Items.Count);

        // Calculate pricing
        var pricing = await CalculateCartPricingAsync(cart, request.ShippingDetails.CouponCode);
        if (pricing.Error != null)
        {
            _logger.LogWarning("[VERIFY-PAYMENT] Pricing calculation failed - CartId: {CartId}, Error: {Error}", 
                request.CartId, pricing.Error);
            return new VerifyPaymentResult(false, Error: pricing.Error);
        }

        // Validate amount
        var amountInPaise = (int)Math.Round(pricing.TotalAmount * 100);
        var razorpayOrderValid = await ValidateRazorpayOrderAsync(request.RazorpayOrderId, amountInPaise);
        if (!razorpayOrderValid)
        {
            _logger.LogError("[VERIFY-PAYMENT] Razorpay amount mismatch - OrderId: {OrderId}, ExpectedAmount: {ExpectedAmount}", 
                request.RazorpayOrderId, amountInPaise);
            return new VerifyPaymentResult(false, Error: "Payment amount verification failed.");
        }

        // Build order
        var order = BuildPaidOrder(userId, request, cart, pricing);
        _logger.LogInformation("[VERIFY-PAYMENT] Order object created - UserId: {UserId}, TotalAmount: {TotalAmount}", 
            userId, pricing.TotalAmount);

        try
        {
            // Add order to database
            _context.Orders.Add(order);
            _logger.LogInformation("[VERIFY-PAYMENT] Order added to context - Order will be saved");

            // Update stock
            foreach (var item in cart.Items)
            {
                if (item.Product != null)
                {
                    item.Product.Stock -= item.Quantity;
                    _logger.LogInformation("[VERIFY-PAYMENT] Stock updated - ProductId: {ProductId}, NewStock: {NewStock}", 
                        item.ProductId, item.Product.Stock);
                }
            }

            // Remove cart
            _context.Carts.Remove(cart);
            _logger.LogInformation("[VERIFY-PAYMENT] Cart marked for deletion - CartId: {CartId}", request.CartId);

            // CRITICAL: Save all changes to database
            await _context.SaveChangesAsync();
            _logger.LogInformation("[VERIFY-PAYMENT] SaveChangesAsync executed successfully - OrderId: {OrderId}, CartId: {CartId}", 
                order.Id, request.CartId);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "[VERIFY-PAYMENT] Database error during SaveChangesAsync - CartId: {CartId}, PaymentId: {PaymentId}, Error: {ErrorMessage}", 
                request.CartId, request.RazorpayPaymentId, ex.Message);
            _context.ChangeTracker.Clear();

            // Check for duplicate order
            var duplicateOrder = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.RazorpayPaymentId == request.RazorpayPaymentId);

            if (duplicateOrder != null && duplicateOrder.UserId == userId)
            {
                _logger.LogInformation("[VERIFY-PAYMENT] Duplicate order detected and matched - OrderId: {OrderId}", 
                    duplicateOrder.Id);
                return new VerifyPaymentResult(true, duplicateOrder);
            }

            return new VerifyPaymentResult(false, Error: "Could not create order for this payment.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VERIFY-PAYMENT] Unexpected error - CartId: {CartId}, PaymentId: {PaymentId}, Error: {ErrorMessage}", 
                request.CartId, request.RazorpayPaymentId, ex.Message);
            return new VerifyPaymentResult(false, Error: $"Server error: {ex.Message}");
        }

        // Send notifications
        try
        {
            await SendOrderWhatsAppNotificationsAsync(order, pricing.TotalAmount);
            await SendAdminPaymentVerifiedWhatsAppAsync(order);
            _logger.LogInformation("[VERIFY-PAYMENT] Notifications sent - OrderId: {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VERIFY-PAYMENT] Notification error (non-blocking) - OrderId: {OrderId}", order.Id);
        }

        _logger.LogInformation("[VERIFY-PAYMENT] SUCCESS - Order created - OrderId: {OrderId}, UserId: {UserId}, CartId: {CartId}, Amount: {Amount}", 
            order.Id, userId, request.CartId, pricing.TotalAmount);
        
        return new VerifyPaymentResult(true, order);
    }

    private async Task<Cart?> GetCartAsync(string cartId)
    {
        if (string.IsNullOrWhiteSpace(cartId))
        {
            return null;
        }

        return await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.Id == cartId);
    }

    private async Task<CartPricing> CalculateCartPricingAsync(Cart cart, string? rawCouponCode)
    {
        foreach (var item in cart.Items)
        {
            if (item.Product == null)
            {
                return new CartPricing(Error: "A product in your cart is no longer available.");
            }

            if (item.Quantity > item.Product.Stock)
            {
                return new CartPricing(Error: $"Only {item.Product.Stock} item(s) available for {item.Product.Name}.");
            }
        }

        var subtotal = cart.Items.Sum(i => i.Quantity * GetEffectiveProductPrice(i.Product));
        var couponCode = rawCouponCode?.Trim().ToUpper();
        var discountAmount = 0m;

        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode && c.IsActive);
            if (coupon == null)
            {
                return new CartPricing(Error: "Coupon code is invalid.");
            }

            discountAmount = Math.Round(subtotal * (coupon.DiscountPercent / 100m), 2);
        }

        var shippingSettings = await _context.ShippingSettings
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync();

        if (shippingSettings == null || !shippingSettings.Enabled)
        {
            shippingSettings = new ShippingSettings();
        }

        var payableSubtotal = subtotal - discountAmount;
        var qualifiesForAmountFreeShipping = payableSubtotal >= 450m;
        decimal shippingFee;
        if (shippingSettings.FreeShippingType == "amount")
        {
            shippingFee = qualifiesForAmountFreeShipping || payableSubtotal >= shippingSettings.FreeShippingAmount
                ? 0m
                : shippingSettings.StandardShippingFee;
        }
        else
        {
            var totalItems = cart.Items.Sum(i => i.Quantity);
            shippingFee = qualifiesForAmountFreeShipping || totalItems >= shippingSettings.FreeShippingThreshold
                ? 0m
                : shippingSettings.StandardShippingFee;
        }

        var totalAmount = subtotal - discountAmount + shippingFee;
        return new CartPricing(subtotal, discountAmount, shippingFee, totalAmount, couponCode);
    }

    private static string? ValidateShippingDetails(ShippingDetails shippingDetails)
    {
        if (string.IsNullOrWhiteSpace(shippingDetails.FullName) ||
            string.IsNullOrWhiteSpace(shippingDetails.PhoneNumber) ||
            string.IsNullOrWhiteSpace(shippingDetails.AddressLine1) ||
            string.IsNullOrWhiteSpace(shippingDetails.City) ||
            string.IsNullOrWhiteSpace(shippingDetails.State) ||
            string.IsNullOrWhiteSpace(shippingDetails.Pincode))
        {
            return "Please fill all required shipping fields.";
        }

        var primaryPhone = shippingDetails.PhoneNumber.Trim();
        return primaryPhone.Length < 10 || primaryPhone.Length > 15
            ? "Primary phone number is invalid."
            : null;
    }

    private Order BuildPaidOrder(string userId, VerifyPaymentRequest request, Cart cart, CartPricing pricing)
    {
        var shippingDetails = request.ShippingDetails;
        var fullAddress = string.Join(", ", new[]
        {
            shippingDetails.AddressLine1?.Trim(),
            shippingDetails.AddressLine2?.Trim(),
            shippingDetails.City?.Trim(),
            shippingDetails.State?.Trim(),
            shippingDetails.Pincode?.Trim()
        }.Where(part => !string.IsNullOrWhiteSpace(part)));

        return new Order
        {
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Subtotal = pricing.Subtotal,
            DiscountAmount = pricing.DiscountAmount,
            ShippingFee = pricing.ShippingFee,
            CouponCode = pricing.CouponCode,
            TotalAmount = pricing.TotalAmount,
            Status = "Pending",
            PaymentStatus = "Paid",
            FullName = (shippingDetails.FullName ?? string.Empty).Trim(),
            PhoneNumber = (shippingDetails.PhoneNumber ?? string.Empty).Trim(),
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
            RazorpayOrderId = request.RazorpayOrderId,
            RazorpayPaymentId = request.RazorpayPaymentId,
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = GetEffectiveProductPrice(i.Product),
                SelectedChoicesJson = i.SelectedChoicesJson
            }).ToList()
        };
    }

    private bool IsValidRazorpaySignature(string razorpayOrderId, string paymentId, string signature)
    {
        var credentials = GetRazorpayCredentials();
        if (credentials == null)
        {
            return false;
        }

        var payload = $"{razorpayOrderId}|{paymentId}";
        var secretBytes = Encoding.UTF8.GetBytes(credentials.Value.KeySecret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        var expectedSignature = Convert.ToHexString(hash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }

    private async Task<bool> ValidateRazorpayOrderAsync(string razorpayOrderId, int amountInPaise)
    {
        var credentials = GetRazorpayCredentials();
        if (credentials == null)
        {
            return false;
        }

        try
        {
            var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{credentials.Value.KeyId}:{credentials.Value.KeySecret}"));
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.razorpay.com/v1/orders/{razorpayOrderId}")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Basic", authHeader) }
            };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var razorpayData = JsonSerializer.Deserialize<JsonElement>(responseBody);
            var razorpayAmount = razorpayData.GetProperty("amount").GetInt32();
            var currency = razorpayData.GetProperty("currency").GetString();

            return razorpayAmount == amountInPaise && currency == "INR";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not validate Razorpay order {RazorpayOrderId}", razorpayOrderId);
            return false;
        }
    }

    private (string KeyId, string KeySecret)? GetRazorpayCredentials()
    {
        var keyId = (_configuration["RAZORPAY_KEY_ID"] ?? Environment.GetEnvironmentVariable("RAZORPAY_KEY_ID"))?.Trim();
        var keySecret = (_configuration["RAZORPAY_KEY_SECRET"] ?? Environment.GetEnvironmentVariable("RAZORPAY_KEY_SECRET"))?.Trim();

        return string.IsNullOrEmpty(keyId) || string.IsNullOrEmpty(keySecret)
            ? null
            : (keyId, keySecret);
    }

    private static decimal GetEffectiveProductPrice(Product? product)
    {
        if (product == null)
        {
            return 0;
        }

        return product.IsPack || product.Name.Contains("combo", StringComparison.OrdinalIgnoreCase)
            ? 499m
            : product.Price;
    }

    private async Task SendOrderWhatsAppNotificationsAsync(Order order, decimal totalAmount)
    {
        try
        {
            var adminPhone = (_configuration["ADMIN_WHATSAPP_PHONE"]
                ?? Environment.GetEnvironmentVariable("ADMIN_WHATSAPP_PHONE")
                ?? Environment.GetEnvironmentVariable("WHATSAPP_ADMIN_PHONE"))?.Trim();

            var itemSummary = string.Join(", ", order.Items.Select(item => $"{item.ProductId} x {item.Quantity}"));
            var adminMessage =
                $"New paid order received on Papercues.\n" +
                $"Order ID: #{order.Id}\n" +
                $"Customer: {order.FullName}\n" +
                $"Phone: {order.PhoneNumber}\n" +
                $"Amount: INR {totalAmount:F2}\n" +
                $"Items: {itemSummary}\n" +
                $"Address: {order.DeliveryAddress}";

            if (!string.IsNullOrWhiteSpace(adminPhone))
            {
                var adminResult = await _whatsAppService.SendMessageAsync(adminPhone, adminMessage);
                if (!adminResult.Success)
                {
                    _logger.LogError("Admin WhatsApp notification failed for order {OrderId}: {Error}", order.Id, adminResult.ErrorMessage);
                }
            }
            else
            {
                _logger.LogWarning("Admin WhatsApp notification skipped for order {OrderId}: ADMIN_WHATSAPP_PHONE is not configured.", order.Id);
            }

            if (!string.IsNullOrWhiteSpace(order.PhoneNumber))
            {
                var customerMessage =
                    $"Hi {order.FullName}, your Papercues order #{order.Id} has been placed successfully.\n" +
                    $"Amount: INR {totalAmount:F2}\n" +
                    $"We will notify you when your order is processed.";

                var customerResult = await _whatsAppService.SendMessageAsync(order.PhoneNumber, customerMessage);
                if (!customerResult.Success)
                {
                    _logger.LogError("Customer WhatsApp notification failed for order {OrderId}: {Error}", order.Id, customerResult.ErrorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp notification flow failed for order {OrderId}. Order creation was not affected.", order.Id);
        }
    }

    private async Task SendAdminPaymentVerifiedWhatsAppAsync(Order order)
    {
        const string adminPhone = "+917591907000";
        var message =
            $"Payment verified for Papercues order.\n" +
            $"Order ID: #{order.Id}\n" +
            $"Customer: {order.FullName}\n" +
            $"Total Amount: INR {order.TotalAmount:F2}";

        try
        {
            var result = await _whatsAppService.SendMessageAsync(adminPhone, message);

            if (result.Success)
            {
                _logger.LogInformation("Twilio SID: {Sid}", result.MessageSid);
                return;
            }

            _logger.LogError("Twilio WhatsApp send failed for order {OrderId}: {Error}", order.Id, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twilio exception details while sending payment notification for order {OrderId}: {Message}", order.Id, ex.Message);
        }
    }

    private record CartPricing(
        decimal Subtotal = 0,
        decimal DiscountAmount = 0,
        decimal ShippingFee = 0,
        decimal TotalAmount = 0,
        string? CouponCode = null,
        string? Error = null);
}
