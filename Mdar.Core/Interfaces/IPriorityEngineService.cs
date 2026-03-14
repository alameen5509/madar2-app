using Mdar.Core.Entities.Tasks;
using Mdar.Core.Models.Priority;

namespace Mdar.Core.Interfaces;

/// <summary>
/// واجهة الخدمة المركزية لمحرك الأولويات — Application Service Facade.
///
/// الغرض من هذه الواجهة:
///   تجميع عمليتَين في عقد واحد تحقناه في الـ Controller:
///
///   1. <see cref="CalculateTaskWeightAsync"/>
///      تُستدعى مباشرةً بعد إنشاء مهمة جديدة لحساب وزنها الأولوي الأولي.
///      تجمع داخلياً بين IPrayerTimeService + IPriorityWeightCalculator.
///
///   2. <see cref="GetPrioritizedTasksAsync"/>
///      تُعيد قائمة المهام كاملةً مرتبةً للسياق الحالي.
///      تُفوِّض داخلياً إلى ITaskPriorityEngine.
///
/// لماذا Facade وليس حقن ثلاث واجهات في Controller؟
///   - يبقي الـ Controller رفيعاً (Thin Controller)
///   - يُخفي تفاصيل التركيب الداخلي عن طبقة API
///   - يُسهِّل الاختبار: mock واحد بدل ثلاثة
/// </summary>
public interface IPriorityEngineService
{
    /// <summary>
    /// يحسب الوزن الأولوي لمهمة واحدة في سياق الفترة الزمنية الحالية.
    ///
    /// الاستخدام النموذجي:
    ///   بعد إنشاء TaskItem وحفظه في DB، نمرره هنا لنحصل على وزنه الأولي
    ///   ونُعيده للعميل في الاستجابة 201 Created.
    /// </summary>
    /// <param name="task">المهمة المراد تقييمها (بعد الحفظ في DB)</param>
    /// <param name="userId">معرّف المستخدم لجلب جدول أوقات صلاته</param>
    /// <param name="asOf">
    /// نقطة زمن مرجعية (UTC). null = DateTime.UtcNow.
    /// للاختبار فقط — في الإنتاج يُترك null.
    /// </param>
    /// <param name="ct">رمز الإلغاء</param>
    /// <returns>تفصيل كامل لمكونات الوزن الأولوي</returns>
    Task<TaskWeightBreakdown> CalculateTaskWeightAsync(
        TaskItem task,
        Guid userId,
        DateTime? asOf = null,
        CancellationToken ct = default);

    /// <summary>
    /// يُعيد قائمة المهام المرتبة تنازلياً بحسب الأولوية للسياق الحالي.
    /// يُفوِّض داخلياً إلى ITaskPriorityEngine.
    /// </summary>
    Task<PriorityEngineResult> GetPrioritizedTasksAsync(
        PriorityEngineRequest request,
        CancellationToken ct = default);
}
