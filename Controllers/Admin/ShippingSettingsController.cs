using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Note.Backend.Data;
using Note.Backend.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

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
        try
        {
            Console.WriteLine($"[GET ShippingSettings] Request received at {DateTime.UtcNow}");
            
            var settings = await _context.ShippingSettings
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                Console.WriteLine("[GET ShippingSettings] No settings found, returning defaults");
                // Return default settings if none exist
                settings = new ShippingSettings();
            }

            Console.WriteLine($"[GET ShippingSettings] Returning settings: {JsonSerializer.Serialize(settings)}");
            return Ok(new { success = true, data = settings });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GET ShippingSettings] Error: {ex.Message}");
            Console.WriteLine($"[GET ShippingSettings] StackTrace: {ex.StackTrace}");
            return StatusCode(500, new { success = false, message = "Failed to fetch shipping settings", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SaveShippingSettings([FromBody] ShippingSettings settings)
    {
        try
        {
            Console.WriteLine($"[POST ShippingSettings] Request received at {DateTime.UtcNow}");
            Console.WriteLine($"[POST ShippingSettings] Request body: {JsonSerializer.Serialize(settings)}");

            if (settings == null)
            {
                Console.WriteLine("[POST ShippingSettings] Request body is null");
                return BadRequest(new { success = false, message = "Request body is required" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                Console.WriteLine($"[POST ShippingSettings] Validation failed: {string.Join(", ", errors)}");
                return BadRequest(new { success = false, message = "Validation failed", errors = errors });
            }

            var existingSettings = await _context.ShippingSettings
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            if (existingSettings != null)
            {
                Console.WriteLine($"[POST ShippingSettings] Updating existing settings ID: {existingSettings.Id}");
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
                Console.WriteLine("[POST ShippingSettings] Creating new settings");
                // Create new settings
                settings.Id = 0; // Let database generate ID
                settings.UpdatedAt = DateTime.UtcNow;
                _context.ShippingSettings.Add(settings);
            }

            var rowsAffected = await _context.SaveChangesAsync();
            Console.WriteLine($"[POST ShippingSettings] Database changes saved. Rows affected: {rowsAffected}");

            return Ok(new { success = true, message = "Shipping settings saved successfully", data = settings });
        }
        catch (DbUpdateException dbEx)
        {
            Console.WriteLine($"[POST ShippingSettings] Database error: {dbEx.Message}");
            Console.WriteLine($"[POST ShippingSettings] Inner exception: {dbEx.InnerException?.Message}");
            return StatusCode(500, new { success = false, message = "Database error occurred", error = dbEx.InnerException?.Message ?? dbEx.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[POST ShippingSettings] General error: {ex.Message}");
            Console.WriteLine($"[POST ShippingSettings] StackTrace: {ex.StackTrace}");
            return StatusCode(500, new { success = false, message = "An error occurred while saving shipping settings", error = ex.Message });
        }
    }
}
