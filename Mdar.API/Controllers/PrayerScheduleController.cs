using Mdar.API.DTOs.PrayerSchedule;
using Mdar.API.Extensions;
using Mdar.Core.Entities.Tasks;
using Mdar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mdar.API.Controllers;

/// <summary>
/// Controller إدارة جداول أوقات الصلاة.
/// يتيح للمستخدم إدخال أو تحديث أوقات الصلاة اليومية
/// التي يعتمد عليها محرك الأولويات لتحديد الفترة الزمنية الحالية.
/// </summary>
[ApiController]
[Route("api/prayer-schedules")]
[Authorize]
[Produces("application/json")]
public sealed class PrayerScheduleController : ControllerBase
{
    private readonly AppDbContext _db;

    public PrayerScheduleController(AppDbContext db)
    {
        _db = db;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // POST /api/prayer-schedules — إنشاء أو تحديث جدول يوم
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// يُنشئ جدول أوقات الصلاة ليوم محدد أو يُحدِّثه إذا كان موجوداً.
    /// (Upsert: Create or Update)
    /// </summary>
    /// <param name="request">أوقات الصلوات الخمس + وقت الشروق</param>
    /// <param name="ct">رمز الإلغاء</param>
    [HttpPost]
    [ProducesResponseType(typeof(PrayerScheduleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(PrayerScheduleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertSchedule(
        [FromBody] UpsertPrayerScheduleRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        var targetDate = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // Upsert: هل يوجد جدول لهذا اليوم؟
        var existing = await _db.DailyPrayerSchedules
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Date == targetDate, ct);

        bool isNew = existing is null;

        var schedule = existing ?? new DailyPrayerSchedule
        {
            UserId = userId,
            Date   = targetDate
        };

        // تحديث الأوقات
        schedule.FajrTime    = request.FajrTime;
        schedule.SunriseTime = request.SunriseTime;
        schedule.DhuhrTime   = request.DhuhrTime;
        schedule.AsrTime     = request.AsrTime;
        schedule.MaghribTime = request.MaghribTime;
        schedule.IshaTime    = request.IshaTime;
        schedule.Source      = request.Source;
        schedule.IsAutoFetched = false;

        if (isNew)
            _db.DailyPrayerSchedules.Add(schedule);

        await _db.SaveChangesAsync(ct);

        var response = PrayerScheduleResponse.From(schedule);

        return isNew
            ? CreatedAtAction(nameof(GetSchedule), new { date = targetDate.ToString("yyyy-MM-dd") }, response)
            : Ok(response);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GET /api/prayer-schedules/today — جدول اليوم
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// يُعيد جدول أوقات الصلاة لليوم الحالي مع الفترة الزمنية الحالية.
    /// </summary>
    [HttpGet("today")]
    [ProducesResponseType(typeof(PrayerScheduleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTodaySchedule(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var today  = DateOnly.FromDateTime(DateTime.UtcNow);

        var schedule = await _db.DailyPrayerSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Date == today, ct);

        if (schedule is null)
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title  = "جدول غير موجود",
                Detail = $"لم يُسجَّل جدول أوقات صلاة لتاريخ {today:yyyy-MM-dd}. " +
                         "أضف الجدول عبر POST /api/prayer-schedules."
            });

        return Ok(PrayerScheduleResponse.From(schedule));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GET /api/prayer-schedules/{date} — جدول يوم محدد
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// يُعيد جدول أوقات الصلاة لتاريخ محدد.
    /// </summary>
    /// <param name="date">التاريخ بصيغة yyyy-MM-dd</param>
    /// <param name="ct">رمز الإلغاء</param>
    [HttpGet("{date}")]
    [ProducesResponseType(typeof(PrayerScheduleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSchedule(string date, CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var targetDate))
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title  = "تنسيق التاريخ غير صالح",
                Detail = "يجب أن يكون التاريخ بصيغة yyyy-MM-dd. مثال: 2026-03-14"
            });

        var userId = User.GetUserId();

        var schedule = await _db.DailyPrayerSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Date == targetDate, ct);

        if (schedule is null)
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title  = "جدول غير موجود",
                Detail = $"لم يُسجَّل جدول أوقات صلاة لتاريخ {date}."
            });

        return Ok(PrayerScheduleResponse.From(schedule, DateTime.UtcNow));
    }
}
