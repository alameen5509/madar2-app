using Mdar.Core.Entities.Common;
using Mdar.Core.Entities.Identity;
using Mdar.Core.Entities.Notes;
using Mdar.Core.Enums;
using TaskStatus = Mdar.Core.Enums.TaskStatus;

namespace Mdar.Core.Entities.Tasks;

/// <summary>
/// المهمة - الوحدة الأساسية في نظام إدارة الإنتاجية.
///
/// الخاصية المحورية: <see cref="IsPomodoroCompatible"/>
///   - true  → المهمة قابلة للتقسيم بنظام الطماطم (25 دقيقة + راحة)
///             مثال: "كتابة تقرير"، "مراجعة كود"، "قراءة كتاب"
///   - false → المهمة غير قابلة للتقسيم بهذا النظام
///             مثال: "انتظار رد بريد"، "اجتماع"، "تحميل ملف"
///
/// دعم المهام الفرعية: يمكن للمهمة أن تحتوي على مهام فرعية عبر ParentTaskId.
/// </summary>
public class TaskItem : BaseEntity
{
    /// <summary>عنوان المهمة - واضح وقابل للتنفيذ</summary>
    public required string Title { get; set; }

    /// <summary>وصف تفصيلي للمهمة: السياق، المتطلبات، معايير الإنجاز</summary>
    public string? Description { get; set; }

    /// <summary>الحالة الحالية للمهمة في دورة حياتها</summary>
    public TaskStatus Status { get; set; } = TaskStatus.Pending;

    /// <summary>مستوى الأولوية لترتيب المهام حسب الأهمية</summary>
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    // ─── Pomodoro Integration ─────────────────────────────────────────────────

    /// <summary>
    /// هل المهمة قابلة للتنفيذ بنظام الطماطم (Pomodoro Technique)؟
    ///
    /// true  → المهمة تحتاج تركيزاً عميقاً ويمكن تقسيمها لجلسات 25 دقيقة.
    ///         ستظهر في واجهة تتبع الطماطم وتُحسب لها جلسات.
    ///
    /// false → المهمة إدارية أو انتظار أو قصيرة جداً/طويلة جداً
    ///         بحيث لا يناسبها نظام الطماطم.
    ///
    /// القيمة الافتراضية: true لأن معظم مهام العمل قابلة للتطبيق.
    /// </summary>
    public bool IsPomodoroCompatible { get; set; } = true;

    /// <summary>
    /// العدد المقدَّر من جلسات الطماطم لإنهاء المهمة.
    /// يُستخدم للتخطيط اليومي ومعرفة حجم المهمة.
    /// null = لم يُحدَّد بعد.
    /// </summary>
    public int? EstimatedPomodoros { get; set; }

    /// <summary>
    /// عدد جلسات الطماطم المكتملة فعلياً على هذه المهمة.
    /// يُحدَّث تلقائياً عند إنهاء كل جلسة.
    /// </summary>
    public int CompletedPomodoros { get; set; } = 0;

    // ─── Priority Engine Context ──────────────────────────────────────────────

    /// <summary>
    /// السياق المكاني المطلوب لتنفيذ هذه المهمة.
    /// يُستخدم في محرك الأولويات لاستبعاد المهام غير الملائمة للموقع الحالي.
    /// القيمة الافتراضية Anywhere = تظهر دائماً بغض النظر عن الموقع.
    /// </summary>
    public ContextTag ContextTag { get; set; } = ContextTag.Anywhere;

    /// <summary>
    /// هل المهمة في وضع الطوارئ؟
    ///
    /// true  → مهمة حرجة تستدعي التعامل الفوري خارج التسلسل الاعتيادي.
    ///         تُستبعد من محرك الأولويات العادي وتُعالَج بمسار منفصل.
    ///         مثال: "انقطع الخادم"، "موعد طبي طارئ".
    ///
    /// false → ضمن التدفق العادي للمحرك (القيمة الافتراضية).
    /// </summary>
    public bool IsEmergency { get; set; } = false;

    /// <summary>
    /// فترة الصلاة المفضلة لتنفيذ هذه المهمة.
    /// null = لا تفضيل — المهمة مناسبة لأي وقت.
    ///
    /// يُستخدم في حساب PriorityWeight:
    ///   - تطابق تام مع الفترة الحالية → مضاعف 1.5
    ///   - فترة مجاورة               → مضاعف 1.2
    ///   - لا تفضيل (null)            → مضاعف 1.0
    ///   - فترة مختلفة               → مضاعف 0.85
    /// </summary>
    public PrayerPeriod? PreferredPrayerPeriod { get; set; }

    // ─── Scheduling ───────────────────────────────────────────────────────────

    /// <summary>تاريخ استحقاق المهمة (اختياري)</summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>
    /// وقت التذكير بالمهمة (UTC).
    /// يُستخدم لإرسال إشعار قبل موعد الاستحقاق.
    /// </summary>
    public DateTime? ReminderAt { get; set; }

    /// <summary>
    /// تاريخ ووقت إتمام المهمة فعلياً.
    /// يُضبط تلقائياً حين تُغيَّر الحالة إلى Completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    // ─── Foreign Keys ─────────────────────────────────────────────────────────

    /// <summary>معرّف المستخدم المالك للمهمة</summary>
    public Guid UserId { get; set; }

    /// <summary>المشروع الذي تنتمي إليه المهمة (اختياري - قد تكون مهمة مستقلة)</summary>
    public Guid? ProjectId { get; set; }

    /// <summary>التصنيف الذي تنتمي إليه المهمة (اختياري)</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// معرّف المهمة الأم في حالة المهام الفرعية (Self-Referencing).
    /// null = مهمة رئيسية مستقلة.
    /// </summary>
    public Guid? ParentTaskId { get; set; }

    // ─── Navigation Properties ────────────────────────────────────────────────

    public User User { get; set; } = null!;
    public Project? Project { get; set; }
    public Category? Category { get; set; }

    /// <summary>المهمة الأم (للمهام الفرعية)</summary>
    public TaskItem? ParentTask { get; set; }

    /// <summary>قائمة المهام الفرعية المنبثقة من هذه المهمة</summary>
    public ICollection<TaskItem> SubTasks { get; set; } = [];

    /// <summary>جلسات الطماطم المرتبطة بهذه المهمة</summary>
    public ICollection<PomodoroSession> PomodoroSessions { get; set; } = [];

    /// <summary>الأوسام المرفقة بهذه المهمة</summary>
    public ICollection<TaskTag> TaskTags { get; set; } = [];

    /// <summary>الملاحظات المرتبطة بهذه المهمة</summary>
    public ICollection<Note> Notes { get; set; } = [];
}
