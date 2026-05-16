using Note.Backend.Models;

namespace Note.Backend.Services;

public interface IAdminCartAnalyticsService
{
    Task<CartAnalyticsResponse> GetCartAnalyticsAsync(CartAnalyticsQuery query);
    Task<CartAnalyticsResponse> GetAbandonedCartsAsync(CartAnalyticsQuery query);
    Task<IReadOnlyList<TopCartProductDto>> GetTopCartProductsAsync(int take);
    Task<UserCartDetailsDto?> GetUserCartAsync(string userId);
}

public class CartAnalyticsQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class CartAnalyticsResponse
{
    public CartAnalyticsSummaryDto Summary { get; set; } = new();
    public IReadOnlyList<CartAnalyticsItemDto> Items { get; set; } = [];
    public IReadOnlyList<TopCartProductDto> TopProducts { get; set; } = [];
    public IReadOnlyList<DailyCartActivityDto> DailyActivity { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

public class CartAnalyticsSummaryDto
{
    public int TotalCarts { get; set; }
    public int ActiveCarts { get; set; }
    public int AbandonedCarts { get; set; }
    public int OrderedCarts { get; set; }
    public string TopBook { get; set; } = "N/A";
}

public class CartAnalyticsItemDto
{
    public string CartId { get; set; } = string.Empty;
    public int? CartItemId { get; set; }
    public string? UserId { get; set; }
    public string UserPhoneNumber { get; set; } = "Guest";
    public string UserRole { get; set; } = "Guest";
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime AddedAt { get; set; }
    public DateTime? OrderedAt { get; set; }
}

public class TopCartProductDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalValue { get; set; }
}

public class DailyCartActivityDto
{
    public string Date { get; set; } = string.Empty;
    public int Carts { get; set; }
    public int Quantity { get; set; }
}

public class UserCartDetailsDto
{
    public string UserId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public bool IsBlocked { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<CartAnalyticsItemDto> Items { get; set; } = [];
    public decimal ActiveCartTotal { get; set; }
    public int ActiveCartItems { get; set; }
}
