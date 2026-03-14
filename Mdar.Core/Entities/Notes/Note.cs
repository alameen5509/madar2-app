using Mdar.Core.Entities.Common;
using Mdar.Core.Entities.Contacts;
using Mdar.Core.Entities.Identity;
using Mdar.Core.Entities.Tasks;

namespace Mdar.Core.Entities.Notes;

/// <summary>
/// ملاحظة - وحدة تخزين المعرفة والأفكار والمحتوى النصي.
/// يمكن ربط الملاحظة بمهمة أو مشروع أو جهة اتصال لتوفير السياق.
/// تدعم الملاحظات الأوسام (Tags) للتنظيم المرن.
///
/// محتوى الملاحظة (Content) مخزَّن بصيغة Markdown لدعم التنسيق
/// الغني دون الاعتماد على محرر HTML.
/// </summary>
public class Note : BaseEntity
{
    /// <summary>عنوان الملاحظة (اختياري - قد تكون بلا عنوان)</summary>
    public string? Title { get; set; }

    /// <summary>
    /// محتوى الملاحظة بصيغة Markdown.
    /// يدعم: عناوين، قوائم، كود، جداول، روابط.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// هل الملاحظة مثبّتة في الأعلى؟
    /// الملاحظات المثبّتة تظهر أولاً في القوائم.
    /// </summary>
    public bool IsPinned { get; set; } = false;

    /// <summary>
    /// لون خلفية الملاحظة بصيغة HEX (مثال: "#FEF3C7" أصفر فاتح).
    /// يُستخدم لتصنيف الملاحظات بصرياً كـ Post-it Notes.
    /// </summary>
    public string? Color { get; set; }

    // ─── Foreign Keys ─────────────────────────────────────────────────────────

    /// <summary>معرّف المستخدم المالك للملاحظة</summary>
    public Guid UserId { get; set; }

    /// <summary>المشروع المرتبط بهذه الملاحظة (اختياري)</summary>
    public Guid? ProjectId { get; set; }

    /// <summary>المهمة المرتبطة بهذه الملاحظة (اختياري)</summary>
    public Guid? TaskItemId { get; set; }

    /// <summary>جهة الاتصال المرتبطة بهذه الملاحظة (اختياري)</summary>
    public Guid? ContactId { get; set; }

    // ─── Navigation Properties ────────────────────────────────────────────────

    public User User { get; set; } = null!;
    public Project? Project { get; set; }
    public TaskItem? TaskItem { get; set; }
    public Contact? Contact { get; set; }

    /// <summary>الأوسام المرفقة بهذه الملاحظة</summary>
    public ICollection<NoteTag> NoteTags { get; set; } = [];
}
