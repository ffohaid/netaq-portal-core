using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netaq.Domain.Entities;

namespace Netaq.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.FullNameAr).IsRequired().HasMaxLength(500);
        builder.Property(e => e.FullNameEn).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Phone).HasMaxLength(50);
        builder.Property(e => e.JobTitleAr).HasMaxLength(256);
        builder.Property(e => e.JobTitleEn).HasMaxLength(256);
        builder.Property(e => e.DepartmentAr).HasMaxLength(256);
        builder.Property(e => e.DepartmentEn).HasMaxLength(256);
        builder.Property(e => e.Locale).IsRequired().HasMaxLength(5).HasDefaultValue("ar");
        builder.Property(e => e.PasswordHash).HasMaxLength(500);
        builder.Property(e => e.PasswordSalt).HasMaxLength(500);
        builder.Property(e => e.OtpCode).HasMaxLength(10);
        builder.Property(e => e.ExternalId).HasMaxLength(500);
        builder.Property(e => e.AvatarUrl).HasMaxLength(1000);
        
        builder.HasIndex(e => new { e.OrganizationId, e.Email }).IsUnique();
        
        // STRICT: DeleteBehavior.NoAction for ALL relationships
        builder.HasOne(e => e.Organization)
            .WithMany(o => o.Users)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
