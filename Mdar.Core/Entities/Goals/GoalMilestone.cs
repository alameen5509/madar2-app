using Mdar.Core.Entities.Common;

namespace Mdar.Core.Entities.Goals;

/// <summary>
/// مرحلة (Milestone) ضمن الهدف الاستراتيجي.
/// تمثل نقطة تحقق واضحة على الطريق نحو الهدف الكامل.
/// مثال لهدف "تعلم Flutter":
///   - مرحلة 1: "إنهاء Dart أساسيات"
///   - مرحلة 2: "بناء تطبيق TODO"
///   - مرحلة 3: "نشر تطبيق على Play Store"
/// </summary>
public class GoalMilestone : BaseEntity
{
    /// <summary>عنوان المرحلة - نتيجة محددة وقابلة للقياس</summary>
    public required string Title { get; set; }

    /// <summary>وصف اختياري يوضح معايير اعتبار المرحلة مكتملة</summary>
    public string? Description { get; set; }

    /// <summary>هل هذه المرحلة مكتملة؟</summary>
    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// تاريخ ووقت إتمام المرحلة.
    /// يُضبط عند تحديد IsCompleted = true.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>تاريخ الاستحقاق المستهدف لهذه المرحلة (اختياري)</summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>
    /// الترتيب العرضي للمرحلة ضمن الهدف.
    /// يُستخدم لترتيب المراحل في الواجهة.
    /// </summary>
    public int SortOrder { get; set; } = 0;

    // ─── Foreign Keys ─────────────────────────────────────────────────────────

    /// <summary>معرّف الهدف الأم</summary>
    public Guid GoalId { get; set; }

    // ─── Navigation Properties ────────────────────────────────────────────────

    public Goal Goal { get; set; } = null!;
}
