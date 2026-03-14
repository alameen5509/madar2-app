using Mdar.API.DTOs.Canvas;
using Mdar.Core.Entities.Canvas;
using Mdar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Mdar.API.Controllers;

/// <summary>
/// إدارة النسخ الاحتياطية المشفرة للوحات InfiniteCanvas.
///
/// مبدأ Zero-Knowledge:
///   الخادم يخزّن البيانات المشفرة كما وصلت — لا يحاول فك تشفيرها.
///   التحقق الوحيد: Magic Bytes "MDAR" للتأكد من صحة تنسيق الملف.
///   كلمة المرور لا تُرسَل إلى الخادم في أي وقت.
///
/// Endpoints:
///   GET    /api/canvas/backups           ← قائمة النسخ (بدون البيانات المشفرة)
///   POST   /api/canvas/backups           ← رفع نسخة جديدة (multipart/form-data)
///   GET    /api/canvas/backups/{id}/download ← تنزيل الملف المشفر
///   PATCH  /api/canvas/backups/{id}      ← تحديث الملاحظة فقط
///   DELETE /api/canvas/backups/{id}      ← حذف نسخة
/// </summary>
[ApiController]
[Route("api/canvas/backups")]
[Authorize]
public class CanvasBackupsController : ControllerBase
{
    private readonly AppDbContext _db;

    // حدود الحماية
    private const int  MaxBackupsPerUser  = 20;
    private const long MaxBackupSizeBytes = 10 * 1024 * 1024; // 10 MB

    // Magic Bytes للتحقق من صحة الملف
    private static readonly byte[] MdarMagic = [(byte)'M', (byte)'D', (byte)'A', (byte)'R'];

    public CanvasBackupsController(AppDbContext db) => _db = db;

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── GET /api/canvas/backups ────────────────────────────────────────────────
    /// <summary>
    /// قائمة النسخ الاحتياطية للمستخدم الحالي.
    /// مرتبة من الأحدث إلى الأقدم.
    /// لا تُعيد البيانات المشفرة — فقط البيانات الوصفية.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<BackupResponse>>> GetBackups()
    {
        var userId = GetUserId();

        var backups = await _db.CanvasBackups
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BackupResponse
            {
                Id        = b.Id,
                FileName  = b.FileName,
                SizeBytes = b.SizeBytes,
                Label     = b.Label,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();

        return Ok(backups);
    }

    // ── POST /api/canvas/backups ───────────────────────────────────────────────
    /// <summary>
    /// رفع نسخة احتياطية مشفرة إلى السحابة.
    /// يتحقق من:
    ///   1. عدم تجاوز الحد الأقصى (20 نسخة)
    ///   2. حجم الملف (≤ 10 MB)
    ///   3. Magic Bytes "MDAR" (تنسيق صالح)
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(10_485_760)] // 10 MB
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BackupResponse>> UploadBackup(
        [FromForm] UploadBackupRequest req)
    {
        var userId = GetUserId();

        // التحقق من عدد النسخ الحالية
        var count = await _db.CanvasBackups.CountAsync(b => b.UserId == userId);
        if (count >= MaxBackupsPerUser)
            return BadRequest(new
            {
                error = $"وصلت إلى الحد الأقصى ({MaxBackupsPerUser} نسخة احتياطية). احذف نسخاً قديمة أولاً."
            });

        // التحقق من حجم الملف
        if (req.File.Length > MaxBackupSizeBytes)
            return BadRequest(new { error = "حجم الملف يتجاوز الحد الأقصى (10 MB)." });

        if (req.File.Length < 33) // أصغر حجم ممكن: 4+1+16+12 = 33
            return BadRequest(new { error = "الملف صغير جداً — تنسيق غير صالح." });

        // قراءة البيانات
        using var ms = new MemoryStream();
        await req.File.CopyToAsync(ms);
        var bytes = ms.ToArray();

        // التحقق من Magic Bytes — الخادم لا يفك التشفير لكن يتحقق من التنسيق
        if (!bytes.AsSpan(0, 4).SequenceEqual(MdarMagic.AsSpan()))
            return BadRequest(new
            {
                error = "ملف غير صالح — يجب أن يكون بتنسيق .mdar الصادر من مدار."
            });

        var backup = new CanvasBackup
        {
            UserId        = userId,
            FileName      = SanitizeFileName(req.File.FileName),
            EncryptedData = bytes,
            SizeBytes     = bytes.Length,
            Label         = req.Label?.Trim()
        };

        _db.CanvasBackups.Add(backup);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBackups), new { id = backup.Id }, new BackupResponse
        {
            Id        = backup.Id,
            FileName  = backup.FileName,
            SizeBytes = backup.SizeBytes,
            Label     = backup.Label,
            CreatedAt = backup.CreatedAt
        });
    }

    // ── GET /api/canvas/backups/{id}/download ──────────────────────────────────
    /// <summary>
    /// تنزيل الملف المشفر.
    /// يُعيد الملف الثنائي بالضبط كما رُفع — الخادم لا يُعالج المحتوى.
    /// </summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> DownloadBackup(Guid id)
    {
        var userId = GetUserId();

        var backup = await _db.CanvasBackups
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (backup is null) return NotFound();

        return File(backup.EncryptedData, "application/octet-stream", backup.FileName);
    }

    // ── PATCH /api/canvas/backups/{id} ────────────────────────────────────────
    /// <summary>
    /// تحديث ملاحظة النسخة الاحتياطية فقط.
    /// لا يسمح بتعديل البيانات المشفرة.
    /// </summary>
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<BackupResponse>> UpdateLabel(
        Guid id,
        [FromBody] UpdateBackupLabelRequest req)
    {
        var userId = GetUserId();

        var backup = await _db.CanvasBackups
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (backup is null) return NotFound();

        backup.Label = req.Label?.Trim();
        await _db.SaveChangesAsync();

        return Ok(new BackupResponse
        {
            Id        = backup.Id,
            FileName  = backup.FileName,
            SizeBytes = backup.SizeBytes,
            Label     = backup.Label,
            CreatedAt = backup.CreatedAt
        });
    }

    // ── DELETE /api/canvas/backups/{id} ───────────────────────────────────────
    /// <summary>حذف نسخة احتياطية (Soft Delete)</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteBackup(Guid id)
    {
        var userId = GetUserId();

        var backup = await _db.CanvasBackups
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (backup is null) return NotFound();

        backup.IsDeleted = true;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// يُنظّف اسم الملف من مسارات خطرة (Path Traversal Prevention).
    /// </summary>
    private static string SanitizeFileName(string rawName)
    {
        var name = Path.GetFileName(rawName);
        // إزالة الأحرف غير المسموح بها في أسماء الملفات
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Where(c => !invalid.Contains(c)));
    }
}

/// <summary>DTO خاص بتحديث الملاحظة — inline لأنه صغير</summary>
public record UpdateBackupLabelRequest(
    [property: MaxLength(100)] string? Label
);
