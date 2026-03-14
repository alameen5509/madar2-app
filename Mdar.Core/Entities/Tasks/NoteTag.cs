using Mdar.Core.Entities.Notes;

namespace Mdar.Core.Entities.Tasks;

/// <summary>
/// جدول الربط بين الملاحظات والأوسام (Many-to-Many).
/// يتيح وضع نفس الوسم على ملاحظات ومهام في آنٍ واحد.
/// </summary>
public class NoteTag
{
    /// <summary>معرّف الملاحظة</summary>
    public Guid NoteId { get; set; }

    /// <summary>معرّف الوسم</summary>
    public Guid TagId { get; set; }

    // ─── Navigation Properties ────────────────────────────────────────────────

    public Note Note { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
