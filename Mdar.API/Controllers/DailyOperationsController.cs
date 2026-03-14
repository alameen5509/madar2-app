using Mdar.API.DTOs.DailyOps;
using Mdar.API.DTOs.PrayerSchedule;
using Mdar.API.DTOs.Tasks;
using Mdar.API.Extensions;
using Mdar.Core.Entities.Tasks;
using Mdar.Core.Enums;
using Mdar.Core.Interfaces;
using Mdar.Core.Models.Priority;
using Mdar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mdar.API.Controllers;

/// <summary>
/// Controller العمليات اليومية — الواجهة المباشرة لصفحة Daily Ops.
///
/// يوفر نقطتَي دخول مُخصَّصتَين للواجهة الأمامية:
///
///   GET  /api/dailyoperations/focus
///     → يُعيد المهام المرتبة للفترة الزمنية المطلوبة
///       مع بيانات الجدول الصلوي للعداد التنازلي
///
///   POST /api/dailyoperations/quick-add
///     → إضافة مهمة سريعة من FAB Modal
///       مع حساب وزنها الأولوي فوراً
/// </summary>
[ApiController]
[Route("api/dailyoperations")]
[Authorize]
[Produces("application/json")]
public sealed class DailyOperationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPriorityEngineService _priorityEngine;
    private readonly IPrayerTimeService _prayerTimeService;

    public DailyOperationsController(
        AppDbContext db,
        IPriorityEngineService priorityEngine,
        IPrayerTimeService prayerTimeService)
    {
        _db             = db;
        _priorityEngine = priorityEngine;
        _prayerTimeService = prayerTimeService;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GET /api/dailyoperations/focus
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// يُعيد جلسة التركيز للمستخدم: المهام المرتبة + جدول الصلاة + إحصاءات.
    ///
    /// prayerTimeContext يمكن أن يكون:
    ///   "current"    → يكتشف الفترة الحالية تلقائياً من جدول الصلاة
    ///   "AfterFajr"  → عرض مهام فترة ما بعد الفجر (للتخطيط المسبق)
    ///   "Duha"       → وقت الضحى
    ///   "AfterDhuhr" → بعد الظهر
    ///   "AfterAsr"   → بعد العصر
    ///   "AfterMaghrib" → بعد المغرب
    ///   "AfterIsha"  → بعد العشاء
    /// </summary>
    /// <param name="prayerTimeContext">سياق الفترة الزمنية (افتراضي: current)</param>
    /// <param name="contextTag">السياق المكاني للتصفية (افتراضي: Anywhere)</param>
    /// <param name="maxResults">الحد الأقصى للمهام (افتراضي: 20)</param>
    /// <param name="includeWeightBreakdown">هل تُضمَّن تفاصيل الأوزان؟ (افتراضي: true)</param>
    /// <param name="ct">رمز الإلغاء</param>
    [HttpGet("focus")]
    [ProducesResponseType(typeof(FocusSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFocusTasks(
        [FromQuery] string prayerTimeContext = "current",
        [FromQuery] ContextTag contextTag = ContextTag.Anywhere,
        [FromQuery] int maxResults = 20,
        [FromQuery] bool includeWeightBreakdown = true,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        var now    = DateTime.UtcNow;

        // ── تحديد فترة الصلاة المستهدفة ─────────────────────────────────────

        PrayerPeriod targetPeriod;

        if (prayerTimeContext.Equals("current", StringComparison.OrdinalIgnoreCase))
        {
            // اكتشاف الفترة الحالية تلقائياً
            targetPeriod = await _prayerTimeService
                .GetCurrentPeriodAsync(userId, now, ct);
        }
        else if (Enum.TryParse<PrayerPeriod>(prayerTimeContext, ignoreCase: true, out var parsed))
        {
            targetPeriod = parsed;
        }
        else
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title  = "قيمة prayerTimeContext غير صالحة",
                Detail = $"القيم المقبولة: current, AfterFajr, Duha, AfterDhuhr, AfterAsr, AfterMaghrib, AfterIsha. " +
                         $"استُلم: '{prayerTimeContext}'"
            });
        }

        // ── تشغيل محرك الأولويات ─────────────────────────────────────────────

        var engineRequest = new PriorityEngineRequest
        {
            UserId                 = userId,
            CurrentContextTag      = contextTag,
            MaxResults             = maxResults,
            IncludeWeightBreakdown = includeWeightBreakdown,
            AsOf                   = now
        };

        var engineResult = await _priorityEngine
            .GetPrioritizedTasksAsync(engineRequest, ct);

        // ── جلب جدول الصلاة لعرض العداد التنازلي ────────────────────────────

        var schedule = await _prayerTimeService
            .GetScheduleAsync(userId, DateOnly.FromDateTime(now), ct);

        return Ok(new FocusSessionResponse
        {
            CurrentPeriod             = engineResult.CurrentPrayerPeriod,
            CurrentPeriodName         = engineResult.CurrentPrayerPeriodName,
            Tasks                     = engineResult.Tasks,
            PrayerSchedule            = schedule is not null
                                         ? PrayerScheduleResponse.From(schedule, now)
                                         : null,
            HasPrayerSchedule         = engineResult.HasPrayerSchedule,
            ExcludedEmergencyCount    = engineResult.ExcludedEmergencyCount,
            ExcludedContextMismatchCount = engineResult.ExcludedContextMismatchCount,
            GeneratedAt               = now
        });
    }

    // ══════════════════════════════════════════════════════════════════════════
    // POST /api/dailyoperations/quick-add
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// إضافة مهمة سريعة من نافذة FAB Modal.
    ///
    /// يُنشئ المهمة ويحسب وزنها الأولوي فوراً،
    /// ثم يُعيد 201 Created مع بيانات المهمة + الوزن.
    /// </summary>
    [HttpPost("quick-add")]
    [ProducesResponseType(typeof(TaskCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> QuickAddTask(
        [FromBody] QuickAddTaskRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();

        // ── إنشاء الكيان ──────────────────────────────────────────────────────

        var task = new TaskItem
        {
            Title                = request.Title,
            Priority             = request.Priority,
            IsPomodoroCompatible = request.IsPomodoroCompatible,
            ContextTag           = request.ContextTag,
            PreferredPrayerPeriod = request.PreferredPrayerPeriod,
            DueDate              = request.DueDate,
            UserId               = userId,
            Status               = TaskStatus.Pending
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);

        // ── حساب الوزن الأولوي ────────────────────────────────────────────────

        var weightBreakdown = await _priorityEngine
            .CalculateTaskWeightAsync(task, userId, ct: ct);

        var response = TaskCreatedResponse.From(task, weightBreakdown);

        // Location Header يُشير إلى المهمة في TasksController
        return CreatedAtAction(
            actionName: "GetById",
            controllerName: "Tasks",
            routeValues: new { id = task.Id },
            value: response);
    }
}
