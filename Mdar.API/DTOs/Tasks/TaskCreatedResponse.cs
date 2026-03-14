using Mdar.Core.Entities.Tasks;
using Mdar.Core.Models.Priority;

namespace Mdar.API.DTOs.Tasks;

/// <summary>
/// استجابة إنشاء مهمة جديدة — HTTP 201 Created.
///
/// تُضيف على TaskResponse معلوماتَي الوزن الأولوي:
///   - InitialPriorityWeight : الرقم الإجمالي لاستخدامه في الترتيب
///   - InitialWeightBreakdown: التفصيل الكامل لكل مكوّن من مكونات الوزن
///
/// القيمة المضافة: يعرف المستخدم فوراً "أين تقع هذه المهمة
/// في قائمة أولوياته الحالية؟" دون الحاجة لطلب منفصل.
/// </summary>
public sealed record TaskCreatedResponse
{
    /// <summary>بيانات المهمة كاملة</summary>
    public required TaskResponse Task { get; init; }

    /// <summary>
    /// الوزن الأولوي الإجمالي المحسوب في لحظة الإنشاء.
    /// كلما كان أعلى، كانت المهمة أقرب للقمة في قائمة الأولويات.
    /// </summary>
    public double InitialPriorityWeight { get; init; }

    /// <summary>
    /// تفصيل مكونات الوزن للشفافية.
    /// يُجيب على سؤال "لماذا حصلت هذه المهمة على هذا الوزن؟"
    /// </summary>
    public required TaskWeightBreakdown InitialWeightBreakdown { get; init; }

    /// <summary>
    /// التفسير النصي بالعربية لأبرز عوامل الوزن.
    /// مثال: "أولوية عالية | موعدها اليوم | مناسبة لفترة الضحى ×1.5"
    /// </summary>
    public string WeightExplanation { get; init; } = string.Empty;

    /// <summary>
    /// Factory Method — ينشئ الاستجابة من الكيان المحفوظ + نتيجة الحاسبة.
    /// يُستدعى من Controller مباشرة بعد الحفظ في DB وحساب الوزن.
    /// </summary>
    public static TaskCreatedResponse From(TaskItem task, TaskWeightBreakdown weight) => new()
    {
        Task                  = TaskResponse.From(task),
        InitialPriorityWeight = weight.TotalWeight,
        InitialWeightBreakdown = weight,
        WeightExplanation     = weight.Explanation
    };
}
