namespace EvidenceX.Domain;

public interface IEvidenceStorage
{
    Task<string> SaveAsync(Stream source, string fileName, CancellationToken ct);

    Task<Stream> OpenReadAsync(string key, CancellationToken ct);

    Task DeleteAsync(string key, CancellationToken ct);
}

public interface IHashService
{
    Task<string> Sha256Async(Stream stream, CancellationToken ct);
}

public interface IAuditWriter
{
    Task WriteAsync(Guid? actorId, string eventType, string entityType,
        string entityId, object payload, CancellationToken ct);
}