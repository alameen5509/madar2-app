namespace Mdar.Core.Enums;

/// <summary>
/// حالة المهمة في دورة حياتها
/// </summary>
public enum TaskStatus
{
    /// <summary>في الانتظار - لم تبدأ بعد</summary>
    Pending = 0,

    /// <summary>قيد التنفيذ الآن</summary>
    InProgress = 1,

    /// <summary>مكتملة بنجاح</summary>
    Completed = 2,

    /// <summary>ملغاة</summary>
    Cancelled = 3,

    /// <summary>معلقة مؤقتاً</summary>
    OnHold = 4
}
