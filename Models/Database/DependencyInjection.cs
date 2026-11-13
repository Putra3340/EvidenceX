using EvidenceX.Domain;
using Microsoft.EntityFrameworkCore;

namespace EvidenceX.Models.Database;

public static class DependencyInjection
{
    public static IServiceCollection AddBuktiChainInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IHashService, Sha256Service>();
        services.AddScoped<IEvidenceStorage, LocalEvidenceStorage>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        return services;
    }
}