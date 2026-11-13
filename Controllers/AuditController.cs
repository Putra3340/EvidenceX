using EvidenceX.Models.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace EvidenceX.Controllers;

[ApiController, Route("api/audit"), Authorize(Roles = "Administrator,Auditor")]
public sealed class AuditController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] long afterSequence = 0,
        [FromQuery] int take = 100, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 500);
        return Ok(await db.AuditEvents.AsNoTracking()
            .Where(x => x.SequenceNumber > afterSequence)
            .OrderBy(x => x.SequenceNumber).Take(take).ToListAsync(ct));
    }

    [HttpPost("verify-chain")]
    public async Task<IActionResult> VerifyChain(CancellationToken ct)
    {
        var events = await db.AuditEvents.AsNoTracking()
            .OrderBy(x => x.SequenceNumber).ToListAsync(ct);
        var previous = new string('0', 64);
        long expectedSequence = 1;
        foreach (var e in events)
        {
            if (e.SequenceNumber != expectedSequence || e.PreviousEventHash != previous)
                return Ok(new { valid = false, brokenAt = e.SequenceNumber, reason = "Sequence or previous hash mismatch." });
            var canonical = $"{e.SequenceNumber}|{e.ActorId}|{e.EventType}|{e.EntityType}|{e.EntityId}|{e.OccurredAt:O}|{e.PreviousEventHash}|{e.PayloadJson}";
            var actual = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
            if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(actual), Convert.FromHexString(e.EventHash)))
                return Ok(new { valid = false, brokenAt = e.SequenceNumber, reason = "Event hash mismatch." });
            previous = e.EventHash;
            expectedSequence++;
        }
        return Ok(new { valid = true, eventsChecked = events.Count, headHash = previous });
    }
}