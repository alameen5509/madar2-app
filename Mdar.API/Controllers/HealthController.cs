using Mdar.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Mdar.API.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db) => _db = db;

    // ── GET /api/health/status ────────────────────────────────────────────────
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var dbStatus = "connected";
        try
        {
            await _db.Database.CanConnectAsync();
        }
        catch
        {
            dbStatus = "disconnected";
        }

        return Ok(new
        {
            status   = "ok",
            version  = "1.1.0",
            database = dbStatus,
            timestamp = DateTime.UtcNow
        });
    }
}
