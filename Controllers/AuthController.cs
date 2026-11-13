using EvidenceX.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EvidenceX.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(UserManager<AppUser> users, RoleManager<IdentityRole<Guid>> roles, IConfiguration config) : ControllerBase
{
    public record RegisterRequest(string Email, string Password, string DisplayName);
    public record LoginRequest(string Email, string Password);

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest r)
    {
        var user = new AppUser { UserName = r.Email, Email = r.Email, DisplayName = r.DisplayName };
        var result = await users.CreateAsync(user, r.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);
        // New registrations get the least-privileged role.
        if (!await roles.RoleExistsAsync("Investigator"))
            await roles.CreateAsync(new IdentityRole<Guid>("Investigator"));
        await users.AddToRoleAsync(user, "Investigator");
        return Created("", new { user.Id, user.Email, user.DisplayName });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest r)
    {
        var user = await users.FindByEmailAsync(r.Email);
        if (user is null || !await users.CheckPasswordAsync(user, r.Password))
            return Unauthorized(new { message = "Invalid credentials." });

        var claims = new List<Claim> {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? "")
        };
        claims.AddRange((await users.GetRolesAsync(user)).Select(x => new Claim(ClaimTypes.Role, x)));
        var section = config.GetSection("Jwt");
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(section["Key"]!));
        var token = new JwtSecurityToken(section["Issuer"], section["Audience"], claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));
        return Ok(new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt = token.ValidTo
        });
    }
}