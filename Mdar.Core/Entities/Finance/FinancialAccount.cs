using Mdar.Core.Entities.Common;
using Mdar.Core.Entities.Identity;
using Mdar.Core.Enums;

namespace Mdar.Core.Entities.Finance;

/// <summary>
/// حساب مالي - يمثل وعاءً مالياً واحداً (محفظة، بنك، بطاقة...).
/// الرصيد يُحسب تراكمياً من جميع المعاملات المرتبطة بالحساب،
/// لكنه مخزَّن هنا أيضاً لأغراض الأداء (Denormalization).
/// </summary>
public class FinancialAccount : BaseEntity
{
    /// <summary>اسم الحساب (مثال: "محفظة نقدية"، "بنك الراجحي")</summary>
    public required string Name { get; set; }

    /// <summary>وصف اختياري أو ملاحظات عن الحساب</summary>
    public string? Description { get; set; }

    /// <summary>
    /// نوع الحساب المالي لتصنيف طريقة الدفع.
    /// </summary>
    public FinancialAccountType AccountType { get; set; } = FinancialAccountType.Cash;

    /// <summary>
    /// الرصيد الحالي للحساب.
    /// يُحدَّث تلقائياً مع كل معاملة.
    /// القيمة السالبة ممكنة في بطاقات الائتمان.
    /// </summary>
    public decimal Balance { get; set; } = 0;

    /// <summary>
    /// العملة المستخدمة في هذا الحساب بصيغة ISO 4217.
    /// مثال: "SAR"، "USD"، "EGP".
    /// </summary>
    public string Currency { get; set; } = "SAR";

    /// <summary>
    /// لون الحساب بصيغة HEX لتمييزه في الواجهة.
    /// </summary>
    public string Color { get; set; } = "#10B981";

    /// <summary>هل الحساب نشط؟ الحسابات المغلقة تُخفى لكن لا تُحذف</summary>
    public bool IsActive { get; set; } = true;

    // ─── Foreign Keys ─────────────────────────────────────────────────────────

    /// <summary>معرّف المستخدم المالك للحساب</summary>
    public Guid UserId { get; set; }

    // ─── Navigation Properties ────────────────────────────────────────────────

    public User User { get; set; } = null!;

    /// <summary>جميع المعاملات الصادرة والواردة لهذا الحساب</summary>
    public ICollection<Transaction> Transactions { get; set; } = [];

    /// <summary>المعاملات الواردة إليه كحساب مستلِم في التحويلات</summary>
    public ICollection<Transaction> IncomingTransfers { get; set; } = [];
}
