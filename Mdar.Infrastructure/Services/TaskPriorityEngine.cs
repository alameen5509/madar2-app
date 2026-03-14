using Mdar.Core.Enums;
using Mdar.Core.Interfaces;
using Mdar.Core.Models.Priority;
using Mdar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mdar.Infrastructure.Services;

/// <summary>
/// المحرك الرئيسي لترتيب المهام بحسب الأولوية.
///
/// ═══ تدفق المعالجة ═══════════════════════════════════════
///
///  [1] جلب الفترة الزمنية الحالية  ← IPrayerTimeService
///  [2] جلب المهام النشطة من DB     ← AppDbContext
///  [3] مرحلة التصفية (Filtering):
///       (أ) استبعاد IsEmergency = true
///       (ب) استبعاد ContextTag غير متطابق
///  [4] حساب PriorityWeight          ← IPriorityWeightCalculator
///  [5] الترتيب تنازلياً بحسب Weight
///  [6] تطبيق MaxResults (اختياري)
///  [7] إعادة PriorityEngineResult
///
/// ═══ قواعد التصفية التفصيلية ══════════════════════════════
///
///  ✗ IsEmergency = true       → مسار طوارئ منفصل (خارج نطاق هذا المحرك)
///  ✗ Status = Completed        → منتهية، لا معنى لترتيبها
///  ✗ Status = Cancelled        → ملغاة
///  ✗ ContextTag مختلف AND     → لا تُنفَّذ في الموقع الحالي
///     ≠ Anywhere
///  ✓ ما تبقى يدخل حساب الوزن
///
/// </summary>
internal sealed class TaskPriorityEngine : ITaskPriorityEngine
{
    private readonly AppDbContext _db;
    private readonly IPrayerTimeService _prayerTimeService;
    private readonly IPriorityWeightCalculator _calculator;
    private readonly ILogger<TaskPriorityEngine> _logger;

    public TaskPriorityEngine(
        AppDbContext db,
        IPrayerTimeService prayerTimeService,
        IPriorityWeightCalculator calculator,
        ILogger<TaskPriorityEngine> logger)
    {
        _db = db;
        _prayerTimeService = prayerTimeService;
        _calculator = calculator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PriorityEngineResult> GetPrioritizedTasksAsync(
        PriorityEngineRequest request,
        CancellationToken ct = default)
    {
        var asOf = request.EffectiveAsOf;

        _logger.LogDebug(
            "تشغيل محرك الأولويات: UserId={UserId}, Context={Context}, AsOf={AsOf}",
            request.UserId, request.CurrentContextTag, asOf);

        // ── [1] تحديد الفترة الزمنية الحالية ─────────────────────────────────
        var (currentPeriod, hasPrayerSchedule) =
            await ResolvePrayerPeriodAsync(request.UserId, asOf, ct);

        // ── [2] جلب المهام النشطة من قاعدة البيانات ──────────────────────────
        //    نجلب فقط الأعمدة اللازمة للحساب والعرض (تجنباً لـ SELECT *)
        var allActiveTasks = await _db.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == request.UserId
                     && t.Status != Core.Enums.TaskStatus.Completed
                     && t.Status != Core.Enums.TaskStatus.Cancelled)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Description,
                t.Status,
                t.Priority,
                t.ContextTag,
                t.IsEmergency,
                t.PreferredPrayerPeriod,
                t.IsPomodoroCompatible,
                t.EstimatedPomodoros,
                t.CompletedPomodoros,
                t.DueDate,
                t.CreatedAt
            })
            .ToListAsync(ct);

        int totalBeforeFilter = allActiveTasks.Count;

        // ── [3أ] استبعاد مهام الطوارئ ────────────────────────────────────────
        var emergencyTasks    = allActiveTasks.Where(t => t.IsEmergency).ToList();
        var nonEmergencyTasks = allActiveTasks.Where(t => !t.IsEmergency).ToList();
        int excludedEmergency = emergencyTasks.Count;

        if (excludedEmergency > 0)
            _logger.LogDebug(
                "تم استبعاد {Count} مهمة طوارئ من الترتيب العادي.",
                excludedEmergency);

        // ── [3ب] استبعاد المهام غير الملائمة للسياق المكاني ──────────────────
        var contextFilteredTasks = nonEmergencyTasks
            .Where(t => IsContextMatch(t.ContextTag, request.CurrentContextTag))
            .ToList();

        int excludedContextMismatch = nonEmergencyTasks.Count - contextFilteredTasks.Count;

        // ── [4] حساب الأوزان ──────────────────────────────────────────────────
        //    نُنشئ TaskItem مؤقتة مخففة لتمريرها للحاسبة (بدلاً من Projection ثقيل)
        var weightedItems = contextFilteredTasks
            .Select(t =>
            {
                // نُنشئ كائناً مخففاً يحمل البيانات اللازمة للحساب فقط
                var taskProxy = new Core.Entities.Tasks.TaskItem
                {
                    Id                   = t.Id,
                    Title                = t.Title,
                    Status               = t.Status,
                    Priority             = t.Priority,
                    ContextTag           = t.ContextTag,
                    IsEmergency          = t.IsEmergency,
                    PreferredPrayerPeriod = t.PreferredPrayerPeriod,
                    IsPomodoroCompatible = t.IsPomodoroCompatible,
                    EstimatedPomodoros   = t.EstimatedPomodoros,
                    CompletedPomodoros   = t.CompletedPomodoros,
                    DueDate              = t.DueDate,
                    CreatedAt            = t.CreatedAt,
                    // حقول مطلوبة بواسطة BaseEntity
                    UserId               = request.UserId
                };

                var breakdown = _calculator.Calculate(taskProxy, currentPeriod, asOf);

                return (Task: t, Breakdown: breakdown);
            })
            .ToList();

        // ── [5] الترتيب تنازلياً بحسب الوزن ──────────────────────────────────
        var sorted = weightedItems
            .OrderByDescending(x => x.Breakdown.TotalWeight)
            .ToList();

        // ── [6] تطبيق الحد الأقصى للنتائج ────────────────────────────────────
        var finalItems = request.MaxResults.HasValue
            ? sorted.Take(request.MaxResults.Value).ToList()
            : sorted;

        // ── [7] تجميع النتيجة ─────────────────────────────────────────────────
        var rankedTasks = finalItems
            .Select((x, index) => new PrioritizedTaskDto
            {
                Id                    = x.Task.Id,
                Title                 = x.Task.Title,
                Description           = x.Task.Description,
                Status                = x.Task.Status,
                Priority              = x.Task.Priority,
                ContextTag            = x.Task.ContextTag,
                PreferredPrayerPeriod = x.Task.PreferredPrayerPeriod,
                DueDate               = x.Task.DueDate,
                IsPomodoroCompatible  = x.Task.IsPomodoroCompatible,
                EstimatedPomodoros    = x.Task.EstimatedPomodoros,
                CompletedPomodoros    = x.Task.CompletedPomodoros,
                PriorityWeight        = x.Breakdown.TotalWeight,
                Rank                  = index + 1,
                WeightBreakdown       = request.IncludeWeightBreakdown ? x.Breakdown : null
            })
            .ToList();

        _logger.LogInformation(
            "محرك الأولويات: الفترة={Period}, الإجمالي={Total}, طوارئ={Emergency}, " +
            "مكان={Context}, نتائج={Results}",
            currentPeriod, totalBeforeFilter, excludedEmergency,
            excludedContextMismatch, rankedTasks.Count);

        return new PriorityEngineResult
        {
            CurrentPrayerPeriod       = currentPeriod,
            CurrentPrayerPeriodName   = GetPeriodNameAr(currentPeriod),
            AppliedContextTag         = request.CurrentContextTag,
            Tasks                     = rankedTasks,
            TotalTasksBeforeFilter    = totalBeforeFilter,
            ExcludedEmergencyCount    = excludedEmergency,
            ExcludedContextMismatchCount = excludedContextMismatch,
            GeneratedAt               = asOf,
            HasPrayerSchedule         = hasPrayerSchedule
        };
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// يحدد الفترة الزمنية ويُشير إذا كان الجدول متوفراً.
    /// مُنفصل عن GetCurrentPeriodAsync لمعرفة حالة الـ Fallback.
    /// </summary>
    private async Task<(PrayerPeriod Period, bool HasSchedule)> ResolvePrayerPeriodAsync(
        Guid userId, DateTime asOf, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(asOf);
        var schedule = await _prayerTimeService.GetScheduleAsync(userId, today, ct);

        if (schedule is null)
            return (PrayerPeriod.Duha, HasSchedule: false);

        var timeOfDay = TimeOnly.FromDateTime(asOf);
        return (schedule.GetPeriodFor(timeOfDay), HasSchedule: true);
    }

    /// <summary>
    /// قاعدة تطابق السياق المكاني.
    ///
    /// مهمة تنجح في الفلتر إذا:
    ///   - ContextTag = Anywhere (لا قيد مكاني على المهمة)
    ///   - ContextTag = currentContext (تطابق تام)
    ///   - currentContext = Anywhere (المستخدم لم يحدد موقعاً → يرى الكل)
    /// </summary>
    private static bool IsContextMatch(ContextTag taskContext, ContextTag currentContext)
        => taskContext == ContextTag.Anywhere
        || currentContext == ContextTag.Anywhere
        || taskContext == currentContext;

    private static string GetPeriodNameAr(PrayerPeriod period) => period switch
    {
        PrayerPeriod.AfterFajr    => "بعد الفجر",
        PrayerPeriod.Duha         => "وقت الضحى",
        PrayerPeriod.AfterDhuhr   => "بعد الظهر",
        PrayerPeriod.AfterAsr     => "بعد العصر",
        PrayerPeriod.AfterMaghrib => "بعد المغرب",
        PrayerPeriod.AfterIsha    => "بعد العشاء",
        _                         => period.ToString()
    };
}
