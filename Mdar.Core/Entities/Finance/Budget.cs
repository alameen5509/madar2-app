using Mdar.Core.Entities.Common;
using Mdar.Core.Entities.Identity;
using Mdar.Core.Entities.Tasks;

namespace Mdar.Core.Entities.Finance;

/// <summary>
/// ميزانية - حد مالي لفئة معينة في فترة زمنية محددة.
/// مثال: "ميزانية الطعام في مارس = 1500 ريال".
/// تُستخدم لتنبيه المستخدم حين يقترب من تجاوز الحد.
/// </summary>
public class Budget : BaseEntity
{
    /// <summary>اسم الميزانية (مثال: "مصاريف الطعام - مارس 2026")</summary>
    public required string Name { get; set; }

    /// <summary>الحد الأقصى المخصص لهذه الميزانية</summary>
    public required decimal LimitAmount { get; set; }

    /// <summary>
    /// المبلغ المصروف فعلياً حتى الآن من هذه الميزانية.
    /// يُحدَّث تلقائياً مع كل معاملة ضمن نطاق الميزانية.
    /// </summary>
    public decimal SpentAmount { get; set; } = 0;

    /// <summary>
    /// نسبة الإنفاق = SpentAmount / LimitAmount * 100.
    /// Computed Property - لا تُخزَّن في قاعدة البيانات.
    /// </summary>
    public decimal SpentPercentage => LimitAmount > 0 ? SpentAmount / LimitAmount * 100 : 0;

    /// <summary>المبلغ المتبقي من الميزانية</summary>
    public decimal RemainingAmount => LimitAmount - SpentAmount;

    /// <summary>تاريخ بدء الميزانية</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>تاريخ انتهاء الميزانية</summary>
    public DateOnly EndDate { get; set; }

    // ─── Foreign Keys ─────────────────────────────────────────────────────────

    /// <summary>معرّف المستخدم المالك للميزانية</summary>
    public Guid UserId { get; set; }

    /// <summary>التصنيف المالي الذي تغطيه هذه الميزانية</summary>
    public Guid? CategoryId { get; set; }

    // ─── Navigation Properties ────────────────────────────────────────────────

    public User User { get; set; } = null!;
    public Category? Category { get; set; }
}
