using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Note.Backend.Data;
using Note.Backend.Models;
using System.Security.Claims;

namespace Note.Backend.Controllers.Admin;

[ApiController]
[Route("api/admin/shipping-settings")]
[Authorize(Roles = "Admin")]
public class ShippingSettingsController : ControllerBase
{
    private readonly NoteDbContext _context;

    public ShippingSettingsController(NoteDbContext context)
    {
        _context = context;
    }

    [HttpGet]
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

    [HttpPost]
    public async Task<IActionResult> SaveShippingSettings([FromBody] ShippingSettings settings)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate settings
        if (settings.StandardShippingFee < 0)
        {
            return BadRequest(new { Message = "Standard shipping fee must be non-negative." });
        }

        if (settings.FreeShippingThreshold < 1)
        {
            return BadRequest(new { Message = "Free shipping threshold must be at least 1." });
        }

        if (settings.FreeShippingAmount < 0)
        {
            return BadRequest(new { Message = "Free shipping amount must be non-negative." });
        }

        if (!new[] { "quantity", "amount" }.Contains(settings.FreeShippingType))
        {
            return BadRequest(new { Message = "Free shipping type must be 'quantity' or 'amount'." });
        }

        var existingSettings = await _context.ShippingSettings
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync();

        if (existingSettings != null)
        {
            // Update existing settings
            existingSettings.Enabled = settings.Enabled;
            existingSettings.StandardShippingFee = settings.StandardShippingFee;
            existingSettings.FreeShippingThreshold = settings.FreeShippingThreshold;
            existingSettings.FreeShippingAmount = settings.FreeShippingAmount;
            existingSettings.FreeShippingType = settings.FreeShippingType;
            existingSettings.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new settings
            settings.Id = 0; // Ensure new ID
            settings.UpdatedAt = DateTime.UtcNow;
            _context.ShippingSettings.Add(settings);
        }

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Shipping settings saved successfully." });
    }
}
