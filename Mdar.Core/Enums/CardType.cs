namespace Mdar.Core.Enums;

/// <summary>
/// نوع بطاقة التفكير — يُحدد اللون الافتراضي والأيقونة المعروضة على اللوحة.
/// </summary>
public enum CardType
{
    /// <summary>ملاحظة عامة — رمادي داكن</summary>
    Note = 0,

    /// <summary>فكرة إبداعية — كهرماني</summary>
    Idea = 1,

    /// <summary>مهمة للتنفيذ — أزرق</summary>
    Task = 2,

    /// <summary>سؤال يحتاج إجابة — بنفسجي</summary>
    Question = 3
}
