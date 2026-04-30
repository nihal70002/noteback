using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Note.Backend.Data;
using Note.Backend.Models;
using System.Security.Claims;

namespace Note.Backend.Controllers;

[ApiController]
[Route("api/products/{productId}/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly NoteDbContext _context;

    public ReviewsController(NoteDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetReviews(string productId)
    {
        var reviews = await _context.ProductReviews
            .Where(r => r.ProductId == productId)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.Rating,
                r.Comment,
                r.CreatedAt,
                Username = r.User != null ? r.User.Username : "Customer"
            })
            .ToListAsync();

        return Ok(reviews);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> UpsertReview(string productId, [FromBody] ReviewRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (request.Rating < 1 || request.Rating > 5)
        {
            return BadRequest(new { Message = "Rating must be between 1 and 5." });
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null) return NotFound(new { Message = "Product not found." });

        var review = await _context.ProductReviews.FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);
        if (review == null)
        {
            review = new ProductReview { ProductId = productId, UserId = userId };
            _context.ProductReviews.Add(review);
        }

        review.Rating = request.Rating;
        review.Comment = request.Comment.Trim();
        review.CreatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await UpdateProductRating(productId);

        return Ok(new { Message = "Review saved." });
    }

    private async Task UpdateProductRating(string productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return;

        var reviews = await _context.ProductReviews
            .Where(r => r.ProductId == productId)
            .ToListAsync();

        product.ReviewCount = reviews.Count;
        product.AverageRating = reviews.Count == 0 ? 0 : Math.Round((decimal)reviews.Average(r => r.Rating), 1);

        await _context.SaveChangesAsync();
    }
}

public class ReviewRequest
{
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}
