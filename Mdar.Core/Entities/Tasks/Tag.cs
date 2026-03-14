using Mdar.Core.Entities.Common;
using Mdar.Core.Entities.Identity;

namespace Mdar.Core.Entities.Tasks;

/// <summary>
/// وسم (Label) يمكن إضافته لأكثر من مهمة أو ملاحظة.
/// على عكس التصنيف (Category)، يمكن للمهمة الواحدة أن تحمل أوساماً متعددة.
/// العلاقة مع المهام: Many-to-Many عبر جدول TaskTag.
/// </summary>
public class Tag : BaseEntity
{
    /// <summary>اسم الوسم (مثال: "عاجل"، "للمراجعة"، "منزل")</summary>
    public required string Name { get; set; }

    /// <summary>
    /// لون الوسم بصيغة HEX.
    /// يُستخدم لعرض Badge ملوّن في الواجهة.
    /// </summary>
    public string Color { get; set; } = "#3B82F6";

    // ─── Foreign Keys ─────────────────────────────────────────────────────────

    /// <summary>معرّف المستخدم المالك لهذا الوسم</summary>
    public Guid UserId { get; set; }

    // ─── Navigation Properties ────────────────────────────────────────────────

    public User User { get; set; } = null!;
    public ICollection<TaskTag> TaskTags { get; set; } = [];
    public ICollection<NoteTag> NoteTags { get; set; } = [];
}
