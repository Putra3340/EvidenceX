using EvidenceX.Domain;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EvidenceX.Models.Database;

public sealed class Sha256Service : IHashService
{
    public async Task<string> Sha256Async(Stream stream, CancellationToken ct)
    {
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed class LocalEvidenceStorage(IConfiguration config) : IEvidenceStorage
{
    private readonly string _root = Path.GetFullPath(
        config["Storage:Root"] ?? "./evidence-data");

    public async Task<string> SaveAsync(Stream source, string fileName, CancellationToken ct)
    {
        Directory.CreateDirectory(_root);
        var key = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}.bin";
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var target = new FileStream(path, FileMode.CreateNew,
            FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.Asynchronous);
        await source.CopyToAsync(target, ct);
        await target.FlushAsync(ct);
        return key;
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Stream stream = new FileStream(Resolve(key), FileMode.Open, FileAccess.Read,
            FileShare.Read, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var path = Resolve(key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string Resolve(string key)
    {
        var path = Path.GetFullPath(Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid storage key.");
        return path;
    }
}

public sealed class AuditWriter(AppDbContext db) : IAuditWriter
{
    public async Task WriteAsync(Guid? actorId, string eventType, string entityType,
        string entityId, object payload, CancellationToken ct)
    {
        var ownsTransaction = db.Database.CurrentTransaction is null;
        // Serialize writes through a transaction/advisory lock in production.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync(
            "LOCK TABLE \"AuditEvents\" IN EXCLUSIVE MODE", ct);

        var previous = await db.AuditEvents.OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefaultAsync(ct);
        var sequence = (previous?.SequenceNumber ?? 0) + 1;
        var occurred = DateTimeOffset.UtcNow;
        var json = JsonSerializer.Serialize(payload);
        var previousHash = previous?.EventHash ?? new string('0', 64);
        var canonical = $"{sequence}|{actorId}|{eventType}|{entityType}|{entityId}|{occurred:O}|{previousHash}|{json}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))
            .ToLowerInvariant();

        db.AuditEvents.Add(new AuditEvent
        {
            SequenceNumber = sequence,
            ActorId = actorId,
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            OccurredAt = occurred,
            PayloadJson = json,
            PreviousEventHash = previousHash,
            EventHash = hash
        });
        await db.SaveChangesAsync(ct);
        if (ownsTransaction)
            await tx!.CommitAsync(ct);
    }
}