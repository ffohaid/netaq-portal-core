using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netaq.Domain.Entities;

namespace Netaq.Infrastructure.Persistence.Configurations;

public class InquiryConfiguration : IEntityTypeConfiguration<Inquiry>
{
    public void Configure(EntityTypeBuilder<Inquiry> builder)
    {
        builder.ToTable("Inquiries");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.SubjectAr).HasMaxLength(500).IsRequired();
        builder.Property(i => i.SubjectEn).HasMaxLength(500).IsRequired();
        builder.Property(i => i.QuestionAr).IsRequired();
        builder.Property(i => i.QuestionEn).IsRequired();
        builder.Property(i => i.ResponseAr);
        builder.Property(i => i.ResponseEn);
        builder.Property(i => i.Status).IsRequired();
        builder.Property(i => i.Priority).IsRequired();
        builder.Property(i => i.Category).IsRequired();

        builder.HasQueryFilter(i => !i.IsDeleted);

        // Relationships
        builder.HasOne(i => i.Organization)
            .WithMany()
            .HasForeignKey(i => i.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(i => i.Tender)
            .WithMany()
            .HasForeignKey(i => i.TenderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(i => i.SubmittedByUser)
            .WithMany()
            .HasForeignKey(i => i.SubmittedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(i => i.AssignedToUser)
            .WithMany()
            .HasForeignKey(i => i.AssignedToUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(i => i.TenderSection)
            .WithMany()
            .HasForeignKey(i => i.TenderSectionId)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes
        builder.HasIndex(i => i.OrganizationId);
        builder.HasIndex(i => i.TenderId);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.SubmittedByUserId);
    }
}
