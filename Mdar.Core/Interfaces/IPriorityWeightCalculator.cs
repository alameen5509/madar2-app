using Mdar.Core.Entities.Tasks;
using Mdar.Core.Enums;
using Mdar.Core.Models.Priority;

namespace Mdar.Core.Interfaces;

/// <summary>
/// عقد حاسبة الوزن الأولوي للمهمة.
/// مسؤوليتها: ترجمة خصائص المهمة + السياق الزمني إلى رقم مقارَن (PriorityWeight).
///
/// الصيغة العامة للحساب:
///   PriorityWeight = (BaseScore × PrayerMultiplier)
///                  + UrgencyBoost
///                  + AgeBoost
///                  + PomodoroBonus
///
/// هذه الواجهة قابلة للاستبدال — يمكن تطوير خوارزمية حساب مختلفة
/// (مثلاً: خوارزمية تعلم آلي) دون المساس بمحرك الأولويات.
/// </summary>
public interface IPriorityWeightCalculator
{
    /// <summary>
    /// يحسب الوزن الأولوي لمهمة واحدة في سياق زمني ومكاني محدد.
    /// </summary>
    /// <param name="task">المهمة المراد حساب وزنها</param>
    /// <param name="currentPeriod">الفترة الزمنية الحالية (من IPrayerTimeService)</param>
    /// <param name="asOf">
    /// نقطة الزمن المرجعية لحساب الإلحاحية (Urgency) والعمر (Age).
    /// يجب أن تكون UTC.
    /// </param>
    /// <returns>
    /// تفصيل الأوزان مع الرقم الإجمالي.
    /// إرجاع التفصيل (لا الرقم فقط) يتيح Debugging وعرض السبب للمستخدم.
    /// </returns>
    TaskWeightBreakdown Calculate(TaskItem task, PrayerPeriod currentPeriod, DateTime asOf);
}
