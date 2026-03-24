using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netaq.Domain.Entities;

namespace Netaq.Infrastructure.Persistence.Configurations;

public class UserTaskConfiguration : IEntityTypeConfiguration<UserTask>
{
    public void Configure(EntityTypeBuilder<UserTask> builder)
    {
        builder.ToTable("UserTasks");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.TitleAr).IsRequired().HasMaxLength(500);
        builder.Property(e => e.TitleEn).IsRequired().HasMaxLength(500);
        builder.Property(e => e.DescriptionAr).HasMaxLength(4000);
        builder.Property(e => e.DescriptionEn).HasMaxLength(4000);
        builder.Property(e => e.EntityType).HasMaxLength(100);
        
        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(e => e.AssignedUser)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(e => e.AssignedUserId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(e => e.WorkflowInstance)
            .WithMany(i => i.Tasks)
            .HasForeignKey(e => e.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(e => e.WorkflowStep)
            .WithMany()
            .HasForeignKey(e => e.WorkflowStepId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class SlaTrackingConfiguration : IEntityTypeConfiguration<SlaTracking>
{
    public void Configure(EntityTypeBuilder<SlaTracking> builder)
    {
        builder.ToTable("SlaTrackings");
        builder.HasKey(e => e.Id);
        
        builder.HasOne(e => e.WorkflowInstance)
            .WithMany(i => i.SlaTrackings)
            .HasForeignKey(e => e.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(e => e.WorkflowStep)
            .WithMany(s => s.SlaTrackings)
            .HasForeignKey(e => e.WorkflowStepId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.ActionType).IsRequired().HasMaxLength(256);
        builder.Property(e => e.ActionDescription).IsRequired().HasMaxLength(4000);
        builder.Property(e => e.EntityType).HasMaxLength(100);
        builder.Property(e => e.IpAddress).HasMaxLength(50);
        builder.Property(e => e.UserAgent).HasMaxLength(1000);
        builder.Property(e => e.Hash).IsRequired().HasMaxLength(128);
        builder.Property(e => e.PreviousHash).IsRequired().HasMaxLength(128);
        
        builder.HasIndex(e => e.SequenceNumber).IsUnique();
        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => new { e.OrganizationId, e.ActionCategory });
        
        builder.HasOne(e => e.Organization)
            .WithMany(o => o.AuditLogs)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(e => e.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        
        // No soft delete filter for audit logs - they are immutable
    }
}

public class AiConfigurationEntityConfiguration : IEntityTypeConfiguration<AiConfiguration>
{
    public void Configure(EntityTypeBuilder<AiConfiguration> builder)
    {
        builder.ToTable("AiConfigurations");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.ProviderName).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Endpoint).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.ModelName).IsRequired().HasMaxLength(256);
        builder.Property(e => e.ApiKeyEncrypted).HasMaxLength(2000);
        builder.Property(e => e.VectorDbEndpoint).HasMaxLength(1000);
        builder.Property(e => e.EmbeddingModel).HasMaxLength(256);
        
        builder.HasOne(e => e.Organization)
            .WithMany(o => o.AiConfigurations)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class PermissionMatrixConfiguration : IEntityTypeConfiguration<PermissionMatrix>
{
    public void Configure(EntityTypeBuilder<PermissionMatrix> builder)
    {
        builder.ToTable("PermissionMatrices");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.CommitteeRole).HasMaxLength(100);
        
        builder.HasOne(e => e.Organization)
            .WithMany(o => o.PermissionMatrices)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(e => e.User)
            .WithMany(u => u.PermissionMatrices)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasIndex(e => new { e.OrganizationId, e.UserId, e.TenderPhase, e.UserRole });
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("Invitations");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Email).IsRequired().HasMaxLength(256);
        builder.Property(e => e.FullNameAr).HasMaxLength(500);
        builder.Property(e => e.FullNameEn).HasMaxLength(500);
        builder.Property(e => e.Token).IsRequired().HasMaxLength(2000);
        
        builder.HasIndex(e => e.Token).IsUnique();
        
        builder.HasOne(e => e.Organization)
            .WithMany(o => o.Invitations)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
