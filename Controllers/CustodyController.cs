using EvidenceX.Domain;
using EvidenceX.Models.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EvidenceX.Controllers;

[ApiController, Route("api"), Authorize]
public sealed class CustodyController(AppDbContext db, IAuditWriter audit) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public record TransferRequest(Guid ToUserId, string Purpose, string? Notes);

    [HttpPost("evidence/{evidenceId:guid}/transfers"), Authorize(Policy = "Custody")]
    public async Task<IActionResult> RequestTransfer(Guid evidenceId, TransferRequest r, CancellationToken ct)
    {
        var e = await db.Evidence.FirstOrDefaultAsync(x => x.Id == evidenceId && !x.IsArchived, ct);
        if (e is null) return NotFound("Evidence not found.");
        if (e.CurrentCustodianId != UserId) return Forbid();
        if (r.ToUserId == UserId) return BadRequest("Cannot transfer to yourself.");
        if (!await db.Users.AnyAsync(x => x.Id == r.ToUserId, ct)) return BadRequest("Recipient not found.");
        if (await db.CustodyTransfers.AnyAsync(x => x.EvidenceId == evidenceId &&
            x.Status == TransferStatus.Pending, ct)) return Conflict("A pending transfer already exists.");

        var transfer = new CustodyTransfer
        {
            EvidenceId = evidenceId,
            FromUserId = UserId,
            ToUserId = r.ToUserId,
            Purpose = r.Purpose.Trim(),
            Notes = r.Notes
        };
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        db.CustodyTransfers.Add(transfer); await db.SaveChangesAsync(ct);
        await audit.WriteAsync(UserId, "TransferRequested", "CustodyTransfer", transfer.Id.ToString(),
            new { evidenceId, r.ToUserId, r.Purpose }, ct);
        await tx.CommitAsync(ct);
        return Created($"/api/transfers/{transfer.Id}", transfer);
    }

    [HttpPost("transfers/{id:guid}/accept"), Authorize(Policy = "Custody")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var t = await db.CustodyTransfers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        if (t.ToUserId != UserId) return Forbid();
        if (t.Status != TransferStatus.Pending) return Conflict("Transfer is not pending.");
        var e = await db.Evidence.FirstOrDefaultAsync(x => x.Id == t.EvidenceId, ct);
        if (e is null) return NotFound("Evidence not found.");
        if (e.IsArchived) return Conflict("Evidence is archived; custody cannot change.");
        if (e.CurrentCustodianId != t.FromUserId) return Conflict("Custodian changed since request.");
        t.Status = TransferStatus.Accepted; t.RespondedAt = DateTimeOffset.UtcNow;
        e.CurrentCustodianId = UserId;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(UserId, "TransferAccepted", "CustodyTransfer", id.ToString(),
            new { t.EvidenceId, t.FromUserId, t.ToUserId }, ct);
        await tx.CommitAsync(ct);
        return Ok(t);
    }

    [HttpPost("transfers/{id:guid}/reject"), Authorize(Policy = "Custody")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct)
    {
        var t = await db.CustodyTransfers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        if (t.ToUserId != UserId) return Forbid();
        if (t.Status != TransferStatus.Pending) return Conflict("Transfer is not pending.");
        t.Status = TransferStatus.Rejected; t.RespondedAt = DateTimeOffset.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(UserId, "TransferRejected", "CustodyTransfer", id.ToString(), new { }, ct);
        await tx.CommitAsync(ct);
        return Ok(t);
    }

    [HttpGet("evidence/{evidenceId:guid}/custody")]
    public async Task<IActionResult> History(Guid evidenceId, CancellationToken ct)
    {
        if (!await db.Evidence.AnyAsync(x => x.Id == evidenceId, ct)) return NotFound();
        return Ok(await db.CustodyTransfers.AsNoTracking()
            .Where(x => x.EvidenceId == evidenceId).OrderBy(x => x.RequestedAt).ToListAsync(ct));
    }

    [HttpGet("transfers/pending")]
    public async Task<IActionResult> Pending(CancellationToken ct) =>
        Ok(await db.CustodyTransfers.AsNoTracking()
            .Where(x => x.ToUserId == UserId && x.Status == TransferStatus.Pending)
            .OrderBy(x => x.RequestedAt).ToListAsync(ct));
}