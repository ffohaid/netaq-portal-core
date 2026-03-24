namespace Netaq.Domain.Common;

/// <summary>
/// Interface for entities that belong to a specific organization (tenant).
/// Used by EF Core interceptors for automatic RLS filtering.
/// </summary>
public interface ITenantEntity
{
    Guid OrganizationId { get; set; }
}
