using Mdar.Core.Entities.Common;
using Mdar.Core.Entities.Identity;
using Mdar.Core.Enums;

namespace Mdar.Core.Entities.Tasks;

/// <summary>
/// جدول أوقات الصلاة ليوم محدد للمستخدم.
/// يُخزَّن يومياً (سجل واحد لكل يوم لكل مستخدم).
///
/// مصدر البيانات: يُدخلها المستخدم يدوياً أو تُجلَب من API خارجي
/// مثل Aladhan API أو IslamicFinder.
///
/// كيف يستخدمها محرك الأولويات:
///   يُقارن الوقت الحالي بحدود كل فترة لتحديد PrayerPeriod الفعلية.
/// </summary>
public class DailyPrayerSchedule : BaseEntity
{
    /// <summary>التاريخ الذي تنتمي إليه هذه الأوقات</summary>
    public DateOnly Date { get; set; }

    // ─── أوقات الصلوات الخمس ─────────────────────────────────────────────────

    /// <summary>وقت أذان الفجر — يبدأ عنده PrayerPeriod.AfterFajr</summary>
    public TimeOnly FajrTime { get; set; }

    /// <summary>
    /// وقت الشروق — ينتهي عنده AfterFajr ويبدأ Duha.
    /// يُسمى أيضاً Shuruq.
    /// </summary>
    public TimeOnly SunriseTime { get; set; }

    /// <summary>وقت أذان الظهر — ينتهي عنده Duha ويبدأ AfterDhuhr</summary>
    public TimeOnly DhuhrTime { get; set; }

    /// <summary>وقت أذان العصر — ينتهي عنده AfterDhuhr ويبدأ AfterAsr</summary>
    public TimeOnly AsrTime { get; set; }

    /// <summary>وقت أذان المغرب — ينتهي عنده AfterAsr ويبدأ AfterMaghrib</summary>
    public TimeOnly MaghribTime { get; set; }

    /// <summary>وقت أذان العشاء — ينتهي عنده AfterMaghrib ويبدأ AfterIsha</summary>
    public TimeOnly IshaTime { get; set; }

    // ─── Metadata ─────────────────────────────────────────────────────────────

    /// <summary>
    /// هل جُلبت هذه الأوقات تلقائياً من API خارجي؟
    /// false = أدخلها المستخدم يدوياً.
    /// </summary>
    public bool IsAutoFetched { get; set; } = false;

    /// <summary>
    /// المصدر الذي جُلبت منه الأوقات (اختياري).
    /// مثال: "Aladhan API"، "أدخلها المستخدم".
    /// </summary>
    public string? Source { get; set; }

    // ─── Foreign Keys ─────────────────────────────────────────────────────────

    /// <summary>معرّف المستخدم المالك لهذا الجدول</summary>
    public Guid UserId { get; set; }

    // ─── Navigation Properties ────────────────────────────────────────────────

    public User User { get; set; } = null!;

    // ─── Domain Logic ─────────────────────────────────────────────────────────

    /// <summary>
    /// تحديد الفترة الزمنية بناءً على وقت معيّن من اليوم.
    /// هذا هو قلب منطق محرك الأولويات.
    /// </summary>
    /// <param name="time">الوقت المراد تصنيفه (عادةً الوقت الحالي)</param>
    /// <returns>الفترة الزمنية التي يقع فيها هذا الوقت</returns>
    public PrayerPeriod GetPeriodFor(TimeOnly time)
    {
        if (time >= FajrTime && time < SunriseTime)
            return PrayerPeriod.AfterFajr;

        if (time >= SunriseTime && time < DhuhrTime)
            return PrayerPeriod.Duha;

        if (time >= DhuhrTime && time < AsrTime)
            return PrayerPeriod.AfterDhuhr;

        if (time >= AsrTime && time < MaghribTime)
            return PrayerPeriod.AfterAsr;

        if (time >= MaghribTime && time < IshaTime)
            return PrayerPeriod.AfterMaghrib;

        // ما قبل الفجر وما بعد العشاء كلاهما AfterIsha
        return PrayerPeriod.AfterIsha;
    }
}
