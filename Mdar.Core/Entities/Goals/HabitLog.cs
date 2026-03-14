using Mdar.Core.Entities.Common;

namespace Mdar.Core.Entities.Goals;

/// <summary>
/// سجل تنفيذ العادة في يوم أو أسبوع محدد.
/// كل إدخال يمثل مرة نفّذ فيها المستخدم العادة.
/// يُستخدم لبناء الرسوم البيانية (Heatmap) وحساب الـ Streak.
/// </summary>
public class HabitLog : BaseEntity
{
    /// <summary>
    /// تاريخ تنفيذ العادة.
    /// للعادات اليومية: يوم محدد.
    /// للعادات الأسبوعية: اليوم الأول من الأسبوع.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// عدد مرات التنفيذ في هذا اليوم.
    /// عادةً = 1، لكن قد يكون أكثر إذا كانت العادة "اشرب 8 أكواب".
    /// </summary>
    public int Count { get; set; } = 1;

    /// <summary>ملاحظة أو تعليق على هذا اليوم (اختياري)</summary>
    public string? Notes { get; set; }

    // ─── Foreign Keys ─────────────────────────────────────────────────────────

    /// <summary>معرّف العادة المرتبط بها هذا السجل</summary>
    public Guid HabitId { get; set; }

    // ─── Navigation Properties ────────────────────────────────────────────────

    public Habit Habit { get; set; } = null!;
}
