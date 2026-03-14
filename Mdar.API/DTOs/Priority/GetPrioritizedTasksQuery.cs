using Mdar.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Mdar.API.DTOs.Priority;

/// <summary>
/// معاملات طلب قائمة المهام المرتبة بالأولوية.
/// تُقرأ من Query String: GET /api/priority?contextTag=Office&amp;maxResults=10
/// </summary>
public sealed record GetPrioritizedTasksQuery
{
    /// <summary>
    /// السياق المكاني الحالي للمستخدم.
    /// Anywhere = اعرض جميع المهام بغض النظر عن السياق المكاني.
    /// </summary>
    public ContextTag ContextTag { get; init; } = ContextTag.Anywhere;

    /// <summary>
    /// الحد الأقصى لعدد المهام في النتيجة.
    /// null = جميع المهام المؤهلة.
    /// القيمة الشائعة: 10 (لعرض "أهم 10 مهام الآن").
    /// </summary>
    [Range(1, 100, ErrorMessage = "MaxResults يجب أن يكون بين 1 و 100.")]
    public int? MaxResults { get; init; }

    /// <summary>
    /// هل تُضمَّن تفاصيل حساب الوزن لكل مهمة؟
    /// true = يُضمَّن (مفيد لواجهة "لماذا هذه المهمة أولاً؟")
    /// false = يُحذف (أسرع وأخف — الافتراضي)
    /// </summary>
    public bool IncludeWeightBreakdown { get; init; } = false;
}
