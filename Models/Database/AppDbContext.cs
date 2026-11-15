using System.Globalization;
using EvidenceX.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EvidenceX.Models.Database;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<Evidence> Evidence => Set<Evidence>();
    public DbSet<CustodyTransfer> CustodyTransfers => Set<CustodyTransfer>();
    public DbSet<IntegrityCheck> IntegrityChecks => Set<IntegrityCheck>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite stores timestamps as text and its provider cannot translate
        // ORDER BY / comparisons over DateTimeOffset. Mapping every
        // DateTimeOffset property through a text converter keeps those queries
        // translatable while the domain keeps its type. Values are normalized
        // to UTC on write so text sort order stays chronological.
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetTextConverter>();
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Case>().HasIndex(x => x.CaseNumber).IsUnique();
        b.Entity<Evidence>().HasIndex(x => x.EvidenceCode).IsUnique();
        b.Entity<Evidence>().HasIndex(x => x.CaseId);
        b.Entity<Evidence>().HasIndex(x => x.CurrentCustodianId);
        b.Entity<CustodyTransfer>().HasIndex(x => new { x.EvidenceId, x.Status });
        b.Entity<AuditEvent>().HasIndex(x => x.SequenceNumber).IsUnique();
        b.Entity<AuditEvent>()
    .Property(x => x.PayloadJson)
    .HasColumnType("TEXT");
        b.Entity<AuditEvent>().ToTable(t => t.HasCheckConstraint(
            "ck_audit_event_hash", "length(\"EventHash\") = 64"));
    }
}

// Reads both the current round-trip format ("2026-11-14T15:03:10.0311865+00:00")
// and the legacy EF SQLite format ("2026-11-14 15:03:10.1283948+00:00") so
// rows written before this converter remain readable.
internal sealed class DateTimeOffsetTextConverter : ValueConverter<DateTimeOffset, string>
{
    public DateTimeOffsetTextConverter()
        : base(
            v => v.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            s => DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind))
    {
    }
}