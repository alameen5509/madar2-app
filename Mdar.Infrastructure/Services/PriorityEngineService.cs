using Mdar.Core.Entities.Tasks;
using Mdar.Core.Interfaces;
using Mdar.Core.Models.Priority;

namespace Mdar.Infrastructure.Services;

/// <summary>
/// تنفيذ Application Service Facade لمحرك الأولويات.
///
/// يجمع ثلاث خدمات في واجهة واحدة بسيطة تُستهلَك من الـ Controller:
///   - IPrayerTimeService  → لتحديد الفترة الزمنية الحالية
///   - IPriorityWeightCalculator → لحساب الوزن لمهمة واحدة
///   - ITaskPriorityEngine → للقائمة الكاملة المرتبة
/// </summary>
internal sealed class PriorityEngineService : IPriorityEngineService
{
    private readonly IPrayerTimeService _prayerTimeService;
    private readonly IPriorityWeightCalculator _calculator;
    private readonly ITaskPriorityEngine _engine;

    public PriorityEngineService(
        IPrayerTimeService prayerTimeService,
        IPriorityWeightCalculator calculator,
        ITaskPriorityEngine engine)
    {
        _prayerTimeService = prayerTimeService;
        _calculator = calculator;
        _engine = engine;
    }

    /// <inheritdoc />
    public async Task<TaskWeightBreakdown> CalculateTaskWeightAsync(
        TaskItem task,
        Guid userId,
        DateTime? asOf = null,
        CancellationToken ct = default)
    {
        var effectiveTime = asOf ?? DateTime.UtcNow;

        // جلب الفترة الزمنية الحالية للمستخدم بناءً على جدول صلاته
        var currentPeriod = await _prayerTimeService
            .GetCurrentPeriodAsync(userId, effectiveTime, ct);

        // حساب الوزن للمهمة في هذه الفترة
        return _calculator.Calculate(task, currentPeriod, effectiveTime);
    }

    /// <inheritdoc />
    public Task<PriorityEngineResult> GetPrioritizedTasksAsync(
        PriorityEngineRequest request,
        CancellationToken ct = default)
        => _engine.GetPrioritizedTasksAsync(request, ct);
}
