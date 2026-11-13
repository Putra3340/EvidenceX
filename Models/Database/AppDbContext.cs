using EvidenceX.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EvidenceX.Models.Database;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<Evidence> Evidence => Set<Evidence>();
    public DbSet<CustodyTransfer> CustodyTransfers => Set<CustodyTransfer>();
    public DbSet<IntegrityCheck> IntegrityChecks => Set<IntegrityCheck>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

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