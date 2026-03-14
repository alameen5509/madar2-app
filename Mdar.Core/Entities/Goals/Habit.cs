using Mdar.Core.Entities.Common;
using Mdar.Core.Entities.Identity;
using Mdar.Core.Enums;

namespace Mdar.Core.Entities.Goals;

/// <summary>
/// عادة - نشاط متكرر يريد المستخدم بناءه أو الحفاظ عليه.
/// يمتلك نظام Streak (سلسلة التزام) لتحفيز الاستمرارية.
/// مثال: "قراءة 30 دقيقة يومياً"، "ممارسة الرياضة 3x أسبوعياً".
/// </summary>
public class Habit : BaseEntity
{
    /// <summary>اسم العادة</summary>
    public required string Title { get; set; }

    /// <summary>وصف أو سبب بناء هذه العادة (اختياري)</summary>
    public string? Description { get; set; }

    /// <summary>تكرار العادة: يومي، أسبوعي، شهري</summary>
    public HabitFrequency Frequency { get; set; } = HabitFrequency.Daily;

    /// <summary>
    /// العدد المستهدف للتنفيذ في الدورة الواحدة.
    /// للعادة الأسبوعية بـ 3 مرات أسبوعياً → TargetCount = 3.
    /// للعادة اليومية → TargetCount = 1 عادةً.
    /// </summary>
    public int TargetCount { get; set; } = 1;

    /// <summary>
    /// وحدة قياس العادة (اختياري).
    /// مثال: "دقيقة"، "صفحة"، "كم"، "مرة".
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>لون العادة بصيغة HEX للتمييز البصري</summary>
    public string Color { get; set; } = "#F59E0B";

    /// <summary>أيقونة العادة</summary>
    public string? Icon { get; set; }

    /// <summary>
    /// طول السلسلة الحالية (Streak) بعدد الأيام/الأسابيع المتتالية.
    /// يُعاد إلى 0 إذا فاتت دورة.
    /// </summary>
    public int CurrentStreak { get; set; } = 0;

    /// <summary>أطول سلسلة حققها المستخدم لهذه العادة</summary>
    public int LongestStreak { get; set; } = 0;

    /// <summary>هل العادة نشطة؟ أم تم إيقافها مؤقتاً؟</summary>
    public bool IsActive { get; set; } = true;

    // ─── Foreign Keys ─────────────────────────────────────────────────────────

    /// <summary>معرّف المستخدم المالك للعادة</summary>
    public Guid UserId { get; set; }

    // ─── Navigation Properties ────────────────────────────────────────────────

    public User User { get; set; } = null!;

    /// <summary>سجلات التنفيذ اليومية/الأسبوعية لهذه العادة</summary>
    public ICollection<HabitLog> Logs { get; set; } = [];
}
