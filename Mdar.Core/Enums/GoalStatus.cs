namespace Mdar.Core.Enums;

/// <summary>
/// حالة الهدف
/// </summary>
public enum GoalStatus
{
    /// <summary>مسودة - لم يبدأ العمل عليه</summary>
    Draft = 0,

    /// <summary>نشط وجارٍ متابعته</summary>
    Active = 1,

    /// <summary>محقق بنجاح</summary>
    Completed = 2,

    /// <summary>متخلى عنه</summary>
    Abandoned = 3
}
