using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Note.Backend.Data;
using Note.Backend.Models;
using System.Security.Claims;

namespace Note.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly NoteDbContext _context;

    public WishlistController(NoteDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetWishlist()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var items = await _context.WishlistItems
            .Where(w => w.UserId == userId)
            .Include(w => w.Product)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{productId}/exists")]
    public async Task<IActionResult> Exists(string productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var exists = await _context.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == productId);
        return Ok(new { Exists = exists });
    }

    [HttpPost("{productId}")]
    public async Task<IActionResult> Add(string productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
        if (!productExists) return NotFound(new { Message = "Product not found." });

        var exists = await _context.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == productId);
        if (!exists)
        {
            _context.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId });
            await _context.SaveChangesAsync();
        }

        return Ok(new { Message = "Added to wishlist." });
    }

    [HttpDelete("{productId}")]
    public async Task<IActionResult> Remove(string productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var item = await _context.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
        if (item != null)
        {
            _context.WishlistItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        return Ok(new { Message = "Removed from wishlist." });
    }
}
