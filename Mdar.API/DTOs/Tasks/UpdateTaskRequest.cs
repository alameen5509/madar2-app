using Mdar.Core.Enums;
using System.ComponentModel.DataAnnotations;
using TaskStatus = Mdar.Core.Enums.TaskStatus;

namespace Mdar.API.DTOs.Tasks;

/// <summary>
/// طلب تحديث مهمة موجودة.
/// جميع الحقول اختيارية — يُحدَّث فقط ما يُرسَل.
/// (نمط Partial Update / PATCH-style PUT)
/// </summary>
public sealed record UpdateTaskRequest
{
    [MinLength(2, ErrorMessage = "العنوان لا يقل عن حرفين.")]
    [MaxLength(300, ErrorMessage = "العنوان لا يتجاوز 300 حرف.")]
    public string? Title { get; init; }

    [MaxLength(2000, ErrorMessage = "الوصف لا يتجاوز 2000 حرف.")]
    public string? Description { get; init; }

    public TaskStatus? Status { get; init; }
    public TaskPriority? Priority { get; init; }
    public ContextTag? ContextTag { get; init; }
    public bool? IsEmergency { get; init; }
    public bool? IsPomodoroCompatible { get; init; }
    public PrayerPeriod? PreferredPrayerPeriod { get; init; }

    [Range(1, 20, ErrorMessage = "عدد الطماطم المقدَّر يجب أن يكون بين 1 و 20.")]
    public int? EstimatedPomodoros { get; init; }

    public DateOnly? DueDate { get; init; }
    public DateTime? ReminderAt { get; init; }
    public Guid? ProjectId { get; init; }
    public Guid? CategoryId { get; init; }
}
