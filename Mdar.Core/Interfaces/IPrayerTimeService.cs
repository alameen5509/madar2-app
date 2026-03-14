using Mdar.Core.Entities.Tasks;
using Mdar.Core.Enums;

namespace Mdar.Core.Interfaces;

/// <summary>
/// عقد خدمة أوقات الصلاة.
/// مسؤوليتها الوحيدة: تحديد الفترة الزمنية الحالية بناءً على جدول أوقات المستخدم.
///
/// تصميم القرار:
///   - الواجهة في Mdar.Core (Domain) لأن المحرك يعتمد عليها.
///   - التنفيذ في Mdar.Infrastructure لأنه يحتاج AppDbContext.
///   - يمكن مستقبلاً إنشاء تنفيذ بديل يجلب الأوقات من API خارجي
///     دون تغيير أي كود في طبقة Business Logic.
/// </summary>
public interface IPrayerTimeService
{
    /// <summary>
    /// يُحدِّد الفترة الزمنية الحالية للمستخدم.
    /// </summary>
    /// <param name="userId">معرّف المستخدم لجلب جدوله الخاص</param>
    /// <param name="asOf">
    /// وقت مرجعي للحساب (UTC).
    /// null = الوقت الحالي DateTime.UtcNow.
    /// يُستخدم للاختبار أو للمعاينة الافتراضية.
    /// </param>
    /// <param name="ct">رمز الإلغاء</param>
    /// <returns>
    /// الفترة الزمنية الحالية.
    /// إذا لم يُسجَّل جدول لليوم، يُعيد Duha كقيمة افتراضية.
    /// </returns>
    Task<PrayerPeriod> GetCurrentPeriodAsync(
        Guid userId,
        DateTime? asOf = null,
        CancellationToken ct = default);

    /// <summary>
    /// يجلب جدول أوقات الصلاة ليوم محدد.
    /// </summary>
    /// <param name="userId">معرّف المستخدم</param>
    /// <param name="date">التاريخ المطلوب. null = اليوم الحالي.</param>
    /// <param name="ct">رمز الإلغاء</param>
    /// <returns>جدول الأوقات أو null إذا لم يُسجَّل جدول لهذا اليوم</returns>
    Task<DailyPrayerSchedule?> GetScheduleAsync(
        Guid userId,
        DateOnly? date = null,
        CancellationToken ct = default);
}
