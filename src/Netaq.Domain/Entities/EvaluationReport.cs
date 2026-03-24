using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Represents a generated evaluation report (minutes/محضر).
/// Three types: Compliance Inspection, Technical Evaluation, Final (Technical + Financial).
/// Reports are generated as PDF and can be signed electronically by committee members.
/// </summary>
public class EvaluationReport : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Reference to the parent tender.
    /// </summary>
    public Guid TenderId { get; set; }

    /// <summary>
    /// Type of evaluation report.
    /// </summary>
    public EvaluationReportType ReportType { get; set; }

    /// <summary>
    /// Report title in Arabic.
    /// </summary>
    public string TitleAr { get; set; } = string.Empty;

    /// <summary>
    /// Report title in English.
    /// </summary>
    public string TitleEn { get; set; } = string.Empty;

    /// <summary>
    /// Auto-generated reference number for the report.
    /// </summary>
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Report content in HTML format (for rendering and PDF generation).
    /// </summary>
    public string ContentHtml { get; set; } = string.Empty;

    /// <summary>
    /// Report content in JSON format (structured data for programmatic access).
    /// </summary>
    public string? ContentJson { get; set; }

    /// <summary>
    /// Current status of the report.
    /// </summary>
    public EvaluationReportStatus Status { get; set; } = EvaluationReportStatus.Draft;

    /// <summary>
    /// MinIO object key for the generated PDF file.
    /// </summary>
    public string? PdfObjectKey { get; set; }

    /// <summary>
    /// MinIO bucket name for report storage.
    /// </summary>
    public string? PdfBucketName { get; set; }

    /// <summary>
    /// Date when the report was finalized.
    /// </summary>
    public DateTime? FinalizedAt { get; set; }

    /// <summary>
    /// User who finalized the report.
    /// </summary>
    public Guid? FinalizedBy { get; set; }

    /// <summary>
    /// AI-generated award justification draft (for final reports).
    /// </summary>
    public string? AiAwardJustification { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Tender Tender { get; set; } = null!;
    public ICollection<ReportSignature> Signatures { get; set; } = new List<ReportSignature>();
}
