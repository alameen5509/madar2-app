using Mdar.API.DTOs.Common;
using Mdar.API.DTOs.Tasks;
using Mdar.API.Extensions;
using Mdar.Core.Entities.Tasks;
using Mdar.Core.Enums;
using Mdar.Core.Interfaces;
using Mdar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mdar.API.Controllers;

/// <summary>
/// Controller إدارة المهام — CRUD + حساب الأولوية عند الإنشاء.
///
/// التبعيات المحقونة:
///   - AppDbContext          : لجميع عمليات قراءة/كتابة قاعدة البيانات
///   - IPriorityEngineService: لحساب الوزن الأولوي لكل مهمة جديدة
///
/// جميع النقاط محمية بـ [Authorize] — يُشترط JWT Bearer Token.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public sealed class TasksController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPriorityEngineService _priorityEngine;

    public TasksController(AppDbContext db, IPriorityEngineService priorityEngine)
    {
        _db            = db;
        _priorityEngine = priorityEngine;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // POST /api/tasks — إنشاء مهمة جديدة مع حساب وزنها الأولوي
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// إنشاء مهمة جديدة.
    ///
    /// بعد الحفظ في قاعدة البيانات، يُحسب الوزن الأولوي الأولي للمهمة
    /// بناءً على فترة الصلاة الحالية، ويُعاد مع الاستجابة 201 Created.
    ///
    /// هذا يتيح للعميل معرفة أين تقع المهمة الجديدة فوراً
    /// في قائمة الأولويات دون الحاجة لطلب إضافي.
    /// </summary>
    /// <param name="request">بيانات المهمة الجديدة</param>
    /// <param name="ct">رمز الإلغاء</param>
    /// <returns>HTTP 201 مع بيانات المهمة + الوزن الأولوي</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TaskCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTask(
        [FromBody] CreateTaskRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();

        // ── التحقق من وجود المراجع الخارجية (FK Validation) ─────────────────

        if (request.ProjectId.HasValue)
        {
            var exists = await _db.Projects
                .AnyAsync(p => p.Id == request.ProjectId.Value
                            && p.UserId == userId, ct);

            if (!exists)
                return NotFound(CreateNotFoundProblem(
                    "المشروع", request.ProjectId.Value));
        }

        if (request.CategoryId.HasValue)
        {
            var exists = await _db.Categories
                .AnyAsync(c => c.Id == request.CategoryId.Value
                            && c.UserId == userId, ct);

            if (!exists)
                return NotFound(CreateNotFoundProblem(
                    "التصنيف", request.CategoryId.Value));
        }

        if (request.ParentTaskId.HasValue)
        {
            var exists = await _db.Tasks
                .AnyAsync(t => t.Id == request.ParentTaskId.Value
                            && t.UserId == userId, ct);

            if (!exists)
                return NotFound(CreateNotFoundProblem(
                    "المهمة الأم", request.ParentTaskId.Value));
        }

        // ── إنشاء كيان TaskItem من الطلب ──────────────────────────────────

        var task = new TaskItem
        {
            Title                = request.Title,
            Description          = request.Description,
            Priority             = request.Priority,
            ContextTag           = request.ContextTag,
            IsEmergency          = request.IsEmergency,
            IsPomodoroCompatible = request.IsPomodoroCompatible,
            EstimatedPomodoros   = request.EstimatedPomodoros,
            PreferredPrayerPeriod = request.PreferredPrayerPeriod,
            DueDate              = request.DueDate,
            ReminderAt           = request.ReminderAt,
            ProjectId            = request.ProjectId,
            CategoryId           = request.CategoryId,
            ParentTaskId         = request.ParentTaskId,
            UserId               = userId,
            Status               = TaskStatus.Pending   // الحالة الأولية دائماً Pending
        };

        // ── الحفظ في قاعدة البيانات ───────────────────────────────────────

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);

        // ── حساب الوزن الأولوي بناءً على فترة الصلاة الحالية ────────────
        //
        // هذه هي نقطة الاتصال مع IPriorityEngineService:
        //   - يجلب جدول أوقات الصلاة للمستخدم من DB
        //   - يحدد الفترة الزمنية الحالية (AfterFajr, Duha, ...)
        //   - يُطبق خوارزمية الحساب على المهمة في هذا السياق
        //   - يُعيد TaskWeightBreakdown مع التفصيل الكامل

        var weightBreakdown = await _priorityEngine
            .CalculateTaskWeightAsync(task, userId, ct: ct);

        // ── بناء الاستجابة 201 Created ────────────────────────────────────

        var response = TaskCreatedResponse.From(task, weightBreakdown);

        // CreatedAtAction يُضيف تلقائياً:
        //   - Header: Location: /api/tasks/{id}
        //   - Status: 201 Created
        return CreatedAtAction(
            actionName: nameof(GetById),
            routeValues: new { id = task.Id },
            value: response);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GET /api/tasks — قائمة المهام مع تصفية وترحيل
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// يُعيد قائمة مهام المستخدم مع دعم التصفية والترحيل.
    /// </summary>
    /// <param name="query">معاملات التصفية والترحيل من Query String</param>
    /// <param name="ct">رمز الإلغاء</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTasks(
        [FromQuery] TaskListQuery query,
        CancellationToken ct)
    {
        var userId = User.GetUserId();

        // بناء الاستعلام تدريجياً بحسب الفلاتر المُرسَلة
        var q = _db.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId);

        if (query.Status.HasValue)
            q = q.Where(t => t.Status == query.Status.Value);

        if (query.Priority.HasValue)
            q = q.Where(t => t.Priority == query.Priority.Value);

        if (query.ContextTag.HasValue)
            q = q.Where(t => t.ContextTag == query.ContextTag.Value);

        if (query.IsPomodoroCompatible.HasValue)
            q = q.Where(t => t.IsPomodoroCompatible == query.IsPomodoroCompatible.Value);

        if (query.IsEmergency.HasValue)
            q = q.Where(t => t.IsEmergency == query.IsEmergency.Value);

        if (query.ProjectId.HasValue)
            q = q.Where(t => t.ProjectId == query.ProjectId.Value);

        if (query.CategoryId.HasValue)
            q = q.Where(t => t.CategoryId == query.CategoryId.Value);

        if (query.DueBefore.HasValue)
            q = q.Where(t => t.DueDate.HasValue && t.DueDate.Value <= query.DueBefore.Value);

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(t => t.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => TaskResponse.From(t))
            .ToListAsync(ct);

        return Ok(PagedResponse<TaskResponse>.Create(items, query.Page, query.PageSize, totalCount));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GET /api/tasks/{id} — مهمة بمعرّفها
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>يُعيد مهمة واحدة بمعرّفها.</summary>
    /// <param name="id">معرّف المهمة (Guid)</param>
    /// <param name="ct">رمز الإلغاء</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var task = await _db.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

        if (task is null)
            return NotFound(CreateNotFoundProblem("المهمة", id));

        return Ok(TaskResponse.From(task));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PUT /api/tasks/{id} — تحديث مهمة
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// يُحدِّث بيانات مهمة موجودة.
    /// فقط الحقول المُرسَلة (غير null) تُحدَّث.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTask(
        Guid id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();

        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

        if (task is null)
            return NotFound(CreateNotFoundProblem("المهمة", id));

        // تحديث الحقول المُرسَلة فقط (Partial Update)
        if (request.Title is not null)
            task.Title = request.Title;

        if (request.Description is not null)
            task.Description = request.Description;

        if (request.Status.HasValue)
        {
            task.Status = request.Status.Value;
            // إذا اكتملت المهمة → نسجل وقت الإنجاز
            if (request.Status.Value == TaskStatus.Completed)
                task.CompletedAt = DateTime.UtcNow;
        }

        if (request.Priority.HasValue)       task.Priority             = request.Priority.Value;
        if (request.ContextTag.HasValue)     task.ContextTag           = request.ContextTag.Value;
        if (request.IsEmergency.HasValue)    task.IsEmergency          = request.IsEmergency.Value;
        if (request.IsPomodoroCompatible.HasValue) task.IsPomodoroCompatible = request.IsPomodoroCompatible.Value;
        if (request.PreferredPrayerPeriod.HasValue) task.PreferredPrayerPeriod = request.PreferredPrayerPeriod.Value;
        if (request.EstimatedPomodoros.HasValue) task.EstimatedPomodoros = request.EstimatedPomodoros.Value;
        if (request.DueDate.HasValue)        task.DueDate              = request.DueDate.Value;
        if (request.ReminderAt.HasValue)     task.ReminderAt           = request.ReminderAt.Value;

        // التحقق من ProjectId الجديد إذا أُرسل
        if (request.ProjectId.HasValue)
        {
            var projectExists = await _db.Projects
                .AnyAsync(p => p.Id == request.ProjectId.Value && p.UserId == userId, ct);
            if (!projectExists)
                return NotFound(CreateNotFoundProblem("المشروع", request.ProjectId.Value));

            task.ProjectId = request.ProjectId.Value;
        }

        // التحقق من CategoryId الجديد إذا أُرسل
        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _db.Categories
                .AnyAsync(c => c.Id == request.CategoryId.Value && c.UserId == userId, ct);
            if (!categoryExists)
                return NotFound(CreateNotFoundProblem("التصنيف", request.CategoryId.Value));

            task.CategoryId = request.CategoryId.Value;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(TaskResponse.From(task));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PATCH /api/tasks/{id}/complete — إتمام المهمة
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// يُغيِّر حالة المهمة إلى Completed ويسجل وقت الإنجاز.
    /// مسار مختصر أسرع من إرسال PUT كامل.
    /// </summary>
    [HttpPatch("{id:guid}/complete")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteTask(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

        if (task is null)
            return NotFound(CreateNotFoundProblem("المهمة", id));

        if (task.Status == TaskStatus.Completed)
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title  = "تعارض في الحالة",
                Detail = "المهمة مكتملة بالفعل."
            });

        task.Status      = TaskStatus.Completed;
        task.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(TaskResponse.From(task));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PATCH /api/tasks/{id}/emergency — تبديل وضع الطوارئ
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// يُبدِّل حالة وضع الطوارئ للمهمة (Toggle).
    /// المهام في وضع الطوارئ تُستبعد من محرك الأولويات العادي.
    /// </summary>
    [HttpPatch("{id:guid}/emergency")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleEmergency(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

        if (task is null)
            return NotFound(CreateNotFoundProblem("المهمة", id));

        task.IsEmergency = !task.IsEmergency;
        await _db.SaveChangesAsync(ct);

        return Ok(TaskResponse.From(task));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DELETE /api/tasks/{id} — حذف ناعم للمهمة
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// يحذف المهمة حذفاً ناعماً (Soft Delete).
    /// لا تُحذف من قاعدة البيانات فعلياً — يُضبط IsDeleted = true.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

        if (task is null)
            return NotFound(CreateNotFoundProblem("المهمة", id));

        // Soft Delete — لا نحذف فعلياً بل نُعلِّم السجل كمحذوف
        task.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────

    /// <summary>يُنشئ ProblemDetails موحَّداً لأخطاء 404</summary>
    private static ProblemDetails CreateNotFoundProblem(string entityName, Guid id) => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title  = "العنصر غير موجود",
        Detail = $"{entityName} برقم '{id}' غير موجود أو لا تملك صلاحية الوصول إليه."
    };
}
