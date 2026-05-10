using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Note.Backend.Data;
using Note.Backend.Models;

namespace Note.Backend.Controllers;

[ApiController]
[Route("api/shipping")]
public class ShippingController : ControllerBase
{
    private readonly NoteDbContext _context;

    public ShippingController(NoteDbContext context)
    {
        _context = context;
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetShippingSettings()
    {
        var settings = await _context.ShippingSettings
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync();

        if (settings == null)
        {
            // Return default settings if none exist
            settings = new ShippingSettings();
        }

        return Ok(settings);
    }
}
