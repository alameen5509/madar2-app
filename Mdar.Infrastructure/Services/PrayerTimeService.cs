using Mdar.Core.Entities.Tasks;
using Mdar.Core.Enums;
using Mdar.Core.Interfaces;
using Mdar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mdar.Infrastructure.Services;

/// <summary>
/// تنفيذ خدمة أوقات الصلاة باستخدام بيانات DailyPrayerSchedule من قاعدة البيانات.
///
/// سلوك الـ Fallback:
///   إذا لم يُسجَّل جدول لليوم → يُعيد Duha (أكثر أوقات اليوم نشاطاً)
///   ويُسجِّل تحذيراً في الـ Log حتى يُنبَّه المطوِّر/المستخدم.
/// </summary>
internal sealed class PrayerTimeService : IPrayerTimeService
{
    private readonly AppDbContext _db;
    private readonly ILogger<PrayerTimeService> _logger;

    public PrayerTimeService(AppDbContext db, ILogger<PrayerTimeService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PrayerPeriod> GetCurrentPeriodAsync(
        Guid userId,
        DateTime? asOf = null,
        CancellationToken ct = default)
    {
        var effectiveTime = asOf ?? DateTime.UtcNow;
        var today = DateOnly.FromDateTime(effectiveTime);
        var timeOfDay = TimeOnly.FromDateTime(effectiveTime);

        var schedule = await GetScheduleAsync(userId, today, ct);

        if (schedule is null)
        {
            _logger.LogWarning(
                "لم يُعثر على جدول أوقات صلاة لـ UserId={UserId} بتاريخ {Date}. " +
                "تم الرجوع إلى الفترة الافتراضية: Duha.",
                userId, today);

            // Fallback: Duha هي الفترة الأكثر شيوعاً ومحايدة
            return PrayerPeriod.Duha;
        }

        return schedule.GetPeriodFor(timeOfDay);
    }

    /// <inheritdoc />
    public async Task<DailyPrayerSchedule?> GetScheduleAsync(
        Guid userId,
        DateOnly? date = null,
        CancellationToken ct = default)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        return await _db.DailyPrayerSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Date == targetDate, ct);
    }
}
