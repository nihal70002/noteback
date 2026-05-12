using Microsoft.AspNetCore.Mvc;

namespace Note.Backend.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    [HttpGet("cors")]
    public IActionResult TestCors()
    {
        return Ok(new { 
            message = "CORS is working!",
            timestamp = DateTime.UtcNow,
            origin = Request.Headers["Origin"].ToString()
        });
    }
    
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { 
            status = "healthy",
            timestamp = DateTime.UtcNow
        });
    }
}
