using EvidenceX.Domain;
using EvidenceX.Models.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EvidenceX.Controllers;

[ApiController, Route("api/evidence/{evidenceId:guid}/integrity"), Authorize]
public sealed class IntegrityController(AppDbContext db, IEvidenceStorage storage,
    IHashService hasher, IAuditWriter audit) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("verify"), Authorize(Policy = "Investigator")]
    public async Task<IActionResult> Verify(Guid evidenceId, CancellationToken ct)
    {
        var e = await db.Evidence.FirstOrDefaultAsync(x => x.Id == evidenceId, ct);
        if (e is null) return NotFound();
        var check = new IntegrityCheck
        {
            EvidenceId = evidenceId,
            ExpectedHash = e.OriginalSha256,
            CheckedById = UserId
        };
        try
        {
            await using var stream = await storage.OpenReadAsync(e.StorageKey, ct);
            check.ActualHash = await hasher.Sha256Async(stream, ct);
            check.Status = string.Equals(check.ExpectedHash, check.ActualHash,
                StringComparison.OrdinalIgnoreCase) ? IntegrityStatus.Match : IntegrityStatus.Mismatch;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            check.Status = IntegrityStatus.Error;
            check.Error = ex.GetType().Name;
        }
        db.IntegrityChecks.Add(check);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(UserId,
            check.Status == IntegrityStatus.Match ? "IntegrityVerified" :
            check.Status == IntegrityStatus.Mismatch ? "IntegrityMismatchDetected" : "IntegrityCheckError",
            "IntegrityCheck", check.Id.ToString(),
            new { check.EvidenceId, check.ExpectedHash, check.ActualHash, check.Status }, ct);
        return Ok(check);
    }

    [HttpGet("checks")]
    public async Task<IActionResult> History(Guid evidenceId, CancellationToken ct)
    {
        if (!await db.Evidence.AnyAsync(x => x.Id == evidenceId, ct)) return NotFound();
        return Ok(await db.IntegrityChecks.AsNoTracking()
            .Where(x => x.EvidenceId == evidenceId).OrderByDescending(x => x.CheckedAt).ToListAsync(ct));
    }
}