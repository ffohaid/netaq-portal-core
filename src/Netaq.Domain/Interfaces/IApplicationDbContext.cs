using Microsoft.EntityFrameworkCore;
using Netaq.Domain.Entities;

namespace Netaq.Domain.Interfaces;

/// <summary>
/// Application database context interface for dependency inversion.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<User> Users { get; }
    DbSet<PermissionMatrix> PermissionMatrices { get; }
    DbSet<Invitation> Invitations { get; }
    DbSet<WorkflowTemplate> WorkflowTemplates { get; }
    DbSet<WorkflowStep> WorkflowSteps { get; }
    DbSet<WorkflowInstance> WorkflowInstances { get; }
    DbSet<WorkflowAction> WorkflowActions { get; }
    DbSet<UserTask> UserTasks { get; }
    DbSet<SlaTracking> SlaTrackings { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<AiConfiguration> AiConfigurations { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
