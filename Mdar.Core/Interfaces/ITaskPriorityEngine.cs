using Mdar.Core.Models.Priority;

namespace Mdar.Core.Interfaces;

/// <summary>
/// عقد محرك الأولويات الرئيسي.
///
/// هذه هي الواجهة التي تستخدمها طبقة API/Controllers مباشرةً.
/// تجمع بين:
///   - IPrayerTimeService  → لتحديد الفترة الزمنية الحالية
///   - IPriorityWeightCalculator → لحساب الوزن لكل مهمة
///   - AppDbContext       → لجلب المهام المؤهلة وتصفيتها
///
/// القواعد الأساسية للمحرك:
///   1. تُستبعد المهام ذات IsEmergency = true (تُعالَج بمسار منفصل)
///   2. تُستبعد المهام التي ContextTag لا يتطابق مع الموقع الحالي
///      (باستثناء Anywhere التي تُضمَّن دائماً)
///   3. تُستبعد المهام المكتملة أو الملغاة (Status = Completed/Cancelled)
///   4. تُرتَّب النتائج تنازلياً بحسب PriorityWeight
/// </summary>
public interface ITaskPriorityEngine
{
    /// <summary>
    /// يُنتج قائمة المهام مرتبة بحسب الأولوية للسياق الحالي.
    /// </summary>
    /// <param name="request">معاملات الطلب: المستخدم، الموقع، الوقت، عدد النتائج</param>
    /// <param name="ct">رمز الإلغاء</param>
    /// <returns>
    /// نتيجة المحرك: القائمة المرتبة + إحصاءات التصفية + الفترة الزمنية الحالية.
    /// </returns>
    Task<PriorityEngineResult> GetPrioritizedTasksAsync(
        PriorityEngineRequest request,
        CancellationToken ct = default);
}
