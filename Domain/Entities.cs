namespace EvidenceX.Domain;

public enum TransferStatus
{ Pending, Accepted, Rejected, Cancelled }

public enum IntegrityStatus
{ Match, Mismatch, Error }

public sealed class AppUser : Microsoft.AspNetCore.Identity.IdentityUser<Guid>
{
    public string DisplayName { get; set; } = "";
}

public sealed class Case
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CaseNumber { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "Open";
    public Guid CreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Evidence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EvidenceCode { get; set; } = "";
    public Guid CaseId { get; set; }
    public string OriginalFileName { get; set; } = "";
    public string StoredFileName { get; set; } = "";
    public string StorageKey { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public string OriginalSha256 { get; set; } = "";
    public Guid RegisteredById { get; set; }
    public Guid CurrentCustodianId { get; set; }
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsArchived { get; set; }
}

public sealed class CustodyTransfer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EvidenceId { get; set; }
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.Pending;
    public string Purpose { get; set; } = "";
    public string? Notes { get; set; }
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RespondedAt { get; set; }
}

public sealed class IntegrityCheck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EvidenceId { get; set; }
    public string ExpectedHash { get; set; } = "";
    public string? ActualHash { get; set; }
    public IntegrityStatus Status { get; set; }
    public Guid? CheckedById { get; set; }
    public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Error { get; set; }
}

public sealed class AuditEvent
{
    public long Id { get; set; }
    public long SequenceNumber { get; set; }
    public Guid? ActorId { get; set; }
    public string EventType { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public string PreviousEventHash { get; set; } = "";
    public string EventHash { get; set; } = "";
}