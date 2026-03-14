using Mdar.Core.Entities.Tasks;
using Mdar.Core.Enums;

namespace Mdar.API.DTOs.PrayerSchedule;

/// <summary>
/// استجابة جدول أوقات الصلاة مع الفترة الزمنية الحالية.
/// </summary>
public sealed record PrayerScheduleResponse
{
    public Guid Id { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly FajrTime { get; init; }
    public TimeOnly SunriseTime { get; init; }
    public TimeOnly DhuhrTime { get; init; }
    public TimeOnly AsrTime { get; init; }
    public TimeOnly MaghribTime { get; init; }
    public TimeOnly IshaTime { get; init; }
    public bool IsAutoFetched { get; init; }
    public string? Source { get; init; }

    /// <summary>الفترة الزمنية الحالية بناءً على هذا الجدول والوقت الآن</summary>
    public PrayerPeriod CurrentPeriod { get; init; }

    /// <summary>الاسم العربي للفترة الحالية</summary>
    public string CurrentPeriodName { get; init; } = string.Empty;

    public static PrayerScheduleResponse From(DailyPrayerSchedule schedule) =>
        From(schedule, DateTime.UtcNow);

    public static PrayerScheduleResponse From(DailyPrayerSchedule schedule, DateTime asOf)
    {
        var currentPeriod = schedule.GetPeriodFor(TimeOnly.FromDateTime(asOf));
        return new()
        {
            Id            = schedule.Id,
            Date          = schedule.Date,
            FajrTime      = schedule.FajrTime,
            SunriseTime   = schedule.SunriseTime,
            DhuhrTime     = schedule.DhuhrTime,
            AsrTime       = schedule.AsrTime,
            MaghribTime   = schedule.MaghribTime,
            IshaTime      = schedule.IshaTime,
            IsAutoFetched = schedule.IsAutoFetched,
            Source        = schedule.Source,
            CurrentPeriod     = currentPeriod,
            CurrentPeriodName = GetPeriodNameAr(currentPeriod)
        };
    }

    private static string GetPeriodNameAr(PrayerPeriod p) => p switch
    {
        PrayerPeriod.AfterFajr    => "بعد الفجر",
        PrayerPeriod.Duha         => "وقت الضحى",
        PrayerPeriod.AfterDhuhr   => "بعد الظهر",
        PrayerPeriod.AfterAsr     => "بعد العصر",
        PrayerPeriod.AfterMaghrib => "بعد المغرب",
        PrayerPeriod.AfterIsha    => "بعد العشاء",
        _                         => p.ToString()
    };
}
