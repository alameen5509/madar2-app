using Mdar.API.DTOs.Thinking;
using Mdar.Core.Entities.Thinking;
using Mdar.Core.Enums;
using Mdar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Mdar.API.Controllers;

[ApiController]
[Route("api/thinking-boards")]
[Authorize]
public class ThinkingBoardController : ControllerBase
{
    private readonly AppDbContext _db;

    public ThinkingBoardController(AppDbContext db) => _db = db;

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── GET /api/thinking-boards ──────────────────────────────────────────────
    /// <summary>قائمة لوحات المستخدم (بدون البطاقات لتسريع الاستجابة)</summary>
    [HttpGet]
    public async Task<ActionResult<List<BoardResponse>>> GetBoards()
    {
        var userId = GetUserId();

        var boards = await _db.ThinkingBoards
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.UpdatedAt)
            .Select(b => new BoardResponse
            {
                Id = b.Id,
                Title = b.Title,
                Description = b.Description,
                CardCount = b.Cards.Count(c => !c.IsDeleted),
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                Cards = new List<CardResponse>()
            })
            .ToListAsync();

        return Ok(boards);
    }

    // ── POST /api/thinking-boards ─────────────────────────────────────────────
    [HttpPost]
    public async Task<ActionResult<BoardResponse>> CreateBoard([FromBody] CreateBoardRequest req)
    {
        var board = new ThinkingBoard
        {
            UserId = GetUserId(),
            Title = req.Title,
            Description = req.Description
        };

        _db.ThinkingBoards.Add(board);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBoard), new { id = board.Id }, MapBoard(board, new List<CardResponse>()));
    }

    // ── GET /api/thinking-boards/{id} ─────────────────────────────────────────
    /// <summary>تفاصيل لوحة محددة مع جميع بطاقاتها</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BoardResponse>> GetBoard(Guid id)
    {
        var userId = GetUserId();

        var board = await _db.ThinkingBoards
            .Include(b => b.Cards)   // Global filter تستبعد المحذوفة تلقائياً
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (board is null) return NotFound();

        var cards = board.Cards.Select(MapCard).ToList();
        return Ok(MapBoard(board, cards));
    }

    // ── DELETE /api/thinking-boards/{id} ──────────────────────────────────────
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteBoard(Guid id)
    {
        var userId = GetUserId();
        var board = await _db.ThinkingBoards.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (board is null) return NotFound();

        board.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── POST /api/thinking-boards/{boardId}/cards ──────────────────────────────
    [HttpPost("{boardId:guid}/cards")]
    public async Task<ActionResult<CardResponse>> CreateCard(Guid boardId, [FromBody] CreateCardRequest req)
    {
        var userId = GetUserId();
        var boardExists = await _db.ThinkingBoards.AnyAsync(b => b.Id == boardId && b.UserId == userId);

        if (!boardExists) return NotFound();

        var card = new ThinkingCard
        {
            BoardId = boardId,
            UserId = userId,
            Title = req.Title,
            Content = req.Content,
            CardType = req.CardType,
            Color = GetDefaultColor(req.CardType),
            PositionX = req.PositionX,
            PositionY = req.PositionY,
            Width = req.Width,
            Height = req.Height,
            ZIndex = await _db.ThinkingCards
                .Where(c => c.BoardId == boardId)
                .MaxAsync(c => (int?)c.ZIndex) + 1 ?? 1
        };

        _db.ThinkingCards.Add(card);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBoard), new { id = boardId }, MapCard(card));
    }

    // ── PATCH /api/thinking-boards/{boardId}/cards/{cardId} ───────────────────
    /// <summary>تحديث جزئي: المحتوى، الموضع، الحجم، أو النوع</summary>
    [HttpPatch("{boardId:guid}/cards/{cardId:guid}")]
    public async Task<ActionResult<CardResponse>> UpdateCard(Guid boardId, Guid cardId, [FromBody] UpdateCardRequest req)
    {
        var userId = GetUserId();
        var card = await _db.ThinkingCards
            .FirstOrDefaultAsync(c => c.Id == cardId && c.BoardId == boardId && c.UserId == userId);

        if (card is null) return NotFound();

        if (req.Title is not null) card.Title = req.Title;
        if (req.Content is not null) card.Content = req.Content;
        if (req.PositionX.HasValue) card.PositionX = req.PositionX.Value;
        if (req.PositionY.HasValue) card.PositionY = req.PositionY.Value;
        if (req.Width.HasValue) card.Width = req.Width.Value;
        if (req.Height.HasValue) card.Height = req.Height.Value;
        if (req.ZIndex.HasValue) card.ZIndex = req.ZIndex.Value;
        if (req.CardType.HasValue)
        {
            card.CardType = req.CardType.Value;
            card.Color = GetDefaultColor(req.CardType.Value);
        }

        await _db.SaveChangesAsync();
        return Ok(MapCard(card));
    }

    // ── DELETE /api/thinking-boards/{boardId}/cards/{cardId} ──────────────────
    [HttpDelete("{boardId:guid}/cards/{cardId:guid}")]
    public async Task<IActionResult> DeleteCard(Guid boardId, Guid cardId)
    {
        var userId = GetUserId();
        var card = await _db.ThinkingCards
            .FirstOrDefaultAsync(c => c.Id == cardId && c.BoardId == boardId && c.UserId == userId);

        if (card is null) return NotFound();

        card.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Mapping Helpers ───────────────────────────────────────────────────────

    private static BoardResponse MapBoard(ThinkingBoard b, List<CardResponse> cards) => new()
    {
        Id = b.Id,
        Title = b.Title,
        Description = b.Description,
        CardCount = cards.Count,
        CreatedAt = b.CreatedAt,
        UpdatedAt = b.UpdatedAt,
        Cards = cards
    };

    private static CardResponse MapCard(ThinkingCard c) => new()
    {
        Id = c.Id,
        BoardId = c.BoardId,
        Title = c.Title,
        Content = c.Content,
        CardType = c.CardType,
        Color = c.Color,
        PositionX = c.PositionX,
        PositionY = c.PositionY,
        Width = c.Width,
        Height = c.Height,
        ZIndex = c.ZIndex,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
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
