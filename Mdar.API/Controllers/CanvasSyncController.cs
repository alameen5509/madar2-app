using Mdar.API.DTOs.Sync;
using Mdar.API.Hubs;
using Mdar.Core.Entities.Canvas;
using Mdar.Core.Entities.Thinking;
using Mdar.Core.Enums;
using Mdar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Mdar.API.Controllers;

/// <summary>
/// المزامنة السحابية الذكية — 3 مستويات.
///
/// Endpoints:
///   POST /api/canvas/sync/push    ← Delta Sync: رفع التغييرات
///   GET  /api/canvas/sync/pull    ← Full/Delta Pull: جلب التغييرات
/// </summary>
[ApiController]
[Route("api/canvas/sync")]
[Authorize]
public class CanvasSyncController : ControllerBase
{
    private readonly AppDbContext             _db;
    private readonly IHubContext<CanvasHub>   _hub;

    public CanvasSyncController(AppDbContext db, IHubContext<CanvasHub> hub)
    {
        _db  = db;
        _hub = hub;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── POST /api/canvas/sync/push ─────────────────────────────────────────────
    /// <summary>
    /// يستقبل قائمة التغييرات من العميل، يطبّقها على ThinkingCards،
    /// يخزّنها في CanvasSyncEvents، ويُرسلها لباقي الجلسات عبر SignalR.
    /// </summary>
    [HttpPost("push")]
    public async Task<ActionResult<PushSyncResponse>> Push([FromBody] PushSyncRequest req)
    {
        var userId  = GetUserId();
        var now     = DateTime.UtcNow;
        int accepted = 0, skipped = 0;

        // التحقق من ملكية اللوحة
        var boardExists = await _db.ThinkingBoards
            .AnyAsync(b => b.Id == req.BoardId && b.UserId == userId);
        if (!boardExists)
            return NotFound(new { error = "اللوحة غير موجودة أو ليست ملكك." });

        foreach (var change in req.Changes.OrderBy(c => c.Timestamp))
        {
            try
            {
                await ApplyChange(change, req.BoardId, userId);

                // تخزين الحدث في سجل المزامنة
                _db.CanvasSyncEvents.Add(new CanvasSyncEvent
                {
                    BoardId   = req.BoardId,
                    UserId    = userId,
                    EventType = change.Type,
                    Payload   = JsonSerializer.Serialize(change),
                    Timestamp = change.Timestamp == default ? now : change.Timestamp,
                    SessionId = req.SessionId,
                });

                accepted++;
            }
            catch
            {
                skipped++;
            }
        }

        await _db.SaveChangesAsync();

        // إرسال التغييرات لباقي المشاركين عبر SignalR
        if (accepted > 0)
        {
            await _hub.Clients.GroupExcept(req.BoardId.ToString(), [])
                .SendAsync("RemoteChange", new
                {
                    changes   = req.Changes,
                    changedBy = userId.ToString(),
                    sessionId = req.SessionId,
                });
        }

        return Ok(new PushSyncResponse(accepted, skipped, now));
    }

    // ── GET /api/canvas/sync/pull ──────────────────────────────────────────────
    /// <summary>
    /// يُعيد جميع الأحداث على اللوحة منذ تاريخ محدد.
    /// عند الفتح الأول (since = null): يُعيد حالة اللوحة الكاملة من ThinkingCards.
    /// </summary>
    [HttpGet("pull")]
    public async Task<ActionResult<PullSyncResponse>> Pull(
        [FromQuery] Guid     boardId,
        [FromQuery] DateTime? since = null)
    {
        var userId = GetUserId();

        var boardExists = await _db.ThinkingBoards
            .AnyAsync(b => b.Id == boardId && b.UserId == userId);
        if (!boardExists)
            return NotFound(new { error = "اللوحة غير موجودة." });

        if (since == null)
        {
            // Full Sync: أعد حالة اللوحة الكاملة كأحداث node_created
            var cards = await _db.ThinkingCards
                .Where(c => c.BoardId == boardId)
                .ToListAsync();

            var fullSync = cards.Select(c => new SyncEventResponse
            {
                Id        = c.Id,
                EventType = "node_created",
                Payload   = JsonSerializer.Serialize(new
                {
                    nodeId   = c.Id,
                    title    = c.Title,
                    content  = c.Content,
                    cardType = (int)c.CardType,
                    x        = c.PositionX,
                    y        = c.PositionY,
                    w        = c.Width,
                    h        = c.Height,
                }),
                Timestamp = c.CreatedAt,
                UserId    = userId,
            }).ToList();

            return Ok(new PullSyncResponse { Changes = fullSync, ServerTime = DateTime.UtcNow });
        }

        // Delta Sync: أعد الأحداث منذ since
        var events = await _db.CanvasSyncEvents
            .Where(e => e.BoardId == boardId && e.Timestamp > since.Value)
            .OrderBy(e => e.Timestamp)
            .Select(e => new SyncEventResponse
            {
                Id        = e.Id,
                EventType = e.EventType,
                Payload   = e.Payload,
                Timestamp = e.Timestamp,
                UserId    = e.UserId,
                SessionId = e.SessionId,
            })
            .ToListAsync();

        return Ok(new PullSyncResponse { Changes = events, ServerTime = DateTime.UtcNow });
    }

    // ── Private: Apply Change to ThinkingCards ─────────────────────────────────

    private async Task ApplyChange(SyncChangeDto change, Guid boardId, Guid userId)
    {
        switch (change.Type)
        {
            case "node_created" when change.Node != null:
            {
                var card = new ThinkingCard
                {
                    BoardId   = boardId,
                    UserId    = userId,
                    Title     = change.Node.Title?.Trim() ?? string.Empty,
                    Content   = change.Node.Content ?? string.Empty,
                    CardType  = (CardType)Math.Clamp(change.Node.CardType, 0, 5),
                    PositionX = change.Node.X,
                    PositionY = change.Node.Y,
                    Width     = change.Node.W > 0 ? change.Node.W : 240,
                    Height    = change.Node.H > 0 ? change.Node.H : 160,
                };
                _db.ThinkingCards.Add(card);
                break;
            }

            case "node_deleted" when change.NodeId != null:
            {
                if (!Guid.TryParse(change.NodeId, out var cardId)) break;
                var card = await _db.ThinkingCards
                    .FirstOrDefaultAsync(c => c.Id == cardId && c.BoardId == boardId);
                if (card != null) card.IsDeleted = true;
                break;
            }

            case "node_moved" when change.NodeId != null:
            {
                if (!Guid.TryParse(change.NodeId, out var cardId)) break;
                var card = await _db.ThinkingCards
                    .FirstOrDefaultAsync(c => c.Id == cardId && c.BoardId == boardId);
                if (card == null) break;
                if (change.X.HasValue) card.PositionX = change.X.Value;
                if (change.Y.HasValue) card.PositionY = change.Y.Value;
                if (change.W.HasValue && change.W > 0) card.Width  = change.W.Value;
                if (change.H.HasValue && change.H > 0) card.Height = change.H.Value;
                break;
            }

            case "text_changed" when change.NodeId != null:
            {
                if (!Guid.TryParse(change.NodeId, out var cardId)) break;
                var card = await _db.ThinkingCards
                    .FirstOrDefaultAsync(c => c.Id == cardId && c.BoardId == boardId);
                if (card == null) break;
                if (change.Title   != null) card.Title   = change.Title.Trim();
                if (change.Content != null) card.Content = change.Content;
                break;
            }
        }
    }
}
