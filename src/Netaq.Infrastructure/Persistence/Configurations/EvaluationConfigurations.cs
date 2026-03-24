using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netaq.Domain.Entities;

namespace Netaq.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configurations for Sprint 3 - Offer Evaluation entities.
/// All relationships use DeleteBehavior.NoAction as per project rules.
/// </summary>

// ===== Proposal Configuration =====
public class ProposalConfiguration : IEntityTypeConfiguration<Proposal>
{
    public void Configure(EntityTypeBuilder<Proposal> builder)
    {
        builder.ToTable("Proposals");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.VendorNameAr).IsRequired().HasMaxLength(500);
        builder.Property(p => p.VendorNameEn).HasMaxLength(500);
        builder.Property(p => p.VendorReferenceNumber).IsRequired().HasMaxLength(100);
        builder.Property(p => p.TotalValue).HasPrecision(18, 2);
        builder.Property(p => p.TechnicalScore).HasPrecision(10, 4);
        builder.Property(p => p.FinancialScore).HasPrecision(10, 4);
        builder.Property(p => p.FinalScore).HasPrecision(10, 4);
        builder.Property(p => p.ComplianceFailureReason).HasMaxLength(2000);
        builder.Property(p => p.Notes).HasMaxLength(4000);

        // AI fields - large text
        builder.Property(p => p.AiSummaryAr).HasColumnType("nvarchar(max)");
        builder.Property(p => p.AiSummaryEn).HasColumnType("nvarchar(max)");
        builder.Property(p => p.AiGapAnalysisJson).HasColumnType("nvarchar(max)");
        builder.Property(p => p.AiAutoMappingJson).HasColumnType("nvarchar(max)");

        // Indexes
        builder.HasIndex(p => p.TenderId);
        builder.HasIndex(p => p.OrganizationId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.VendorReferenceNumber);
        builder.HasIndex(p => new { p.TenderId, p.VendorReferenceNumber }).IsUnique();

        // Relationships
        builder.HasOne(p => p.Tender)
            .WithMany(t => t.Proposals)
            .HasForeignKey(p => p.TenderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(p => p.Organization)
            .WithMany()
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);

        // Soft delete filter
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}

// ===== ProposalFile Configuration =====
public class ProposalFileConfiguration : IEntityTypeConfiguration<ProposalFile>
{
    public void Configure(EntityTypeBuilder<ProposalFile> builder)
    {
        builder.ToTable("ProposalFiles");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.OriginalFileName).IsRequired().HasMaxLength(500);
        builder.Property(f => f.StoredFileName).IsRequired().HasMaxLength(500);
        builder.Property(f => f.BucketName).IsRequired().HasMaxLength(100);
        builder.Property(f => f.ObjectKey).IsRequired().HasMaxLength(1000);
        builder.Property(f => f.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(f => f.FileHash).IsRequired().HasMaxLength(128);
        builder.Property(f => f.ExtractedText).HasColumnType("nvarchar(max)");

        // Indexes
        builder.HasIndex(f => f.ProposalId);
        builder.HasIndex(f => f.ObjectKey).IsUnique();

        // Relationships
        builder.HasOne(f => f.Proposal)
            .WithMany(p => p.Files)
            .HasForeignKey(f => f.ProposalId)
            .OnDelete(DeleteBehavior.NoAction);

        // Soft delete filter
        builder.HasQueryFilter(f => !f.IsDeleted);
    }
}

// ===== ComplianceChecklist Configuration =====
public class ComplianceChecklistConfiguration : IEntityTypeConfiguration<ComplianceChecklist>
{
    public void Configure(EntityTypeBuilder<ComplianceChecklist> builder)
    {
        builder.ToTable("ComplianceChecklists");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.NameAr).IsRequired().HasMaxLength(500);
        builder.Property(c => c.NameEn).HasMaxLength(500);
        builder.Property(c => c.DescriptionAr).HasMaxLength(2000);
        builder.Property(c => c.DescriptionEn).HasMaxLength(2000);

        // Indexes
        builder.HasIndex(c => c.TenderId);
        builder.HasIndex(c => new { c.TenderId, c.OrderIndex });

        // Relationships
        builder.HasOne(c => c.Tender)
            .WithMany(t => t.ComplianceChecklists)
            .HasForeignKey(c => c.TenderId)
            .OnDelete(DeleteBehavior.NoAction);

        // Soft delete filter
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

// ===== ComplianceCheckResult Configuration =====
public class ComplianceCheckResultConfiguration : IEntityTypeConfiguration<ComplianceCheckResult>
{
    public void Configure(EntityTypeBuilder<ComplianceCheckResult> builder)
    {
        builder.ToTable("ComplianceCheckResults");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.FailureReason).HasMaxLength(2000);
        builder.Property(r => r.Notes).HasMaxLength(4000);

        // Unique constraint: one result per proposal per checklist item
        builder.HasIndex(r => new { r.ProposalId, r.ChecklistItemId }).IsUnique();

        // Indexes
        builder.HasIndex(r => r.ProposalId);
        builder.HasIndex(r => r.ChecklistItemId);
        builder.HasIndex(r => r.CheckedByUserId);

        // Relationships
        builder.HasOne(r => r.Proposal)
            .WithMany(p => p.ComplianceResults)
            .HasForeignKey(r => r.ProposalId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.ChecklistItem)
            .WithMany(c => c.Results)
            .HasForeignKey(r => r.ChecklistItemId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.CheckedByUser)
            .WithMany()
            .HasForeignKey(r => r.CheckedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Soft delete filter
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}

// ===== EvaluationScore Configuration =====
public class EvaluationScoreConfiguration : IEntityTypeConfiguration<EvaluationScore>
{
    public void Configure(EntityTypeBuilder<EvaluationScore> builder)
    {
        builder.ToTable("EvaluationScores");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Score).HasPrecision(10, 4);
        builder.Property(s => s.FinalizedScore).HasPrecision(10, 4);
        builder.Property(s => s.AiSuggestedScore).HasPrecision(10, 4);
        builder.Property(s => s.Justification).HasMaxLength(4000);
        builder.Property(s => s.FinalizationNotes).HasMaxLength(4000);
        builder.Property(s => s.AiJustification).HasColumnType("nvarchar(max)");

        // Unique constraint: one score per evaluator per proposal per criterion
        builder.HasIndex(s => new { s.ProposalId, s.CriteriaId, s.EvaluatorUserId }).IsUnique();

        // Indexes for blind evaluation filtering
        builder.HasIndex(s => s.ProposalId);
        builder.HasIndex(s => s.CriteriaId);
        builder.HasIndex(s => s.EvaluatorUserId);
        builder.HasIndex(s => new { s.ProposalId, s.EvaluatorUserId });

        // Relationships
        builder.HasOne(s => s.Proposal)
            .WithMany(p => p.EvaluationScores)
            .HasForeignKey(s => s.ProposalId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(s => s.Criteria)
            .WithMany()
            .HasForeignKey(s => s.CriteriaId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(s => s.EvaluatorUser)
            .WithMany()
            .HasForeignKey(s => s.EvaluatorUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Soft delete filter
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}

// ===== EvaluationReport Configuration =====
public class EvaluationReportConfiguration : IEntityTypeConfiguration<EvaluationReport>
{
    public void Configure(EntityTypeBuilder<EvaluationReport> builder)
    {
        builder.ToTable("EvaluationReports");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.TitleAr).IsRequired().HasMaxLength(500);
        builder.Property(r => r.TitleEn).HasMaxLength(500);
        builder.Property(r => r.ReferenceNumber).IsRequired().HasMaxLength(50);
        builder.Property(r => r.ContentHtml).HasColumnType("nvarchar(max)");
        builder.Property(r => r.ContentJson).HasColumnType("nvarchar(max)");
        builder.Property(r => r.PdfObjectKey).HasMaxLength(1000);
        builder.Property(r => r.PdfBucketName).HasMaxLength(100);
        builder.Property(r => r.AiAwardJustification).HasColumnType("nvarchar(max)");

        // Indexes
        builder.HasIndex(r => r.TenderId);
        builder.HasIndex(r => r.OrganizationId);
        builder.HasIndex(r => r.ReferenceNumber).IsUnique();
        builder.HasIndex(r => new { r.TenderId, r.ReportType });

        // Relationships
        builder.HasOne(r => r.Tender)
            .WithMany(t => t.EvaluationReports)
            .HasForeignKey(r => r.TenderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.Organization)
            .WithMany()
            .HasForeignKey(r => r.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);

        // Soft delete filter
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}

// ===== ReportSignature Configuration =====
public class ReportSignatureConfiguration : IEntityTypeConfiguration<ReportSignature>
{
    public void Configure(EntityTypeBuilder<ReportSignature> builder)
    {
        builder.ToTable("ReportSignatures");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Comments).HasMaxLength(4000);
        builder.Property(s => s.ContentHash).HasMaxLength(128);
        builder.Property(s => s.SignerIpAddress).HasMaxLength(45);

        // Unique constraint: one signature per user per report
        builder.HasIndex(s => new { s.EvaluationReportId, s.SignedByUserId }).IsUnique();

        // Indexes
        builder.HasIndex(s => s.EvaluationReportId);
        builder.HasIndex(s => s.SignedByUserId);

        // Relationships
        builder.HasOne(s => s.EvaluationReport)
            .WithMany(r => r.Signatures)
            .HasForeignKey(s => s.EvaluationReportId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(s => s.SignedByUser)
            .WithMany()
            .HasForeignKey(s => s.SignedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Soft delete filter
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
