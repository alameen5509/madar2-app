using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Mdar.API.Hubs;

/// <summary>
/// SignalR Hub للتزامن اللحظي بين جلسات متعددة على نفس اللوحة.
///
/// كل لوحة = Group واحدة بمعرّف boardId.
///
/// الأحداث:
///   Client → Server:
///     JoinSession   { boardId }             ← الانضمام لغرفة اللوحة
///     LeaveSession  { boardId }             ← مغادرة الغرفة
///     PushChange    { boardId, change }     ← إرسال تغيير لباقي المشاركين
///
///   Server → Client:
///     NodeMoved     { nodeId, x, y, w, h, movedBy }
///     NodeCreated   { node, createdBy }
///     NodeDeleted   { nodeId, deletedBy }
///     TextChanged   { nodeId, title, content, changedBy }
///     UserJoined    { userId, boardId }
///     UserLeft      { userId, boardId }
/// </summary>
[Authorize]
public class CanvasHub : Hub
{
    private string GetUserId() =>
        Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";

    // ── Client → Server ─────────────────────────────────────────────────────

    /// <summary>الانضمام لغرفة لوحة معينة</summary>
    public async Task JoinSession(string boardId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, boardId);
        await Clients.OthersInGroup(boardId).SendAsync("UserJoined", new
        {
            userId  = GetUserId(),
            boardId,
        });
    }

    /// <summary>مغادرة غرفة اللوحة</summary>
    public async Task LeaveSession(string boardId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, boardId);
        await Clients.OthersInGroup(boardId).SendAsync("UserLeft", new
        {
            userId  = GetUserId(),
            boardId,
        });
    }

    /// <summary>
    /// إرسال تغيير واحد لجميع المشاركين الآخرين في نفس اللوحة.
    /// العميل المُرسِل لا يستقبل الحدث (OthersInGroup).
    /// </summary>
    public async Task PushChange(string boardId, object change)
    {
        var userId = GetUserId();

        // توجيه الحدث حسب النوع مع إضافة معرّف المُرسِل
        await Clients.OthersInGroup(boardId).SendAsync("RemoteChange", new
        {
            change,
            changedBy = userId,
        });
    }

    // ── Connection Lifecycle ─────────────────────────────────────────────────

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // لا حاجة لتنظيف Groups يدوياً — SignalR يتولى ذلك تلقائياً
        await base.OnDisconnectedAsync(exception);
    }
}
