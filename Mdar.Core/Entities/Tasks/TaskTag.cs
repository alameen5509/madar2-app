namespace Mdar.Core.Entities.Tasks;

/// <summary>
/// جدول الربط بين المهام والأوسام (Many-to-Many).
/// لا يرث BaseEntity لأنه جدول وصل (Junction Table) بسيط
/// يُعرَّف بمفتاح مركّب (TaskItemId + TagId).
/// </summary>
public class TaskTag
{
    /// <summary>معرّف المهمة</summary>
    public Guid TaskItemId { get; set; }

    /// <summary>معرّف الوسم</summary>
    public Guid TagId { get; set; }

    // ─── Navigation Properties ────────────────────────────────────────────────

    public TaskItem TaskItem { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
