using Mdar.Core.Enums;
using Mdar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskStatus = Mdar.Core.Enums.TaskStatus;

namespace Mdar.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── GET /api/dashboard/stats ──────────────────────────────────────────────
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var userId      = GetUserId();
        var weekStart   = DateTime.UtcNow.Date.AddDays(-6);

        var totalTasks     = await _db.Tasks.CountAsync(t => t.UserId == userId);
        var completedTasks = await _db.Tasks.CountAsync(t => t.UserId == userId
                                && t.Status == TaskStatus.Completed);
        var pendingTasks   = await _db.Tasks.CountAsync(t => t.UserId == userId
                                && t.Status == TaskStatus.Pending);
        var totalNodes     = await _db.ThinkingCards.CountAsync(c => c.UserId == userId);
        var activeSessions = await _db.ThinkingBoards.CountAsync(b => b.UserId == userId);

        // تقدم الأسبوع — نجلب التواريخ ثم نجمّعها في الذاكرة لتجنب مشاكل ترجمة EF
        var completedDates = await _db.Tasks
            .Where(t => t.UserId == userId
                     && t.Status == TaskStatus.Completed
                     && t.CompletedAt >= weekStart)
            .Select(t => t.CompletedAt!.Value)
            .ToListAsync();

        var weeklyProgress = completedDates
            .GroupBy(d => d.Date)
            .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), count = g.Count() })
            .OrderBy(x => x.date)
            .ToList();

        // أهم الأهداف النشطة
        var topValues = await _db.Goals
            .AsNoTracking()
            .Where(g => g.UserId == userId && g.Status == GoalStatus.Active)
            .OrderByDescending(g => g.ProgressPercentage)
            .Take(5)
            .Select(g => new
            {
                g.Id,
                g.Title,
                g.ProgressPercentage,
                status = g.Status.ToString(),
                g.TargetDate
            })
            .ToListAsync();

        return Ok(new
        {
            totalTasks,
            completedTasks,
            pendingTasks,
            totalNodes,
            activeSessions,
            weeklyProgress,
            topValues
        });
    }

    // ── GET /api/dashboard/activity ───────────────────────────────────────────
    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity()
    {
        var userId = GetUserId();

        var taskActivity = await _db.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.UpdatedAt)
            .Take(5)
            .Select(t => new
            {
                type      = "task",
                id        = t.Id,
                title     = t.Title,
                action    = t.Status == TaskStatus.Completed ? "completed" : "updated",
                timestamp = t.UpdatedAt
            })
            .ToListAsync();

        var nodeActivity = await _db.ThinkingCards
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .Take(5)
            .Select(c => new
            {
                type      = "node",
                id        = c.Id,
                title     = c.Title,
                action    = "updated",
                timestamp = c.UpdatedAt
            })
            .ToListAsync();

        // دمج القائمتين وترتيبهما بالأحدث
        var combined = taskActivity
            .Cast<dynamic>()
            .Concat(nodeActivity.Cast<dynamic>())
            .OrderByDescending(x => (DateTime)x.timestamp)
            .Take(10)
            .ToList();

        return Ok(combined);
    }
}
