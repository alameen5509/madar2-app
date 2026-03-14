using Mdar.Core.Entities.Common;
using Mdar.Core.Entities.Identity;

namespace Mdar.Core.Entities.Thinking;

/// <summary>
/// لوحة التفكير اللانهائية — تحتوي على مجموعة من البطاقات المرئية.
/// كل مستخدم يمكنه امتلاك عدة لوحات (مشاريع، مواضيع، إلخ).
/// </summary>
public class ThinkingBoard : BaseEntity
{
    public string Title { get; set; } = "لوحتي";

    public string? Description { get; set; }

    // ── علاقة المستخدم ────────────────────────────────────────────────────────
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // ── بطاقات اللوحة ─────────────────────────────────────────────────────────
    public ICollection<ThinkingCard> Cards { get; set; } = new List<ThinkingCard>();
}
