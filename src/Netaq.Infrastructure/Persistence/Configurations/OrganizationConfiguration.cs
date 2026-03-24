using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netaq.Domain.Entities;

namespace Netaq.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(500);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(500);
        builder.Property(e => e.DescriptionAr).HasMaxLength(2000);
        builder.Property(e => e.DescriptionEn).HasMaxLength(2000);
        builder.Property(e => e.Email).HasMaxLength(256);
        builder.Property(e => e.Phone).HasMaxLength(50);
        builder.Property(e => e.Website).HasMaxLength(500);
        builder.Property(e => e.LogoUrl).HasMaxLength(1000);
        builder.Property(e => e.Address).HasMaxLength(1000);
        
        // SSO encrypted fields
        builder.Property(e => e.SsoClientSecretEncrypted).HasMaxLength(2000);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
