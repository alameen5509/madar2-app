using Mdar.Core.Entities.Tasks;
using Mdar.Core.Enums;

namespace Mdar.API.DTOs.Tasks;

/// <summary>
/// تمثيل المهمة في استجابات API (قراءة).
/// يُستخدم في: GET /api/tasks، GET /api/tasks/{id}، PUT /api/tasks/{id}.
/// </summary>
public sealed record TaskResponse
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public TaskStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public TaskPriority Priority { get; init; }
    public string PriorityName { get; init; } = string.Empty;
    public ContextTag ContextTag { get; init; }
    public bool IsEmergency { get; init; }
    public bool IsPomodoroCompatible { get; init; }
    public int? EstimatedPomodoros { get; init; }
    public int CompletedPomodoros { get; init; }
    public PrayerPeriod? PreferredPrayerPeriod { get; init; }
    public DateOnly? DueDate { get; init; }
    public DateTime? ReminderAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public Guid? ProjectId { get; init; }
    public Guid? CategoryId { get; init; }
    public Guid? ParentTaskId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// يُنشئ TaskResponse من كيان TaskItem.
    /// Static Factory لتمركز منطق الـ Mapping في مكان واحد.
    /// </summary>
    public static TaskResponse From(TaskItem task) => new()
    {
        Id                   = task.Id,
        Title                = task.Title,
        Description          = task.Description,
        Status               = task.Status,
        StatusName           = task.Status.ToString(),
        Priority             = task.Priority,
        PriorityName         = task.Priority.ToString(),
        ContextTag           = task.ContextTag,
        IsEmergency          = task.IsEmergency,
        IsPomodoroCompatible = task.IsPomodoroCompatible,
        EstimatedPomodoros   = task.EstimatedPomodoros,
        CompletedPomodoros   = task.CompletedPomodoros,
        PreferredPrayerPeriod = task.PreferredPrayerPeriod,
        DueDate              = task.DueDate,
        ReminderAt           = task.ReminderAt,
        CompletedAt          = task.CompletedAt,
        ProjectId            = task.ProjectId,
        CategoryId           = task.CategoryId,
        ParentTaskId         = task.ParentTaskId,
        CreatedAt            = task.CreatedAt,
        UpdatedAt            = task.UpdatedAt
    };
}
