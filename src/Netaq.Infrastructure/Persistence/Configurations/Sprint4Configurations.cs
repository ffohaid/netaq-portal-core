using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netaq.Domain.Entities;

namespace Netaq.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for KnowledgeSource entity.
/// </summary>
public class KnowledgeSourceConfiguration : IEntityTypeConfiguration<KnowledgeSource>
{
    public void Configure(EntityTypeBuilder<KnowledgeSource> builder)
    {
        builder.ToTable("KnowledgeSources");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.TitleAr).HasMaxLength(500).IsRequired();
        builder.Property(e => e.TitleEn).HasMaxLength(500).IsRequired();
        builder.Property(e => e.DescriptionAr).HasMaxLength(2000);
        builder.Property(e => e.DescriptionEn).HasMaxLength(2000);
        builder.Property(e => e.FileUrl).HasMaxLength(1000);
        builder.Property(e => e.VectorDocumentId).HasMaxLength(200);
        builder.Property(e => e.IndexingError).HasMaxLength(2000);
        builder.Property(e => e.SourceType).HasConversion<int>();
        builder.Property(e => e.IndexingStatus).HasConversion<int>();
        
        builder.HasOne(e => e.Organization)
            .WithMany(o => o.KnowledgeSources)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);
            
        builder.HasOne(e => e.Tender)
            .WithMany()
            .HasForeignKey(e => e.TenderId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasIndex(e => new { e.OrganizationId, e.SourceType });
        builder.HasIndex(e => e.IndexingStatus);
        builder.HasIndex(e => e.TenderId);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

/// <summary>
/// EF Core configuration for SystemSetting entity.
/// </summary>
public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Category).HasMaxLength(100).IsRequired();
        builder.Property(e => e.SettingKey).HasMaxLength(200).IsRequired();
        builder.Property(e => e.SettingValue).HasMaxLength(4000).IsRequired();
        builder.Property(e => e.LabelAr).HasMaxLength(500);
        builder.Property(e => e.LabelEn).HasMaxLength(500);
        builder.Property(e => e.DataType).HasMaxLength(50).HasDefaultValue("string");
        
        builder.HasOne(e => e.Organization)
            .WithMany(o => o.SystemSettings)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasIndex(e => new { e.OrganizationId, e.Category, e.SettingKey }).IsUnique();
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
