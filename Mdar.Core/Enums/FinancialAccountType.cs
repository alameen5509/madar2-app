namespace Mdar.Core.Enums;

/// <summary>
/// نوع الحساب المالي
/// </summary>
public enum FinancialAccountType
{
    /// <summary>نقد في اليد</summary>
    Cash = 0,

    /// <summary>حساب بنكي</summary>
    BankAccount = 1,

    /// <summary>بطاقة ائتمان</summary>
    CreditCard = 2,

    /// <summary>محفظة إلكترونية</summary>
    DigitalWallet = 3,

    /// <summary>استثمار</summary>
    Investment = 4
}
