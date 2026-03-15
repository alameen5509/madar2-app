using Mdar.Core.Entities.Thinking;
using Mdar.Core.Enums;
using Mdar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Mdar.API.Controllers;

/// <summary>
/// إدارة لوحات الـ Canvas كـ Sessions + Nodes.
/// Sessions = ThinkingBoards | Nodes = ThinkingCards
/// </summary>
[ApiController]
[Authorize]
public class CanvasSessionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CanvasSessionsController(AppDbContext db) => _db = db;

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── GET /api/sessions ─────────────────────────────────────────────────────
    [HttpGet("/api/sessions")]
    public async Task<IActionResult> GetSessions()
    {
        var userId = GetUserId();

        var sessions = await _db.ThinkingBoards
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.UpdatedAt)
            .Select(b => new
            {
                b.Id,
                b.Title,
                b.Description,
                nodeCount  = b.Cards.Count(c => !c.IsDeleted),
                b.CreatedAt,
                b.UpdatedAt
            })
            .ToListAsync();

        return Ok(sessions);
    }

    // ── POST /api/sessions ────────────────────────────────────────────────────
    [HttpPost("/api/sessions")]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest req)
    {
        var board = new ThinkingBoard
        {
            UserId      = GetUserId(),
            Title       = req.Title,
            Description = req.Description
        };

        _db.ThinkingBoards.Add(board);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSession), new { id = board.Id }, new
        {
            board.Id,
            board.Title,
            board.Description,
            nodeCount  = 0,
            board.CreatedAt,
            board.UpdatedAt
        });
    }

    // ── GET /api/sessions/{id} ────────────────────────────────────────────────
    [HttpGet("/api/sessions/{id:guid}")]
    public async Task<IActionResult> GetSession(Guid id)
    {
        var userId = GetUserId();

        var board = await _db.ThinkingBoards
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (board is null) return NotFound();

        return Ok(new
        {
            board.Id,
            board.Title,
            board.Description,
            board.CreatedAt,
            board.UpdatedAt
        });
    }

    // ── GET /api/sessions/{id}/nodes ──────────────────────────────────────────
    [HttpGet("/api/sessions/{id:guid}/nodes")]
    public async Task<IActionResult> GetSessionNodes(Guid id)
    {
        var userId = GetUserId();

        var boardExists = await _db.ThinkingBoards
            .AnyAsync(b => b.Id == id && b.UserId == userId);

        if (!boardExists) return NotFound();

        var nodes = await _db.ThinkingCards
            .AsNoTracking()
            .Where(c => c.BoardId == id)
            .OrderBy(c => c.ZIndex)
            .ToListAsync();

        return Ok(nodes.Select(MapNode));
    }

    // ── POST /api/nodes ───────────────────────────────────────────────────────
    [HttpPost("/api/nodes")]
    public async Task<IActionResult> CreateNode([FromBody] CreateNodeRequest req)
    {
        var userId = GetUserId();

        var boardExists = await _db.ThinkingBoards
            .AnyAsync(b => b.Id == req.SessionId && b.UserId == userId);

        if (!boardExists)
            return NotFound(new { message = "الجلسة غير موجودة" });

        var maxZ = await _db.ThinkingCards
            .Where(c => c.BoardId == req.SessionId)
            .MaxAsync(c => (int?)c.ZIndex) ?? 0;

        var card = new ThinkingCard
        {
            BoardId   = req.SessionId,
            UserId    = userId,
            Title     = req.Title,
            Content   = req.Content,
            CardType  = req.CardType,
            Color     = GetDefaultColor(req.CardType),
            PositionX = req.PositionX,
            PositionY = req.PositionY,
            Width     = req.Width,
            Height    = req.Height,
            ZIndex    = maxZ + 1
        };

        _db.ThinkingCards.Add(card);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSessionNodes),
            new { id = req.SessionId }, MapNode(card));
    }

    // ── PUT /api/nodes/{id} ───────────────────────────────────────────────────
    [HttpPut("/api/nodes/{id:guid}")]
    public async Task<IActionResult> UpdateNode(Guid id, [FromBody] UpdateNodeRequest req)
    {
        var userId = GetUserId();

        var card = await _db.ThinkingCards
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (card is null) return NotFound();

        if (req.Title    is not null) card.Title   = req.Title;
        if (req.Content  is not null) card.Content = req.Content;
        if (req.PositionX.HasValue)   card.PositionX = req.PositionX.Value;
        if (req.PositionY.HasValue)   card.PositionY = req.PositionY.Value;
        if (req.Width.HasValue)       card.Width     = req.Width.Value;
        if (req.Height.HasValue)      card.Height    = req.Height.Value;
        if (req.ZIndex.HasValue)      card.ZIndex    = req.ZIndex.Value;
        if (req.CardType.HasValue)
        {
            card.CardType = req.CardType.Value;
            card.Color    = GetDefaultColor(req.CardType.Value);
        }

        await _db.SaveChangesAsync();
        return Ok(MapNode(card));
    }

    // ── DELETE /api/nodes/{id} ────────────────────────────────────────────────
    [HttpDelete("/api/nodes/{id:guid}")]
    public async Task<IActionResult> DeleteNode(Guid id)
    {
        var userId = GetUserId();

        var card = await _db.ThinkingCards
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (card is null) return NotFound();

        card.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static object MapNode(ThinkingCard c) => new
    {
        c.Id,
        sessionId  = c.BoardId,
        c.Title,
        c.Content,
        c.CardType,
        c.Color,
        c.PositionX,
        c.PositionY,
        c.Width,
        c.Height,
        c.ZIndex,
        c.CreatedAt,
        c.UpdatedAt
    };

    private static string GetDefaultColor(CardType type) => type switch
    {
        CardType.Note     => "#1e293b",
        CardType.Idea     => "#7c2d12",
        CardType.Task     => "#0c4a6e",
        CardType.Question => "#2e1065",
        _                 => "#1e293b"
    };
}

public record CreateSessionRequest(string Title, string? Description);
public record CreateNodeRequest(
    Guid SessionId, string Title, string? Content,
    CardType CardType,
    double PositionX, double PositionY,
    double Width, double Height);
public record UpdateNodeRequest(
    string? Title, string? Content,
    CardType? CardType,
    double? PositionX, double? PositionY,
    double? Width, double? Height,
    int? ZIndex);
