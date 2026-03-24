using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netaq.Domain.Entities;

namespace Netaq.Infrastructure.Persistence.Configurations;

public class WorkflowTemplateConfiguration : IEntityTypeConfiguration<WorkflowTemplate>
{
    public void Configure(EntityTypeBuilder<WorkflowTemplate> builder)
    {
        builder.ToTable("WorkflowTemplates");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(500);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(500);
        builder.Property(e => e.DescriptionAr).HasMaxLength(2000);
        builder.Property(e => e.DescriptionEn).HasMaxLength(2000);
        
        builder.HasOne(e => e.Organization)
            .WithMany(o => o.WorkflowTemplates)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>
{
    public void Configure(EntityTypeBuilder<WorkflowStep> builder)
    {
        builder.ToTable("WorkflowSteps");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(500);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(500);
        builder.Property(e => e.DescriptionAr).HasMaxLength(2000);
        builder.Property(e => e.DescriptionEn).HasMaxLength(2000);
        builder.Property(e => e.ParallelGroupId).HasMaxLength(100);
        builder.Property(e => e.ConditionExpression).HasMaxLength(1000);
        
        builder.HasOne(e => e.WorkflowTemplate)
            .WithMany(t => t.Steps)
            .HasForeignKey(e => e.WorkflowTemplateId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(e => e.AssignedUser)
            .WithMany()
            .HasForeignKey(e => e.AssignedUserId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable("WorkflowInstances");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.EntityType).HasMaxLength(100);
        
        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(e => e.WorkflowTemplate)
            .WithMany(t => t.Instances)
            .HasForeignKey(e => e.WorkflowTemplateId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(e => e.CurrentStep)
            .WithMany()
            .HasForeignKey(e => e.CurrentStepId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class WorkflowActionConfiguration : IEntityTypeConfiguration<WorkflowAction>
{
    public void Configure(EntityTypeBuilder<WorkflowAction> builder)
    {
        builder.ToTable("WorkflowActions");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Justification).HasMaxLength(4000);
        builder.Property(e => e.Notes).HasMaxLength(4000);
        
        builder.HasOne(e => e.WorkflowInstance)
            .WithMany(i => i.Actions)
            .HasForeignKey(e => e.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(e => e.WorkflowStep)
            .WithMany()
            .HasForeignKey(e => e.WorkflowStepId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(e => e.ActorUser)
            .WithMany(u => u.WorkflowActions)
            .HasForeignKey(e => e.ActorUserId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(e => e.DelegatedToUser)
            .WithMany()
            .HasForeignKey(e => e.DelegatedToUserId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
