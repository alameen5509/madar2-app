namespace Mdar.Core.Models.Priority;

/// <summary>
/// تفصيل مكونات الوزن الأولوي لمهمة واحدة.
///
/// الغرض المزدوج:
///   1. Debugging: تتبع كيف وصل المحرك لهذا الترتيب
///   2. UX شفاف: عرض "لماذا هذه المهمة أولاً؟" للمستخدم
///
/// مثال على العرض للمستخدم:
///   "ترتيبها الأول لأنها: مهمة عالية الأولوية (+750)،
///    موعدها اليوم (+400)، ومناسبة لفترة الضحى الحالية (×1.5)"
/// </summary>
public sealed record TaskWeightBreakdown
{
    /// <summary>
    /// النقاط الأساسية المبنية على أولوية المهمة (Priority).
    /// Critical=1000 | High=750 | Medium=500 | Low=250
    /// </summary>
    public double BaseScore { get; init; }

    /// <summary>
    /// معامل ضرب فترة الصلاة المُطبَّق على BaseScore.
    /// تطابق تام=1.5 | مجاور=1.2 | لا تفضيل=1.0 | مختلف=0.85
    /// </summary>
    public double PrayerPeriodMultiplier { get; init; }

    /// <summary>
    /// قيمة (BaseScore × PrayerPeriodMultiplier) بعد تطبيق المضاعف.
    /// </summary>
    public double WeightedBaseScore { get; init; }

    /// <summary>
    /// مكافأة الإلحاحية المبنية على الموعد النهائي (DueDate).
    /// متأخرة=حتى 800 | اليوم=400 | غداً=250 | هذا الأسبوع=75 | لا موعد=0
    /// </summary>
    public double UrgencyBoost { get; init; }

    /// <summary>
    /// مكافأة العمر لمنع "تجويع" المهام القديمة (Anti-Starvation).
    /// = أيام_الانتظار × 1.5 بحد أقصى 75 نقطة.
    /// </summary>
    public double AgeBoost { get; init; }

    /// <summary>
    /// مكافأة التوافق مع نظام الطماطم في الفترة الحالية.
    /// فترة عالية التركيز=60 | متوسطة=30 | منخفضة=10 | غير متوافقة=0
    /// </summary>
    public double PomodoroBonus { get; init; }

    /// <summary>
    /// الوزن الإجمالي النهائي = WeightedBaseScore + UrgencyBoost + AgeBoost + PomodoroBonus
    /// </summary>
    public double TotalWeight { get; init; }

    /// <summary>
    /// تفسير نصي بالعربية يشرح أبرز عوامل هذا الترتيب.
    /// مثال: "أولوية عالية + موعدها اليوم + مناسبة لفترة الضحى"
    /// </summary>
    public string Explanation { get; init; } = string.Empty;
}
