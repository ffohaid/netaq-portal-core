using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Netaq.Domain.Common;

namespace Netaq.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Provides multi-tenancy RLS (Row-Level Security) via EF Core global query filters.
/// Automatically filters all ITenantEntity queries by the current user's OrganizationId.
/// </summary>
public static class TenantQueryFilterExtensions
{
    public static void ApplyTenantQueryFilters(this ModelBuilder modelBuilder, Guid? tenantId)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(TenantQueryFilterExtensions)
                    .GetMethod(nameof(ApplyFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);
                
                method.Invoke(null, new object?[] { modelBuilder, tenantId });
            }
        }
    }

    private static void ApplyFilter<TEntity>(ModelBuilder modelBuilder, Guid? tenantId)
        where TEntity : class, ITenantEntity
    {
        if (tenantId.HasValue)
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.OrganizationId == tenantId.Value);
        }
    }
}

/// <summary>
/// Service to hold the current tenant context.
/// </summary>
public interface ITenantProvider
{
    Guid? GetCurrentTenantId();
    void SetCurrentTenantId(Guid? tenantId);
    bool ShouldBypassTenantFilter { get; set; }
}

public class TenantProvider : ITenantProvider
{
    private Guid? _tenantId;
    
    public Guid? GetCurrentTenantId() => _tenantId;
    
    public void SetCurrentTenantId(Guid? tenantId) => _tenantId = tenantId;
    
    public bool ShouldBypassTenantFilter { get; set; } = false;
}
