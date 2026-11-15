using EvidenceX.Domain;
using EvidenceX.Models.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EvidenceX.Controllers;

[ApiController, Route("api/evidence"), Authorize]
public sealed class EvidenceController(
    AppDbContext db, IEvidenceStorage storage, IHashService hasher,
    IAuditWriter audit) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost, Authorize(Policy = "Investigator"), RequestSizeLimit(10L * 1024 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Register([FromForm] Guid caseId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length <= 0) return BadRequest("A non-empty file is required.");
        if (!await db.Cases.AnyAsync(x => x.Id == caseId, ct)) return NotFound("Case not found.");

        await using var input = file.OpenReadStream();
        var key = "";
        try
        {
            // Evidence row, hash and audit event commit together or not at
            // all; the stored file is removed when the transaction fails.
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            key = await storage.SaveAsync(input, Path.GetFileName(file.FileName), ct);
            string hash;
            await using (var stored = await storage.OpenReadAsync(key, ct))
                hash = await hasher.Sha256Async(stored, ct);

            var evidence = new Evidence
            {
                EvidenceCode = $"EV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..31],
                CaseId = caseId,
                OriginalFileName = Path.GetFileName(file.FileName),
                StoredFileName = Path.GetFileName(key),
                StorageKey = key,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                SizeBytes = file.Length,
                OriginalSha256 = hash,
                RegisteredById = UserId,
                CurrentCustodianId = UserId
            };
            db.Evidence.Add(evidence);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync(UserId, "EvidenceRegistered", "Evidence", evidence.Id.ToString(),
                new { evidence.EvidenceCode, evidence.OriginalSha256, evidence.SizeBytes }, ct);
            await tx.CommitAsync(ct);
            return CreatedAtAction(nameof(Get), new { id = evidence.Id }, ToDto(evidence));
        }
        catch
        {
            if (key.Length > 0) await storage.DeleteAsync(key, ct);
            throw;
        }
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? caseId, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var q = db.Evidence.AsNoTracking().Where(x => !x.IsArchived);
        if (caseId.HasValue) q = q.Where(x => x.CaseId == caseId);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.RegisteredAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(new { page, pageSize, total, items = items.Select(ToDto) });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var e = await db.Evidence.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return e is null ? NotFound() : Ok(ToDto(e));
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var e = await db.Evidence.FirstOrDefaultAsync(x => x.Id == id && !x.IsArchived, ct);
        if (e is null) return NotFound();
        var stream = await storage.OpenReadAsync(e.StorageKey, ct);
        await audit.WriteAsync(UserId, "EvidenceDownloaded", "Evidence", id.ToString(),
            new { e.EvidenceCode }, ct);
        return File(stream, e.ContentType, e.OriginalFileName, enableRangeProcessing: true);
    }

    [HttpPost("{id:guid}/archive"), Authorize(Policy = "Investigator")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        var e = await db.Evidence.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return NotFound();
        e.IsArchived = true;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(UserId, "EvidenceArchived", "Evidence", id.ToString(), new { }, ct);
        await tx.CommitAsync(ct);
        return NoContent();
    }

    private static object ToDto(Evidence e) => new
    {
        e.Id,
        e.EvidenceCode,
        e.CaseId,
        e.OriginalFileName,
        e.ContentType,
        e.SizeBytes,
        e.OriginalSha256,
        e.RegisteredById,
        e.CurrentCustodianId,
        e.RegisteredAt,
        e.IsArchived
    };
}