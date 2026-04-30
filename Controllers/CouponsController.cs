using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Note.Backend.Data;

namespace Note.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CouponsController : ControllerBase
{
    private readonly NoteDbContext _context;

    public CouponsController(NoteDbContext context)
    {
        _context = context;
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> ValidateCoupon(string code)
    {
        var normalizedCode = code.Trim().ToUpper();
        var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == normalizedCode && c.IsActive);

        if (coupon == null)
        {
            return NotFound(new { Message = "Coupon code is invalid." });
        }

        return Ok(new { coupon.Code, coupon.DiscountPercent });
    }
}
