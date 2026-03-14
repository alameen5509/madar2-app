using Mdar.Core.Enums;

namespace Mdar.Core.Models.Priority;

/// <summary>
/// النتيجة الكاملة لتشغيل محرك الأولويات.
/// تحمل: القائمة المرتبة + السياق الزمني + إحصاءات التصفية.
/// </summary>
public sealed record PriorityEngineResult
{
    // ─── السياق الزمني ──────────────────────────────────────────────────────

    /// <summary>الفترة الزمنية الحالية التي شكّلت أوزان الترتيب</summary>
    public PrayerPeriod CurrentPrayerPeriod { get; init; }

    /// <summary>
    /// الاسم العربي الإنساني للفترة الحالية.
    /// مثال: "بعد الفجر"، "وقت الضحى"، "بعد الظهر".
    /// </summary>
    public required string CurrentPrayerPeriodName { get; init; }

    /// <summary>السياق المكاني المستخدم في التصفية</summary>
    public ContextTag AppliedContextTag { get; init; }

    // ─── القائمة المرتبة ──────────────────────────────────────────────────

    /// <summary>
    /// المهام المرتبة تنازلياً بحسب PriorityWeight.
    /// العنصر الأول (index 0) هو الأعلى أولوية.
    /// </summary>
    public IReadOnlyList<PrioritizedTaskDto> Tasks { get; init; } = [];

    // ─── إحصاءات التصفية ─────────────────────────────────────────────────

    /// <summary>إجمالي المهام قبل أي تصفية (Pending + InProgress)</summary>
    public int TotalTasksBeforeFilter { get; init; }

    /// <summary>عدد المهام المستبعدة بسبب IsEmergency = true</summary>
    public int ExcludedEmergencyCount { get; init; }

    /// <summary>عدد المهام المستبعدة بسبب عدم تطابق ContextTag</summary>
    public int ExcludedContextMismatchCount { get; init; }

    /// <summary>عدد المهام المعروضة فعلياً في النتيجة</summary>
    public int ReturnedCount => Tasks.Count;

    // ─── Metadata ────────────────────────────────────────────────────────

    /// <summary>وقت توليد هذه النتيجة (UTC)</summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// هل كان جدول أوقات الصلاة متوفراً؟
    /// false = استُخدمت Duha كفترة افتراضية (لم يُسجَّل جدول لليوم).
    /// </summary>
    public bool HasPrayerSchedule { get; init; }
}
