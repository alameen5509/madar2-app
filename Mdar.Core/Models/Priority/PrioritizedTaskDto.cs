using Mdar.Core.Enums;
using TaskStatus = Mdar.Core.Enums.TaskStatus;

namespace Mdar.Core.Models.Priority;

/// <summary>
/// مهمة مرتبة بعد معالجتها عبر محرك الأولويات.
/// تحمل البيانات الأساسية للعرض + الوزن المحسوب + التفسير الاختياري.
/// </summary>
public sealed record PrioritizedTaskDto
{
    // ─── بيانات المهمة الأساسية ────────────────────────────────────────────

    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public TaskStatus Status { get; init; }
    public TaskPriority Priority { get; init; }
    public ContextTag ContextTag { get; init; }
    public PrayerPeriod? PreferredPrayerPeriod { get; init; }
    public DateOnly? DueDate { get; init; }

    // ─── بيانات الطماطم ────────────────────────────────────────────────────

    public bool IsPomodoroCompatible { get; init; }
    public int? EstimatedPomodoros { get; init; }
    public int CompletedPomodoros { get; init; }

    // ─── ترتيب الأولوية ─────────────────────────────────────────────────────

    /// <summary>
    /// الوزن الإجمالي المحسوب. يُستخدم للترتيب التنازلي.
    /// الأعلى = الأهم.
    /// </summary>
    public double PriorityWeight { get; init; }

    /// <summary>
    /// ترتيب المهمة في القائمة النهائية (1 = الأعلى أولوية).
    /// </summary>
    public int Rank { get; init; }

    /// <summary>
    /// تفصيل مكونات الوزن (متوفر فقط إذا كان IncludeWeightBreakdown = true).
    /// null = لم يُطلَب التفصيل.
    /// </summary>
    public TaskWeightBreakdown? WeightBreakdown { get; init; }
}
