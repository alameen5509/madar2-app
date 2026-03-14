using Mdar.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Mdar.API.DTOs.Tasks;

/// <summary>
/// طلب إنشاء مهمة جديدة.
/// يُقبل في جسم الطلب (Request Body) بصيغة JSON.
///
/// بعد الإنشاء، يُحسب الوزن الأولوي تلقائياً ويُعاد في الاستجابة 201.
/// </summary>
public sealed record CreateTaskRequest
{
    // ─── الحقول المطلوبة ──────────────────────────────────────────────────

    /// <summary>
    /// عنوان المهمة. يجب أن يكون واضحاً وقابلاً للتنفيذ.
    /// مثال: "كتابة تقرير الربع الثالث"
    /// </summary>
    [Required(ErrorMessage = "عنوان المهمة مطلوب.")]
    [MinLength(2, ErrorMessage = "العنوان لا يقل عن حرفين.")]
    [MaxLength(300, ErrorMessage = "العنوان لا يتجاوز 300 حرف.")]
    public required string Title { get; init; }

    // ─── الحقول الاختيارية ────────────────────────────────────────────────

    /// <summary>وصف تفصيلي: السياق، المتطلبات، معايير الإنجاز</summary>
    [MaxLength(2000, ErrorMessage = "الوصف لا يتجاوز 2000 حرف.")]
    public string? Description { get; init; }

    /// <summary>مستوى الأولوية. القيمة الافتراضية: Medium</summary>
    public TaskPriority Priority { get; init; } = TaskPriority.Medium;

    /// <summary>
    /// السياق المكاني المطلوب لتنفيذ المهمة.
    /// القيمة الافتراضية: Anywhere (لا قيد مكاني).
    /// </summary>
    public ContextTag ContextTag { get; init; } = ContextTag.Anywhere;

    /// <summary>
    /// هل المهمة في وضع الطوارئ؟
    /// true = تُستبعد من المحرك العادي وتُعالَج فوراً.
    /// </summary>
    public bool IsEmergency { get; init; } = false;

    /// <summary>
    /// هل المهمة قابلة للتنفيذ بنظام الطماطم (جلسات 25 دقيقة)؟
    /// القيمة الافتراضية: true
    /// </summary>
    public bool IsPomodoroCompatible { get; init; } = true;

    /// <summary>
    /// العدد المقدَّر من جلسات الطماطم لإنهاء المهمة.
    /// يجب أن يكون بين 1 و 20 إذا أُدخل.
    /// </summary>
    [Range(1, 20, ErrorMessage = "عدد الطماطم المقدَّر يجب أن يكون بين 1 و 20.")]
    public int? EstimatedPomodoros { get; init; }

    /// <summary>
    /// فترة الصلاة المفضلة لتنفيذ هذه المهمة.
    /// null = مناسبة لأي وقت (لا تفضيل).
    /// </summary>
    public PrayerPeriod? PreferredPrayerPeriod { get; init; }

    /// <summary>تاريخ استحقاق المهمة (اختياري)</summary>
    public DateOnly? DueDate { get; init; }

    /// <summary>وقت التذكير (UTC، اختياري)</summary>
    public DateTime? ReminderAt { get; init; }

    /// <summary>معرّف المشروع الذي تنتمي إليه المهمة (اختياري)</summary>
    public Guid? ProjectId { get; init; }

    /// <summary>معرّف التصنيف (اختياري)</summary>
    public Guid? CategoryId { get; init; }

    /// <summary>معرّف المهمة الأم للمهام الفرعية (اختياري)</summary>
    public Guid? ParentTaskId { get; init; }
}
