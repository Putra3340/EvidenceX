using EvidenceX.Domain;
using EvidenceX.Models.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EvidenceX.Controllers;

[ApiController, Route("api/cases"), Authorize]
public sealed class CasesController(AppDbContext db) : ControllerBase
{
    public record CreateCaseRequest(string Title, string? Description);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> Create(CreateCaseRequest r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Title)) return BadRequest("Title is required.");
        var c = new Case
        {
            CaseNumber = $"CASE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..25],
            Title = r.Title.Trim(),
            Description = r.Description ?? "",
            CreatedById = UserId
        };
        db.Cases.Add(c); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = c.Id }, c);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await db.Cases.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var c = await db.Cases.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return c is null ? NotFound() : Ok(c);
    }
}