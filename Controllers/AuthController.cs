using EvidenceX.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EvidenceX.Controllers;

[ApiController, Route("api/auth"), EnableRateLimiting("auth")]
public sealed class AuthController(UserManager<AppUser> users, RoleManager<IdentityRole<Guid>> roles, IConfiguration config) : ControllerBase
{
    public record RegisterRequest([EmailAddress] string Email, string Password, string DisplayName);
    public record LoginRequest(string Email, string Password);

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest r)
    {
        var user = new AppUser { UserName = r.Email, Email = r.Email, DisplayName = r.DisplayName };
        var result = await users.CreateAsync(user, r.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);
        // New registrations get the least-privileged role. Creating it races
        // with the very first concurrent registrations; a DuplicateRoleName
        // failure means another request already created it, which is fine.
        if (await roles.FindByNameAsync("Investigator") is null)
        {
            var role = await roles.CreateAsync(new IdentityRole<Guid>("Investigator"));
            if (!role.Succeeded && role.Errors.All(e => e.Code != "DuplicateRoleName"))
                return BadRequest(role.Errors);
        }
        await users.AddToRoleAsync(user, "Investigator");
        return Created("", new { user.Id, user.Email, user.DisplayName });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest r)
    {
        var user = await users.FindByEmailAsync(r.Email);
        if (user is null)
            return Unauthorized(new { message = "Invalid credentials." });

        if (await users.IsLockedOutAsync(user))
            return Unauthorized(new { message = "Account temporarily locked after repeated failed attempts. Try again later." });

        if (!await users.CheckPasswordAsync(user, r.Password))
        {
            // CheckPasswordAsync does not touch the lockout counters itself;
            // record the failure so repeated guessing locks the account.
            await users.AccessFailedAsync(user);
            return Unauthorized(new { message = "Invalid credentials." });
        }
        await users.ResetAccessFailedCountAsync(user);

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