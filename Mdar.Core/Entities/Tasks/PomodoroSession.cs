using Mdar.Core.Entities.Common;
using Mdar.Core.Entities.Identity;

namespace Mdar.Core.Entities.Tasks;

/// <summary>
/// جلسة طماطم (Pomodoro Session) - سجل كل جلسة تركيز أو استراحة.
///
/// كيف يعمل نظام الطماطم:
///   1. تعمل 25 دقيقة بتركيز تام (IsBreak = false)
///   2. تأخذ استراحة 5 دقائق (IsBreak = true, IsLongBreak = false)
///   3. بعد 4 جلسات، تأخذ استراحة طويلة 15-30 دقيقة (IsBreak = true, IsLongBreak = true)
///
/// الجلسات دائماً مرتبطة بمهمة (TaskItem) حتى نعرف كم أنفقنا من الوقت على كل مهمة.
/// </summary>
public class PomodoroSession : BaseEntity
{
    /// <summary>وقت بدء الجلسة (UTC)</summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// وقت انتهاء الجلسة (UTC).
    /// null = الجلسة لا تزال جارية.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// المدة المخططة للجلسة بالدقائق.
    /// تُؤخذ من إعدادات المستخدم عند بدء الجلسة.
    /// </summary>
    public int PlannedDurationMinutes { get; set; }

    /// <summary>
    /// المدة الفعلية التي قضاها المستخدم في الجلسة بالدقائق.
    /// يُحسب عند انتهاء الجلسة: (EndTime - StartTime).TotalMinutes.
    /// </summary>
    public int? ActualDurationMinutes { get; set; }

    /// <summary>
    /// هل الجلسة اكتملت بنجاح؟
    /// false = توقف المستخدم قبل نهاية الوقت المحدد.
    /// </summary>
    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// هل هذه جلسة استراحة؟
    /// true  = استراحة (قصيرة أو طويلة).
    /// false = جلسة تركيز وعمل.
    /// </summary>
    public bool IsBreak { get; set; } = false;

    /// <summary>
    /// هل هذه استراحة طويلة (Long Break)؟
    /// تكون true فقط عندما IsBreak = true أيضاً.
    /// الاستراحة الطويلة تأتي بعد كل 4 جلسات تركيز متتالية.
    /// </summary>
    public bool IsLongBreak { get; set; } = false;

    /// <summary>
    /// ملاحظات أو أفكار سجّلها المستخدم أثناء الجلسة أو بعدها.
    /// مفيد لتتبع ما أنجزه في هذه الجلسة تحديداً.
    /// </summary>
    public string? Notes { get; set; }

    // ─── Foreign Keys ─────────────────────────────────────────────────────────

    /// <summary>معرّف المستخدم صاحب الجلسة</summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// معرّف المهمة التي تعمل عليها في هذه الجلسة.
    /// يجب أن تكون المهمة IsPomodoroCompatible = true.
    /// </summary>
    public Guid TaskItemId { get; set; }

    // ─── Navigation Properties ────────────────────────────────────────────────

    public User User { get; set; } = null!;
    public TaskItem TaskItem { get; set; } = null!;
}
