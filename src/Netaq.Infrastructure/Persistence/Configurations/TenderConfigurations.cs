using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netaq.Domain.Entities;

namespace Netaq.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Tender entity.
/// </summary>
public class TenderConfiguration : IEntityTypeConfiguration<Tender>
{
    public void Configure(EntityTypeBuilder<Tender> builder)
    {
        builder.ToTable("Tenders");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.TitleAr).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.TitleEn).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.ReferenceNumber).IsRequired().HasMaxLength(50);
        builder.Property(e => e.DescriptionAr).HasMaxLength(4000);
        builder.Property(e => e.DescriptionEn).HasMaxLength(4000);
        builder.Property(e => e.EstimatedValue).HasPrecision(18, 2);
        builder.Property(e => e.TechnicalWeight).HasPrecision(5, 2);
        builder.Property(e => e.FinancialWeight).HasPrecision(5, 2);
        builder.Property(e => e.CancellationReason).HasMaxLength(2000);
        
        builder.HasIndex(e => e.ReferenceNumber).IsUnique();
        builder.HasIndex(e => e.OrganizationId);
        builder.HasIndex(e => e.Status);
        
        builder.HasOne(e => e.Organization)
            .WithMany(o => o.Tenders)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(e => e.BookletTemplate)
            .WithMany(t => t.Tenders)
            .HasForeignKey(e => e.BookletTemplateId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(e => e.WorkflowInstance)
            .WithMany()
            .HasForeignKey(e => e.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

/// <summary>
/// EF Core configuration for the TenderSection entity.
/// </summary>
public class TenderSectionConfiguration : IEntityTypeConfiguration<TenderSection>
{
    public void Configure(EntityTypeBuilder<TenderSection> builder)
    {
        builder.ToTable("TenderSections");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.TitleAr).IsRequired().HasMaxLength(500);
        builder.Property(e => e.TitleEn).IsRequired().HasMaxLength(500);
        builder.Property(e => e.ContentHtml).HasColumnType("nvarchar(max)");
        builder.Property(e => e.AiComplianceResult).HasColumnType("nvarchar(max)");
        
        builder.HasIndex(e => new { e.TenderId, e.SectionType }).IsUnique();
        
        builder.HasOne(e => e.Tender)
            .WithMany(t => t.Sections)
            .HasForeignKey(e => e.TenderId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

/// <summary>
/// EF Core configuration for the TenderCriteria entity.
/// </summary>
public class TenderCriteriaConfiguration : IEntityTypeConfiguration<TenderCriteria>
{
    public void Configure(EntityTypeBuilder<TenderCriteria> builder)
    {
        builder.ToTable("TenderCriteria");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(500);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(500);
        builder.Property(e => e.DescriptionAr).HasMaxLength(2000);
        builder.Property(e => e.DescriptionEn).HasMaxLength(2000);
        builder.Property(e => e.Weight).HasPrecision(5, 2);
        builder.Property(e => e.PassingThreshold).HasPrecision(5, 2);
        
        builder.HasIndex(e => e.TenderId);
        builder.HasIndex(e => e.ParentId);
        
        builder.HasOne(e => e.Tender)
            .WithMany(t => t.Criteria)
            .HasForeignKey(e => e.TenderId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(e => e.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

/// <summary>
/// EF Core configuration for the BookletTemplate entity.
/// </summary>
public class BookletTemplateConfiguration : IEntityTypeConfiguration<BookletTemplate>
{
    public void Configure(EntityTypeBuilder<BookletTemplate> builder)
    {
        builder.ToTable("BookletTemplates");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(500);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(500);
        builder.Property(e => e.DescriptionAr).HasMaxLength(2000);
        builder.Property(e => e.DescriptionEn).HasMaxLength(2000);
        builder.Property(e => e.Version).HasMaxLength(20);
        
        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.OrganizationId);
        
        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

/// <summary>
/// EF Core configuration for the BookletTemplateSection entity.
/// </summary>
public class BookletTemplateSectionConfiguration : IEntityTypeConfiguration<BookletTemplateSection>
{
    public void Configure(EntityTypeBuilder<BookletTemplateSection> builder)
    {
        builder.ToTable("BookletTemplateSections");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.TitleAr).IsRequired().HasMaxLength(500);
        builder.Property(e => e.TitleEn).IsRequired().HasMaxLength(500);
        builder.Property(e => e.DefaultContentHtml).HasColumnType("nvarchar(max)");
        builder.Property(e => e.GuidanceNotesAr).HasMaxLength(4000);
        builder.Property(e => e.GuidanceNotesEn).HasMaxLength(4000);
        
        builder.HasIndex(e => new { e.BookletTemplateId, e.SectionType }).IsUnique();
        
        builder.HasOne(e => e.BookletTemplate)
            .WithMany(t => t.Sections)
            .HasForeignKey(e => e.BookletTemplateId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

/// <summary>
/// EF Core configuration for the Committee entity.
/// </summary>
public class CommitteeConfiguration : IEntityTypeConfiguration<Committee>
{
    public void Configure(EntityTypeBuilder<Committee> builder)
    {
        builder.ToTable("Committees");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(500);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(500);
        builder.Property(e => e.PurposeAr).HasMaxLength(2000);
        builder.Property(e => e.PurposeEn).HasMaxLength(2000);
        
        builder.HasIndex(e => e.OrganizationId);
        builder.HasIndex(e => e.TenderId);
        
        builder.HasOne(e => e.Organization)
            .WithMany(o => o.Committees)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(e => e.Tender)
            .WithMany()
            .HasForeignKey(e => e.TenderId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

/// <summary>
/// EF Core configuration for the CommitteeMember entity.
/// </summary>
public class CommitteeMemberConfiguration : IEntityTypeConfiguration<CommitteeMember>
{
    public void Configure(EntityTypeBuilder<CommitteeMember> builder)
    {
        builder.ToTable("CommitteeMembers");
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => new { e.CommitteeId, e.UserId }).IsUnique();
        
        builder.HasOne(e => e.Committee)
            .WithMany(c => c.Members)
            .HasForeignKey(e => e.CommitteeId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
