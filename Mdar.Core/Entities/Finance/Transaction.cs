using Mdar.Core.Entities.Common;
using Mdar.Core.Entities.Contacts;
using Mdar.Core.Entities.Identity;
using Mdar.Core.Entities.Tasks;
using Mdar.Core.Enums;

namespace Mdar.Core.Entities.Finance;

/// <summary>
/// معاملة مالية - سجل كل حركة مالية (دخل، مصروف، تحويل).
/// تُبنى التقارير المالية والإحصاءات من مجموع هذه السجلات.
/// </summary>
public class Transaction : BaseEntity
{
    /// <summary>
    /// المبلغ بالقيمة المطلقة (دائماً موجب).
    /// الإشارة (دخل/مصروف) تُحدَّد عبر خاصية Type.
    /// </summary>
    public required decimal Amount { get; set; }

    /// <summary>وصف المعاملة (مثال: "فاتورة كهرباء"، "راتب شهر مارس")</summary>
    public required string Description { get; set; }

    /// <summary>
    /// تاريخ حدوث المعاملة فعلياً.
    /// قد يختلف عن CreatedAt في حالة إدخال معاملات تاريخية.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>نوع المعاملة: دخل، مصروف، أو تحويل</summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// ملاحظات إضافية أو سياق حول المعاملة (اختياري).
    /// مفيد لتدوين تفاصيل لن تظهر في الوصف القصير.
    /// </summary>
    public string? Notes { get; set; }

    // ─── Foreign Keys ─────────────────────────────────────────────────────────

    /// <summary>معرّف المستخدم صاحب المعاملة</summary>
    public Guid UserId { get; set; }

    /// <summary>الحساب المالي الذي صدرت منه هذه المعاملة</summary>
    public Guid FinancialAccountId { get; set; }

    /// <summary>
    /// الحساب المستلِم في حالة التحويل (Transfer).
    /// يجب أن يكون محدداً عندما Type = Transfer.
    /// </summary>
    public Guid? ToFinancialAccountId { get; set; }

    /// <summary>التصنيف المالي للمعاملة (مثال: "طعام"، "مواصلات"، "ترفيه")</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>جهة الاتصال المرتبطة بالمعاملة (مثال: العميل، المورّد)</summary>
    public Guid? ContactId { get; set; }

    // ─── Navigation Properties ────────────────────────────────────────────────

    public User User { get; set; } = null!;
    public FinancialAccount FinancialAccount { get; set; } = null!;
    public FinancialAccount? ToFinancialAccount { get; set; }
    public Category? Category { get; set; }
    public Contact? Contact { get; set; }
}
