using Microsoft.EntityFrameworkCore;
using Note.Backend.Data;
using Note.Backend.Models;

namespace Note.Backend.Services;

public class AdminCartAnalyticsService : IAdminCartAnalyticsService
{
    private readonly NoteDbContext _context;
    private static readonly TimeSpan AbandonedAfter = TimeSpan.FromHours(24);

    public AdminCartAnalyticsService(NoteDbContext context)
    {
        _context = context;
    }

    public async Task<CartAnalyticsResponse> GetCartAnalyticsAsync(CartAnalyticsQuery query)
    {
        var items = await GetAnalyticsItemsAsync(query.Status);
        return await BuildResponseAsync(items, query);
    }

    public async Task<CartAnalyticsResponse> GetAbandonedCartsAsync(CartAnalyticsQuery query)
    {
        query.Status = "Abandoned";
        var items = await GetAnalyticsItemsAsync(query.Status);
        return await BuildResponseAsync(items, query);
    }

    public async Task<IReadOnlyList<TopCartProductDto>> GetTopCartProductsAsync(int take)
    {
        var safeTake = Math.Clamp(take, 1, 25);
        var items = await GetAnalyticsItemsAsync(null);

        return items
            .GroupBy(i => new { i.ProductId, i.ProductName, i.ProductImage })
            .Select(g => new TopCartProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                ProductImage = g.Key.ProductImage,
                Quantity = g.Sum(i => i.Quantity),
                TotalValue = g.Sum(i => i.Total)
            })
            .OrderByDescending(i => i.Quantity)
            .ThenBy(i => i.ProductName)
            .Take(safeTake)
            .ToList();
    }

    public async Task<UserCartDetailsDto?> GetUserCartAsync(string userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return null;
        }

        var items = (await GetAnalyticsItemsAsync(null))
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.AddedAt)
            .ToList();

        var activeItems = items.Where(i => i.Status == "Active").ToList();

        return new UserCartDetailsDto
        {
            UserId = user.Id,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            IsBlocked = user.IsBlocked,
            CreatedAt = user.CreatedAt,
            Items = items,
            ActiveCartItems = activeItems.Sum(i => i.Quantity),
            ActiveCartTotal = activeItems.Sum(i => i.Total)
        };
    }

    private async Task<CartAnalyticsResponse> BuildResponseAsync(
        IReadOnlyList<CartAnalyticsItemDto> sourceItems,
        CartAnalyticsQuery query)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var filtered = ApplySearch(sourceItems, query.Search);
        var sorted = ApplySorting(filtered, query.SortBy, query.SortDirection).ToList();
        var totalItems = sorted.Count;
        var topProducts = await GetTopCartProductsAsync(5);

        return new CartAnalyticsResponse
        {
            Summary = await GetSummaryAsync(topProducts),
            Items = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            TopProducts = topProducts,
            DailyActivity = BuildDailyActivity(sourceItems),
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };
    }

    private async Task<IReadOnlyList<CartAnalyticsItemDto>> GetAnalyticsItemsAsync(string? status)
    {
        var cutoff = DateTime.UtcNow.Subtract(AbandonedAfter);
        var includeActive = string.IsNullOrWhiteSpace(status) || status.Equals("Active", StringComparison.OrdinalIgnoreCase);
        var includeAbandoned = string.IsNullOrWhiteSpace(status) || status.Equals("Abandoned", StringComparison.OrdinalIgnoreCase);
        var includeOrdered = string.IsNullOrWhiteSpace(status) || status.Equals("Ordered", StringComparison.OrdinalIgnoreCase);
        var items = new List<CartAnalyticsItemDto>();

        if (includeActive || includeAbandoned)
        {
            var cartItems = await (
                from cart in _context.Carts
                    .AsNoTracking()
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                from item in cart.Items
                join user in _context.Users.AsNoTracking()
                    on cart.UserId equals user.Id into userGroup
                from user in userGroup.DefaultIfEmpty()
                where !cart.IsOrdered
                select new
                {
                    Cart = cart,
                    Item = item,
                    User = user,
                    Product = item.Product
                })
                .ToListAsync();

            items.AddRange(cartItems
                .Select(row =>
                {
                    var itemStatus = row.Cart.AddedAt < cutoff ? "Abandoned" : "Active";
                    return new CartAnalyticsItemDto
                    {
                        CartId = row.Cart.Id,
                        CartItemId = row.Item.Id,
                        UserId = row.Cart.UserId,
                        UserPhoneNumber = row.User?.PhoneNumber ?? "Guest",
                        UserRole = row.User?.Role ?? "Guest",
                        ProductId = row.Item.ProductId,
                        ProductName = row.Product?.Name ?? row.Item.ProductId,
                        ProductImage = row.Product?.Image ?? string.Empty,
                        ProductCategory = row.Product?.Category ?? string.Empty,
                        Quantity = row.Item.Quantity,
                        Price = GetEffectiveProductPrice(row.Product),
                        Total = row.Item.Quantity * GetEffectiveProductPrice(row.Product),
                        Status = itemStatus,
                        AddedAt = row.Cart.AddedAt,
                        OrderedAt = row.Cart.OrderedAt
                    };
                })
                .Where(i => (includeActive && i.Status == "Active") || (includeAbandoned && i.Status == "Abandoned")));
        }

        if (includeOrdered)
        {
            var orderedItems = await (
                from order in _context.Orders
                    .AsNoTracking()
                    .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                from item in order.Items
                where order.PaymentStatus == "Paid"
                select new CartAnalyticsItemDto
                {
                    CartId = "order_" + order.Id,
                    CartItemId = item.Id,
                    UserId = order.UserId,
                    UserPhoneNumber = order.PhoneNumber,
                    UserRole = "User",
                    ProductId = item.ProductId,
                    ProductName = item.Product != null ? item.Product.Name : item.ProductId,
                    ProductImage = item.Product != null ? item.Product.Image : string.Empty,
                    ProductCategory = item.Product != null ? item.Product.Category : string.Empty,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    Total = item.Price * item.Quantity,
                    Status = "Ordered",
                    AddedAt = order.OrderDate,
                    OrderedAt = order.OrderDate
                })
                .ToListAsync();

            items.AddRange(orderedItems);
        }

        return items;
    }

    private async Task<CartAnalyticsSummaryDto> GetSummaryAsync(IReadOnlyList<TopCartProductDto> topProducts)
    {
        var cutoff = DateTime.UtcNow.Subtract(AbandonedAfter);
        var activeCarts = await _context.Carts.CountAsync(c => c.Items.Any() && !c.IsOrdered && c.AddedAt >= cutoff);
        var abandonedCarts = await _context.Carts.CountAsync(c => c.Items.Any() && !c.IsOrdered && c.AddedAt < cutoff);
        var orderedCarts = await _context.Orders.CountAsync(o => o.PaymentStatus == "Paid");

        return new CartAnalyticsSummaryDto
        {
            TotalCarts = activeCarts + abandonedCarts + orderedCarts,
            ActiveCarts = activeCarts,
            AbandonedCarts = abandonedCarts,
            OrderedCarts = orderedCarts,
            TopBook = topProducts.FirstOrDefault()?.ProductName ?? "N/A"
        };
    }

    private static IReadOnlyList<CartAnalyticsItemDto> ApplySearch(
        IReadOnlyList<CartAnalyticsItemDto> items,
        string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return items;
        }

        var term = search.Trim();
        return items
            .Where(i =>
                i.UserPhoneNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.ProductName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.ProductCategory.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.CartId.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static IEnumerable<CartAnalyticsItemDto> ApplySorting(
        IReadOnlyList<CartAnalyticsItemDto> items,
        string? sortBy,
        string? sortDirection)
    {
        var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return (sortBy ?? "addedAt").ToLowerInvariant() switch
        {
            "user" => descending ? items.OrderByDescending(i => i.UserPhoneNumber) : items.OrderBy(i => i.UserPhoneNumber),
            "product" => descending ? items.OrderByDescending(i => i.ProductName) : items.OrderBy(i => i.ProductName),
            "quantity" => descending ? items.OrderByDescending(i => i.Quantity) : items.OrderBy(i => i.Quantity),
            "price" => descending ? items.OrderByDescending(i => i.Price) : items.OrderBy(i => i.Price),
            "total" => descending ? items.OrderByDescending(i => i.Total) : items.OrderBy(i => i.Total),
            "status" => descending ? items.OrderByDescending(i => i.Status) : items.OrderBy(i => i.Status),
            _ => descending ? items.OrderByDescending(i => i.AddedAt) : items.OrderBy(i => i.AddedAt)
        };
    }

    private static IReadOnlyList<DailyCartActivityDto> BuildDailyActivity(IReadOnlyList<CartAnalyticsItemDto> items)
    {
        var today = DateTime.UtcNow.Date;
        var start = today.AddDays(-6);
        var groups = items
            .Where(i => i.AddedAt.Date >= start && i.AddedAt.Date <= today)
            .GroupBy(i => i.AddedAt.Date)
            .ToDictionary(g => g.Key, g => new
            {
                Carts = g.Select(i => i.CartId).Distinct().Count(),
                Quantity = g.Sum(i => i.Quantity)
            });

        return Enumerable.Range(0, 7)
            .Select(offset => start.AddDays(offset))
            .Select(date => new DailyCartActivityDto
            {
                Date = date.ToString("MMM dd"),
                Carts = groups.TryGetValue(date, out var group) ? group.Carts : 0,
                Quantity = groups.TryGetValue(date, out group) ? group.Quantity : 0
            })
            .ToList();
    }

    private static decimal GetEffectiveProductPrice(Product? product)
    {
        if (product == null)
        {
            return 0m;
        }

        return product.IsPack || product.Name.Contains("combo", StringComparison.OrdinalIgnoreCase)
            ? 499m
            : product.Price;
    }
}
