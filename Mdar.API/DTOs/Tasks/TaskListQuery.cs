using Mdar.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Mdar.API.DTOs.Tasks;

/// <summary>
/// معاملات تصفية وترحيل قائمة المهام.
/// تُقرأ من Query String: GET /api/tasks?status=Pending&amp;page=1&amp;pageSize=20
/// </summary>
public sealed record TaskListQuery
{
    /// <summary>تصفية بحسب الحالة (اختياري)</summary>
    public TaskStatus? Status { get; init; }

    /// <summary>تصفية بحسب مستوى الأولوية (اختياري)</summary>
    public TaskPriority? Priority { get; init; }

    /// <summary>تصفية بحسب السياق المكاني (اختياري)</summary>
    public ContextTag? ContextTag { get; init; }

    /// <summary>تصفية بحسب توافق الطماطم (اختياري)</summary>
    public bool? IsPomodoroCompatible { get; init; }

    /// <summary>تصفية بحسب وضع الطوارئ (اختياري)</summary>
    public bool? IsEmergency { get; init; }

    /// <summary>تصفية بحسب المشروع (اختياري)</summary>
    public Guid? ProjectId { get; init; }

    /// <summary>تصفية بحسب التصنيف (اختياري)</summary>
    public Guid? CategoryId { get; init; }

    /// <summary>
    /// تصفية: يُعيد المهام التي موعدها في هذا اليوم أو قبله (اختياري).
    /// مفيد لقائمة "مهام اليوم": ?dueBefore=2026-03-14
    /// </summary>
    public DateOnly? DueBefore { get; init; }

    /// <summary>رقم الصفحة (يبدأ من 1)</summary>
    [Range(1, int.MaxValue, ErrorMessage = "رقم الصفحة يبدأ من 1.")]
    public int Page { get; init; } = 1;

    /// <summary>عدد العناصر في الصفحة (افتراضي: 20، أقصى: 100)</summary>
    [Range(1, 100, ErrorMessage = "حجم الصفحة يجب أن يكون بين 1 و 100.")]
    public int PageSize { get; init; } = 20;
}
