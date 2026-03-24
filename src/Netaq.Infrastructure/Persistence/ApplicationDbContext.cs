using Microsoft.EntityFrameworkCore;
using Netaq.Domain.Entities;
using Netaq.Domain.Interfaces;

namespace Netaq.Infrastructure.Persistence;

/// <summary>
/// Main application database context with multi-tenancy support via RLS interceptors.
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<PermissionMatrix> PermissionMatrices => Set<PermissionMatrix>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<WorkflowTemplate> WorkflowTemplates => Set<WorkflowTemplate>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowAction> WorkflowActions => Set<WorkflowAction>();
    public DbSet<UserTask> UserTasks => Set<UserTask>();
    public DbSet<SlaTracking> SlaTrackings => Set<SlaTracking>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AiConfiguration> AiConfigurations => Set<AiConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
