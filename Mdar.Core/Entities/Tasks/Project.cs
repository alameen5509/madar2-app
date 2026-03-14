using Mdar.Core.Entities.Common;
using Mdar.Core.Entities.Goals;
using Mdar.Core.Entities.Identity;
using Mdar.Core.Entities.Notes;
using Mdar.Core.Enums;

namespace Mdar.Core.Entities.Tasks;

/// <summary>
/// مشروع يجمع مجموعة من المهام المترابطة نحو هدف موحّد.
/// يمكن ربط المشروع بهدف استراتيجي (Goal) لتتبع التقدم نحو الأهداف الكبرى.
/// </summary>
public class Project : BaseEntity
{
    /// <summary>عنوان المشروع</summary>
    public required string Title { get; set; }

    /// <summary>وصف تفصيلي للمشروع وأهدافه (اختياري)</summary>
    public string? Description { get; set; }

    /// <summary>الحالة الحالية للمشروع</summary>
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

    /// <summary>الأولوية النسبية للمشروع مقارنة بالمشاريع الأخرى</summary>
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    /// <summary>
    /// لون المشروع بصيغة HEX لتمييزه بصرياً في لوحة التحكم.
    /// مثال: "#8B5CF6" (بنفسجي).
    /// </summary>
    public string Color { get; set; } = "#8B5CF6";

    /// <summary>أيقونة المشروع (اسم من مكتبة أيقونات)</summary>
    public string? Icon { get; set; }

    /// <summary>تاريخ البدء المخطط أو الفعلي للمشروع</summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>تاريخ الاستحقاق المستهدف لإنهاء المشروع</summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>
    /// تاريخ ووقت إتمام المشروع فعلياً.
    /// يكون null ما لم تُضبط الحالة على Completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    // ─── Foreign Keys ─────────────────────────────────────────────────────────

    /// <summary>معرّف المستخدم المالك للمشروع</summary>
    public Guid UserId { get; set; }

    /// <summary>التصنيف الذي ينتمي إليه المشروع (اختياري)</summary>
    public Guid? CategoryId { get; set; }

    // ─── Navigation Properties ────────────────────────────────────────────────

    public User User { get; set; } = null!;
    public Category? Category { get; set; }
    public ICollection<TaskItem> Tasks { get; set; } = [];
    public ICollection<Goal> Goals { get; set; } = [];
    public ICollection<Note> Notes { get; set; } = [];
}
