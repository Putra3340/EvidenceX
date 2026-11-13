using EvidenceX.Domain;
using EvidenceX.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace EvidenceX.Services;

public sealed class IntegrityScanWorker(IServiceScopeFactory scopes,
    ILogger<IntegrityScanWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await Scan(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "Scheduled integrity scan failed."); }
            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task Scan(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IEvidenceStorage>();
        var hash = scope.ServiceProvider.GetRequiredService<IHashService>();
        var audit = scope.ServiceProvider.GetRequiredService<IAuditWriter>();
        var ids = await db.Evidence.AsNoTracking().Where(x => !x.IsArchived)
            .Select(x => x.Id).ToListAsync(ct);

        foreach (var id in ids)
        {
            var e = await db.Evidence.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) continue;
            var check = new IntegrityCheck { EvidenceId = id, ExpectedHash = e.OriginalSha256 };
            try
            {
                await using var stream = await storage.OpenReadAsync(e.StorageKey, ct);
                check.ActualHash = await hash.Sha256Async(stream, ct);
                check.Status = check.ExpectedHash == check.ActualHash
                    ? IntegrityStatus.Match : IntegrityStatus.Mismatch;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                check.Status = IntegrityStatus.Error; check.Error = ex.GetType().Name;
            }
            db.IntegrityChecks.Add(check);
            await db.SaveChangesAsync(ct);
            if (check.Status != IntegrityStatus.Match)
                await audit.WriteAsync(null, "ScheduledIntegrityAnomaly", "Evidence", id.ToString(),
                    new { check.Id, check.Status, check.ExpectedHash, check.ActualHash }, ct);
        }
    }
}