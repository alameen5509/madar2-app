using Mdar.Core.Entities.Common;
using Mdar.Core.Entities.Identity;

namespace Mdar.Core.Entities.Tasks;

/// <summary>
/// تصنيف يُنظِّم المهام والمشاريع والمعاملات المالية.
/// التصنيفات قابلة للتخصيص بلون وأيقونة لسهولة التمييز البصري.
/// </summary>
public class Category : BaseEntity
{
    /// <summary>اسم التصنيف (مثال: "عمل"، "شخصي"، "تعليم")</summary>
    public required string Name { get; set; }

    /// <summary>
    /// وصف اختياري يوضح متى يُستخدم هذا التصنيف
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// لون التصنيف بصيغة HEX (مثال: "#FF5733").
    /// يُستخدم في الواجهة لتمييز التصنيفات بصرياً.
    /// </summary>
    public string Color { get; set; } = "#6B7280";

    /// <summary>
    /// اسم الأيقونة (من مكتبة Lucide أو Material Icons).
    /// مثال: "briefcase"، "book"، "heart".
    /// </summary>
    public string? Icon { get; set; }

    // ─── Foreign Keys ─────────────────────────────────────────────────────────

    /// <summary>معرّف المستخدم المالك لهذا التصنيف</summary>
    public Guid UserId { get; set; }

    // ─── Navigation Properties ────────────────────────────────────────────────

    public User User { get; set; } = null!;
    public ICollection<TaskItem> Tasks { get; set; } = [];
    public ICollection<Project> Projects { get; set; } = [];
}
