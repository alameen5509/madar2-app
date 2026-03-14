namespace Mdar.Core.Enums;

/// <summary>
/// نوع الحركة المالية
/// </summary>
public enum TransactionType
{
    /// <summary>دخل - أموال واردة</summary>
    Income = 0,

    /// <summary>مصروف - أموال صادرة</summary>
    Expense = 1,

    /// <summary>تحويل بين حسابين</summary>
    Transfer = 2
}
