using Mdar.Core.Entities.Canvas;
using Mdar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Mdar.API.Controllers;

/// <summary>
/// فحص صحة بيانات Canvas — Health Check
///
/// الفحوصات المتاحة:
///   1. OrphanedConnections  — بطاقات تشير لروابط معلقة    [TODO: يتطلب جدول CanvasConnections]
///   2. OrphanedTasks        — مهام مرتبطة ببطاقات محذوفة  [TODO: يتطلب عمود SourceCardId على Tasks]
///   3. CorruptedSnapshots   — لقطات بيانات JSON تالفة     [TODO: يتطلب جدول CanvasSnapshots]
///   4. DatabaseIntegrity    — DBCC CHECKDB / قابلية الاتصال
///   5. BackupMagicBytes     — نسخ احتياطية بتنسيق مكسور  ✅ مُطبَّق
///   6. StaleExports         — ملفات تصدير مؤقتة           [N/A: التصدير يتم في المتصفح]
///
/// Endpoints:
///   POST /api/canvas/health         ← تشغيل دورة فحص كاملة
///   GET  /api/canvas/health/logs    ← آخر 50 سجل فحص
/// </summary>
[ApiController]
[Route("api/canvas/health")]
[Authorize]
public class CanvasHealthController : ControllerBase
{
    private readonly AppDbContext _db;
    private static readonly byte[] MdarMagic = [(byte)'M', (byte)'D', (byte)'A', (byte)'R'];

    public CanvasHealthController(AppDbContext db) => _db = db;

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── POST /api/canvas/health ────────────────────────────────────────────────
    /// <summary>
    /// يُشغِّل دورة فحص كاملة ويُخزِّن النتائج في HealthCheckLogs.
    /// يعمل على بيانات المستخدم الحالي فقط.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<HealthCheckSummaryResponse>> RunHealthCheck()
    {
        var userId  = GetUserId();
        var results = new List<HealthCheckLog>();
        var now     = DateTime.UtcNow;

        // ── 1. فحص Magic Bytes للنسخ الاحتياطية ──────────────────────────────
        var backupCheck = await CheckBackupMagicBytes(userId, now);
        results.Add(backupCheck);

        // ── 2. فحص الروابط المعلقة ─────────────────────────────────────────────
        // TODO: يتطلب إضافة جدول CanvasConnections لاحقاً
        results.Add(new HealthCheckLog
        {
            CheckedAt   = now,
            CheckType   = nameof(CheckType.OrphanedConnections),
            Status      = "Skipped",
            IssuesFound = 0, IssuesFixed = 0,
            Details     = "جدول CanvasConnections غير موجود بعد — سيُفعَّل بعد إضافة ميزة الروابط.",
        });

        // ── 3. فحص المهام المعلقة ─────────────────────────────────────────────
        // TODO: يتطلب عمود SourceCardId على جدول Tasks
        results.Add(new HealthCheckLog
        {
            CheckedAt   = now,
            CheckType   = nameof(CheckType.OrphanedTasks),
            Status      = "Skipped",
            IssuesFound = 0, IssuesFixed = 0,
            Details     = "عمود SourceCardId غير موجود على Tasks بعد.",
        });

        // ── 4. فحص الـ Snapshots التالفة ──────────────────────────────────────
        // TODO: يتطلب جدول CanvasSnapshots
        results.Add(new HealthCheckLog
        {
            CheckedAt   = now,
            CheckType   = nameof(CheckType.CorruptedSnapshots),
            Status      = "Skipped",
            IssuesFound = 0, IssuesFixed = 0,
            Details     = "جدول CanvasSnapshots غير موجود بعد.",
        });

        // ── 5. فحص اتصال قاعدة البيانات ──────────────────────────────────────
        var dbCheck = await CheckDatabaseConnectivity(now);
        results.Add(dbCheck);

        // حفظ جميع نتائج الفحص
        _db.HealthCheckLogs.AddRange(results);
        await _db.SaveChangesAsync();

        var totalIssues = results.Sum(r => r.IssuesFound);
        var totalFixed  = results.Sum(r => r.IssuesFixed);
        var overallStatus = totalIssues == 0 ? "Passed"
                          : totalFixed == totalIssues ? "Fixed"
                          : "Warning";

        return Ok(new HealthCheckSummaryResponse
        {
            CheckedAt     = now,
            OverallStatus = overallStatus,
            TotalIssues   = totalIssues,
            TotalFixed    = totalFixed,
            Checks        = results.Select(r => new CheckResultDto
            {
                CheckType   = r.CheckType,
                Status      = r.Status,
                IssuesFound = r.IssuesFound,
                IssuesFixed = r.IssuesFixed,
                Details     = r.Details,
            }).ToList(),
        });
    }

    // ── GET /api/canvas/health/logs ────────────────────────────────────────────
    /// <summary>آخر 50 سجل فحص مرتبة من الأحدث للأقدم.</summary>
    [HttpGet("logs")]
    public async Task<ActionResult<List<HealthCheckLog>>> GetLogs()
    {
        var logs = await _db.HealthCheckLogs
            .OrderByDescending(h => h.CheckedAt)
            .Take(50)
            .ToListAsync();

        return Ok(logs);
    }

    // ── Private Checks ──────────────────────────────────────────────────────────

    /// <summary>
    /// يتحقق من أن كل نسخة احتياطية تبدأ بـ Magic Bytes "MDAR".
    /// النسخ الفاسدة: IsDeleted = true + تسجيل في الـ log.
    /// </summary>
    private async Task<HealthCheckLog> CheckBackupMagicBytes(Guid userId, DateTime now)
    {
        var backups = await _db.CanvasBackups
            .Where(b => b.UserId == userId)
            .Select(b => new { b.Id, b.FileName, Head = b.EncryptedData.Take(4).ToArray() })
            .ToListAsync();

        var corrupted = backups
            .Where(b => b.Head.Length < 4 || !b.Head.SequenceEqual(MdarMagic))
            .ToList();

        var details = new List<string>();

        foreach (var bad in corrupted)
        {
            // soft-delete النسخة الفاسدة
            var entity = await _db.CanvasBackups.FindAsync(bad.Id);
            if (entity != null) { entity.IsDeleted = true; }
            details.Add($"[حذف] {bad.FileName} — Magic bytes غير صالحة");
        }

        if (corrupted.Count > 0)
            await _db.SaveChangesAsync();

        return new HealthCheckLog
        {
            CheckedAt   = now,
            CheckType   = nameof(CheckType.BackupMagicBytes),
            Status      = corrupted.Count == 0 ? "Passed" : "Fixed",
            IssuesFound = corrupted.Count,
            IssuesFixed = corrupted.Count,
            Details     = corrupted.Count == 0
                ? $"تم فحص {backups.Count} نسخة — جميعها سليمة."
                : string.Join("\n", details),
        };
    }

    /// <summary>فحص اتصال قاعدة البيانات عبر استعلام بسيط.</summary>
    private async Task<HealthCheckLog> CheckDatabaseConnectivity(DateTime now)
    {
        try
        {
            // أبسط استعلام ممكن — يتحقق من الاتصال
            var canConnect = await _db.Database.CanConnectAsync();
            return new HealthCheckLog
            {
                CheckedAt   = now,
                CheckType   = nameof(CheckType.DatabaseIntegrity),
                Status      = canConnect ? "Passed" : "Failed",
                IssuesFound = canConnect ? 0 : 1,
                IssuesFixed = 0,
                Details     = canConnect ? "الاتصال بقاعدة البيانات سليم." : "⚠️ تعذّر الاتصال بقاعدة البيانات.",
            };
        }
        catch (Exception ex)
        {
            return new HealthCheckLog
            {
                CheckedAt   = now,
                CheckType   = nameof(CheckType.DatabaseIntegrity),
                Status      = "Failed",
                IssuesFound = 1, IssuesFixed = 0,
                Details     = $"استثناء: {ex.Message}",
            };
        }
    }
}

// ── Response DTOs ───────────────────────────────────────────────────────────────

public record HealthCheckSummaryResponse
{
    public DateTime          CheckedAt     { get; init; }
    public string            OverallStatus { get; init; } = string.Empty;
    public int               TotalIssues   { get; init; }
    public int               TotalFixed    { get; init; }
    public List<CheckResultDto> Checks     { get; init; } = [];
}

public record CheckResultDto
{
    public string  CheckType   { get; init; } = string.Empty;
    public string  Status      { get; init; } = string.Empty;
    public int     IssuesFound { get; init; }
    public int     IssuesFixed { get; init; }
    public string? Details     { get; init; }
}
