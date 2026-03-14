using Mdar.Core.Entities.Common;
using Mdar.Core.Entities.Identity;
using Mdar.Core.Entities.Tasks;
using Mdar.Core.Enums;

namespace Mdar.Core.Entities.Goals;

/// <summary>
/// هدف استراتيجي طويل المدى.
/// يتكون من مراحل (Milestones) وقد يرتبط بمشروع واحد أو أكثر.
///
/// الفرق بين Goal و Project:
///   - Goal: "ماذا أريد أن أحقق؟" - نتيجة مستقبلية (مثال: "أتعلم Flutter")
///   - Project: "كيف سأحققه؟" - مجموعة مهام ملموسة (مثال: "بناء تطبيق أول")
/// </summary>
public class Goal : BaseEntity
{
    /// <summary>عنوان الهدف - ملهِم وواضح</summary>
    public required string Title { get; set; }

    /// <summary>وصف تفصيلي: لماذا هذا الهدف مهم؟ ما الذي سيتغير؟</summary>
    public string? Description { get; set; }

    /// <summary>الحالة الحالية للهدف في رحلة تحقيقه</summary>
    public GoalStatus Status { get; set; } = GoalStatus.Draft;

    /// <summary>الأولوية النسبية مقارنة بالأهداف الأخرى</summary>
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    /// <summary>
    /// نسبة التقدم نحو تحقيق الهدف (0-100).
    /// يمكن تحديثها يدوياً أو حسابها من المراحل المكتملة.
    /// </summary>
    public int ProgressPercentage { get; set; } = 0;

    /// <summary>التاريخ المستهدف لتحقيق الهدف</summary>
    public DateOnly? TargetDate { get; set; }

    /// <summary>
    /// تاريخ ووقت تحقيق الهدف فعلياً.
    /// يُضبط عند تغيير Status إلى Completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    // ─── Foreign Keys ─────────────────────────────────────────────────────────

    /// <summary>معرّف المستخدم المالك للهدف</summary>
    public Guid UserId { get; set; }

    /// <summary>المشروع الرئيسي المرتبط بهذا الهدف (اختياري)</summary>
    public Guid? ProjectId { get; set; }

    // ─── Navigation Properties ────────────────────────────────────────────────

    public User User { get; set; } = null!;
    public Project? Project { get; set; }

    /// <summary>المراحل التفصيلية لتحقيق هذا الهدف</summary>
    public ICollection<GoalMilestone> Milestones { get; set; } = [];
}
