using Mdar.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Mdar.API.DTOs.DailyOps;

/// <summary>
/// طلب الإضافة السريعة للمهمة من الـ FAB Modal.
/// مُبسَّط عن CreateTaskRequest — يحتوي الحقول الأكثر استخداماً فقط.
/// </summary>
public sealed record QuickAddTaskRequest
{
    [Required(ErrorMessage = "عنوان المهمة مطلوب.")]
    [MinLength(2, ErrorMessage = "العنوان لا يقل عن حرفين.")]
    [MaxLength(300, ErrorMessage = "العنوان لا يتجاوز 300 حرف.")]
    public required string Title { get; init; }

    public TaskPriority Priority { get; init; } = TaskPriority.Medium;

    public bool IsPomodoroCompatible { get; init; } = true;

    public ContextTag ContextTag { get; init; } = ContextTag.Anywhere;

    /// <summary>فترة الصلاة المفضلة. null = أي وقت</summary>
    public PrayerPeriod? PreferredPrayerPeriod { get; init; }

    public DateOnly? DueDate { get; init; }
}
