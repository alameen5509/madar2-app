using Mdar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mdar.API.Controllers;

/// <summary>
/// إدارة المستخدمين — Admin فقط.
/// يُشترط أن يحمل الـ JWT claim بـ role = "admin".
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "admin")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db) => _db = db;

    // ── GET /api/admin/users ──────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.Role,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    // ── PUT /api/admin/users/{id}/role ────────────────────────────────────────
    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest req)
    {
        if (req.Role is not ("admin" or "user"))
            return BadRequest(new { message = "الدور يجب أن يكون 'admin' أو 'user'" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user is null) return NotFound();

        user.Role = req.Role;
        await _db.SaveChangesAsync();

        return Ok(new { user.Id, user.FullName, user.Email, user.Role });
    }

    // ── DELETE /api/admin/users/{id} ──────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user is null) return NotFound();

        user.IsDeleted = true;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public record UpdateRoleRequest(string Role);
