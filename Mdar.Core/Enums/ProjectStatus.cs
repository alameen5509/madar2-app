namespace Mdar.Core.Enums;

/// <summary>
/// حالة المشروع في دورة حياته
/// </summary>
public enum ProjectStatus
{
    /// <summary>في مرحلة التخطيط</summary>
    Planning = 0,

    /// <summary>نشط وجارٍ العمل عليه</summary>
    Active = 1,

    /// <summary>معلق مؤقتاً</summary>
    OnHold = 2,

    /// <summary>مكتمل بنجاح</summary>
    Completed = 3,

    /// <summary>مؤرشف</summary>
    Archived = 4
}
