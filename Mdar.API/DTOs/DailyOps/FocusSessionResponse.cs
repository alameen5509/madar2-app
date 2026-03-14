using Mdar.API.DTOs.PrayerSchedule;
using Mdar.Core.Enums;
using Mdar.Core.Models.Priority;

namespace Mdar.API.DTOs.DailyOps;

/// <summary>
/// استجابة نقطة Focus — تحمل كل ما تحتاجه صفحة العمليات اليومية.
/// </summary>
public sealed record FocusSessionResponse
{
    /// <summary>الفترة الزمنية الحالية أو المطلوبة</summary>
    public PrayerPeriod CurrentPeriod { get; init; }

    /// <summary>الاسم العربي للفترة</summary>
    public required string CurrentPeriodName { get; init; }

    /// <summary>المهام مرتبة تنازلياً بحسب الأولوية</summary>
    public IReadOnlyList<PrioritizedTaskDto> Tasks { get; init; } = [];

    /// <summary>جدول الصلاة لليوم (لعرض العداد التنازلي)</summary>
    public PrayerScheduleResponse? PrayerSchedule { get; init; }

    /// <summary>هل كان جدول الصلاة متوفراً؟</summary>
    public bool HasPrayerSchedule { get; init; }

    /// <summary>عدد المهام المستبعدة بسبب الطوارئ</summary>
    public int ExcludedEmergencyCount { get; init; }

    /// <summary>عدد المهام المستبعدة بسبب السياق المكاني</summary>
    public int ExcludedContextMismatchCount { get; init; }

    /// <summary>وقت توليد الاستجابة (UTC)</summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}
