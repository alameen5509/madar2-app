using Mdar.Core.Enums;

namespace Mdar.Core.Models.Priority;

/// <summary>
/// طلب محرك الأولويات — يحمل كل المعلومات السياقية اللازمة.
/// </summary>
public sealed record PriorityEngineRequest
{
    /// <summary>
    /// معرّف المستخدم الذي نُرتِّب مهامه.
    /// [مطلوب]
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// السياق المكاني الحالي للمستخدم.
    /// يُستخدم لتصفية المهام حسب ContextTag.
    /// Anywhere = اعرض كل المهام بغض النظر عن السياق المكاني.
    /// </summary>
    public ContextTag CurrentContextTag { get; init; } = ContextTag.Anywhere;

    /// <summary>
    /// نقطة زمن مرجعية (UTC) لحساب الفترة الزمنية والإلحاحية والعمر.
    /// null = DateTime.UtcNow (الحالة الطبيعية للإنتاج).
    /// تُستخدم قيمة غير null في اختبارات الوحدة فقط.
    /// </summary>
    public DateTime? AsOf { get; init; }

    /// <summary>
    /// الحد الأقصى لعدد المهام في النتيجة.
    /// null = إعادة جميع المهام المؤهلة مرتبةً.
    /// قيمة شائعة: 10 (لعرض قائمة "أهم 10 مهام اليوم").
    /// </summary>
    public int? MaxResults { get; init; }

    /// <summary>
    /// هل تُضمَّن تفاصيل حساب الوزن (WeightBreakdown) في النتيجة؟
    /// true  = يُضمَّن (مفيد للـ Debugging وواجهة "لماذا هذه أولاً؟")
    /// false = يُحذف (أخف للأداء في الاستخدام العادي)
    /// </summary>
    public bool IncludeWeightBreakdown { get; init; } = false;

    /// <summary>الوقت الفعلي المستخدم بعد تطبيق القيمة الافتراضية.</summary>
    internal DateTime EffectiveAsOf => AsOf ?? DateTime.UtcNow;
}
