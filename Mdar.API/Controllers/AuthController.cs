using Mdar.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Mdar.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, IConfiguration config) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email && !u.IsDeleted);
        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "البريد الإلكتروني أو كلمة المرور غير صحيحة" });

        var jwtSection = config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(jwtSection.GetValue<int>("ExpiryMinutes", 1440));

        var token = new JwtSecurityToken(
            issuer:   jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("name", user.FullName)
            ],
            expires:  expiry,
            signingCredentials: creds
        );

        return Ok(new
        {
            token   = new JwtSecurityTokenHandler().WriteToken(token),
            expires = expiry,
            name    = user.FullName,
            email   = user.Email
        });
    }
}

public record LoginRequest(string Email, string Password);
