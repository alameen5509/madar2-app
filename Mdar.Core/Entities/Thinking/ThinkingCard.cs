using Mdar.Core.Entities.Common;
using Mdar.Core.Enums;

namespace Mdar.Core.Entities.Thinking;

/// <summary>
/// بطاقة تفكير داخل اللوحة — تمثّل فكرة، ملاحظة، مهمة، أو سؤالاً.
/// تُخزَّن إحداثياتها وأبعادها لاستعادة موضعها بدقة عند كل فتح للوحة.
/// </summary>
public class ThinkingCard : BaseEntity
{
    // ── ارتباط اللوحة ─────────────────────────────────────────────────────────
    public Guid BoardId { get; set; }
    public ThinkingBoard Board { get; set; } = null!;

    /// <summary>المستخدم المالك — للفلترة السريعة دون JOIN مع Board</summary>
    public Guid UserId { get; set; }

    // ── المحتوى ───────────────────────────────────────────────────────────────
    public string Title { get; set; } = "فكرة جديدة";

    public string Content { get; set; } = string.Empty;

    // ── نوع البطاقة ولونها ────────────────────────────────────────────────────
    public CardType CardType { get; set; } = CardType.Note;

    /// <summary>لون خلفية البطاقة (Hex) — يُستخدم للتخصيص البصري</summary>
    public string Color { get; set; } = "#1e293b";

    // ── الموضع والأبعاد (بالبكسل على اللوحة قبل التحويل) ─────────────────────
    public double PositionX { get; set; } = 100;
    public double PositionY { get; set; } = 100;
    public double Width { get; set; } = 220;
    public double Height { get; set; } = 160;

    /// <summary>ترتيب الطبقات — القيمة الأعلى تظهر فوق البطاقات الأخرى</summary>
    public int ZIndex { get; set; } = 0;
}
