using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Note.Backend.Data;
using Note.Backend.Models;

namespace Note.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly NoteDbContext _context;

    public AdminController(NoteDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);
        var totalOrders = await _context.Orders.CountAsync();
        var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
        var totalExpenses = await _context.BusinessExpenses.SumAsync(e => (decimal?)e.Amount) ?? 0m;
        var netProfit = totalRevenue - totalExpenses;
        var totalUsers = await _context.Users.CountAsync(u => u.Role != "Admin");
        var totalAdmins = await _context.Users.CountAsync(u => u.Role == "Admin");
        var blockedUsers = await _context.Users.CountAsync(u => u.IsBlocked);
        
        var today = DateTime.UtcNow.Date;
        var recentOrders = await _context.Orders
            .Where(o => o.OrderDate >= today.AddDays(-7))
            .ToListAsync();

        var salesData = recentOrders
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new {
                Date = g.Key.ToString("MMM dd"),
                Revenue = g.Sum(o => o.TotalAmount),
                Orders = g.Count()
            })
            .OrderBy(s => s.Date)
            .ToList();

        // Fill in missing days
        var last7Days = Enumerable.Range(0, 7)
            .Select(i => today.AddDays(-i))
            .Reverse()
            .Select(d => {
                var existing = salesData.FirstOrDefault(s => s.Date == d.ToString("MMM dd"));
                return existing ?? new { Date = d.ToString("MMM dd"), Revenue = 0m, Orders = 0 };
            });

        return Ok(new
        {
            TotalRevenue = totalRevenue,
            TotalExpenses = totalExpenses,
            NetProfit = netProfit,
            TotalOrders = totalOrders,
            PendingOrders = pendingOrders,
            TotalUsers = totalUsers,
            TotalAdmins = totalAdmins,
            BlockedUsers = blockedUsers,
            SalesChart = last7Days
        });
    }

    [HttpGet("expenses")]
    public async Task<IActionResult> GetExpenses()
    {
        var expenses = await _context.BusinessExpenses
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.Id)
            .ToListAsync();

        var totalExpenses = expenses.Sum(e => e.Amount);
        var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);

        return Ok(new
        {
            Items = expenses,
            Totals = new
            {
                TotalRevenue = totalRevenue,
                TotalExpenses = totalExpenses,
                NetProfit = totalRevenue - totalExpenses
            }
        });
    }

    [HttpPost("expenses")]
    public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { Message = "Title is required." });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { Message = "Amount should be greater than zero." });
        }

        var expenseDate = request.ExpenseDate?.Date ?? DateTime.UtcNow.Date;
        if (expenseDate.Kind != DateTimeKind.Utc)
        {
            expenseDate = DateTime.SpecifyKind(expenseDate, DateTimeKind.Utc);
        }

        var expense = new BusinessExpense
        {
            Title = request.Title.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Other" : request.Category.Trim(),
            Amount = request.Amount,
            Notes = request.Notes?.Trim() ?? string.Empty,
            ExpenseDate = expenseDate,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.BusinessExpenses.Add(expense);
            await _context.SaveChangesAsync();
            return Ok(expense);
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, new { Message = "Could not save expense right now. Please restart backend and try again." });
        }
    }

    [HttpPut("expenses/{id}")]
    public async Task<IActionResult> UpdateExpense(int id, [FromBody] UpdateExpenseRequest request)
    {
        var expense = await _context.BusinessExpenses.FindAsync(id);
        if (expense == null)
        {
            return NotFound(new { Message = "Expense not found." });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { Message = "Title is required." });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { Message = "Amount should be greater than zero." });
        }

        var expenseDate = request.ExpenseDate?.Date ?? expense.ExpenseDate;
        if (expenseDate.Kind != DateTimeKind.Utc)
        {
            expenseDate = DateTime.SpecifyKind(expenseDate, DateTimeKind.Utc);
        }

        expense.Title = request.Title.Trim();
        expense.Category = string.IsNullOrWhiteSpace(request.Category) ? "Other" : request.Category.Trim();
        expense.Amount = request.Amount;
        expense.Notes = request.Notes?.Trim() ?? string.Empty;
        expense.ExpenseDate = expenseDate;

        try
        {
            await _context.SaveChangesAsync();
            return Ok(expense);
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, new { Message = "Could not update expense. Please try again." });
        }
    }

    [HttpDelete("expenses/{id}")]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        var expense = await _context.BusinessExpenses.FindAsync(id);
        if (expense == null)
        {
            return NotFound(new { Message = "Expense not found." });
        }

        _context.BusinessExpenses.Remove(expense);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Expense deleted." });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] string? role)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(normalizedSearch) ||
                u.Email.ToLower().Contains(normalizedSearch));
        }

        if (!string.IsNullOrWhiteSpace(role) && role != "All")
        {
            query = query.Where(u => u.Role == role);
        }

        var users = await query
            .GroupJoin(
                _context.Orders,
                user => user.Id,
                order => order.UserId,
                (user, orders) => new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.Role,
                    user.IsBlocked,
                    OrderCount = orders.Count(),
                    TotalSpent = orders.Sum(o => (decimal?)o.TotalAmount) ?? 0m,
                    LastOrderDate = orders.Max(o => (DateTime?)o.OrderDate)
                })
            .OrderByDescending(u => u.LastOrderDate ?? DateTime.MinValue)
            .ThenBy(u => u.Username)
            .ToListAsync();

        return Ok(users);
    }

    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> UpdateUserRole(string id, [FromBody] UpdateUserRoleRequest request)
    {
        if (request.Role != "Admin" && request.Role != "User")
        {
            return BadRequest(new { Message = "Role must be Admin or User." });
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound(new { Message = "User not found." });

        user.Role = request.Role;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "User role updated." });
    }

    [HttpPut("users/{id}/block")]
    public async Task<IActionResult> UpdateUserBlockStatus(string id, [FromBody] UpdateUserBlockStatusRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound(new { Message = "User not found." });

        user.IsBlocked = request.IsBlocked;
        await _context.SaveChangesAsync();

        return Ok(new { Message = request.IsBlocked ? "User blocked." : "User unblocked." });
    }

    [HttpGet("users/{id}/orders")]
    public async Task<IActionResult> GetUserOrders(string id)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == id);
        if (!userExists) return NotFound(new { Message = "User not found." });

        var orders = await _context.Orders
            .Where(o => o.UserId == id)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return Ok(orders);
    }
}

public class UpdateUserRoleRequest
{
    public string Role { get; set; } = "User";
}

public class UpdateUserBlockStatusRequest
{
    public bool IsBlocked { get; set; }
}

public class CreateExpenseRequest
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Other";
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public DateTime? ExpenseDate { get; set; }
}

public class UpdateExpenseRequest
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Other";
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public DateTime? ExpenseDate { get; set; }
}
