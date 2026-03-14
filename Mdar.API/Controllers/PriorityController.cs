using Mdar.API.DTOs.Priority;
using Mdar.API.Extensions;
using Mdar.Core.Interfaces;
using Mdar.Core.Models.Priority;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mdar.API.Controllers;

/// <summary>
/// Controller محرك الأولويات.
/// يُعيد قائمة المهام مرتبةً بحسب السياق الزمني (فترة الصلاة) والمكاني.
///
/// التبعيات المحقونة:
///   - IPriorityEngineService: يُدير كامل منطق الترتيب والتصفية
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public sealed class PriorityController : ControllerBase
{
    private readonly IPriorityEngineService _priorityEngine;

    public PriorityController(IPriorityEngineService priorityEngine)
    {
        _priorityEngine = priorityEngine;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GET /api/priority — قائمة المهام المرتبة للسياق الحالي
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// يُعيد قائمة المهام مرتبةً تنازلياً بحسب الأولوية للسياق الحالي.
    ///
    /// تُطبَّق القواعد التالية تلقائياً:
    ///   ✗ تُستبعد المهام في وضع الطوارئ (IsEmergency = true)
    ///   ✗ تُستبعد المهام غير الملائمة للسياق المكاني المُرسَل
    ///   ✗ تُستبعد المهام المكتملة أو الملغاة
    ///   ✓ الباقي يُرتَّب بخوارزمية الوزن المبنية على فترة الصلاة
    ///
    /// مثال: GET /api/priority?contextTag=Office&amp;maxResults=10&amp;includeWeightBreakdown=true
    /// </summary>
    /// <param name="query">معاملات السياق والحد الأقصى من Query String</param>
    /// <param name="ct">رمز الإلغاء</param>
    [HttpGet]
    [ProducesResponseType(typeof(PriorityEngineResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPrioritizedTasks(
        [FromQuery] GetPrioritizedTasksQuery query,
        CancellationToken ct)
    {
        var userId = User.GetUserId();

        var request = new PriorityEngineRequest
        {
            UserId                = userId,
            CurrentContextTag     = query.ContextTag,
            MaxResults            = query.MaxResults,
            IncludeWeightBreakdown = query.IncludeWeightBreakdown
        };

        var result = await _priorityEngine.GetPrioritizedTasksAsync(request, ct);

        return Ok(result);
    }
}
