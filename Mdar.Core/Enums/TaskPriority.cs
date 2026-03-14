namespace Mdar.Core.Enums;

/// <summary>
/// مستوى أولوية المهمة أو المشروع
/// </summary>
public enum TaskPriority
{
    /// <summary>منخفضة - يمكن تأجيلها</summary>
    Low = 0,

    /// <summary>متوسطة - الحالة الافتراضية</summary>
    Medium = 1,

    /// <summary>عالية - تستحق الاهتمام</summary>
    High = 2,

    /// <summary>حرجة - يجب إنجازها فوراً</summary>
    Critical = 3
}
