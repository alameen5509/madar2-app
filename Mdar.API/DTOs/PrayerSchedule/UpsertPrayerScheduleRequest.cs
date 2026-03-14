using System.ComponentModel.DataAnnotations;

namespace Mdar.API.DTOs.PrayerSchedule;

/// <summary>
/// طلب إنشاء أو تحديث جدول أوقات الصلاة ليوم محدد.
/// إذا وجد جدول لنفس اليوم → تحديث | وإلا → إنشاء جديد.
/// </summary>
public sealed record UpsertPrayerScheduleRequest
{
    /// <summary>
    /// التاريخ المراد ضبط أوقاته.
    /// null = اليوم الحالي.
    /// </summary>
    public DateOnly? Date { get; init; }

    [Required(ErrorMessage = "وقت الفجر مطلوب.")]
    public TimeOnly FajrTime { get; init; }

    [Required(ErrorMessage = "وقت الشروق مطلوب.")]
    public TimeOnly SunriseTime { get; init; }

    [Required(ErrorMessage = "وقت الظهر مطلوب.")]
    public TimeOnly DhuhrTime { get; init; }

    [Required(ErrorMessage = "وقت العصر مطلوب.")]
    public TimeOnly AsrTime { get; init; }

    [Required(ErrorMessage = "وقت المغرب مطلوب.")]
    public TimeOnly MaghribTime { get; init; }

    [Required(ErrorMessage = "وقت العشاء مطلوب.")]
    public TimeOnly IshaTime { get; init; }

    /// <summary>مصدر الأوقات (اختياري). مثال: "Aladhan API"</summary>
    [MaxLength(100)]
    public string? Source { get; init; }
}
