using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Represents a vendor proposal (offer) uploaded manually by the coordinator.
/// Each proposal belongs to a Tender. No direct vendor entity exists in Netaq;
/// vendor information is stored as metadata only.
/// </summary>
public class Proposal : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Reference to the parent tender/competition.
    /// </summary>
    public Guid TenderId { get; set; }

    /// <summary>
    /// Vendor/supplier name (entered manually by coordinator).
    /// </summary>
    public string VendorNameAr { get; set; } = string.Empty;

    /// <summary>
    /// Vendor/supplier name in English.
    /// </summary>
    public string VendorNameEn { get; set; } = string.Empty;

    /// <summary>
    /// Vendor reference number (from Etimad platform, without vendor name for blind evaluation).
    /// </summary>
    public string VendorReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Total financial value of the proposal in SAR.
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Current status of the proposal in the evaluation pipeline.
    /// </summary>
    public ProposalStatus Status { get; set; } = ProposalStatus.Received;

    /// <summary>
    /// Date when the proposal was received from Etimad.
    /// </summary>
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the proposal passed the systematic compliance check.
    /// Null means not yet checked.
    /// </summary>
    public bool? PassedComplianceCheck { get; set; }

    /// <summary>
    /// Reason for compliance check failure (if applicable).
    /// </summary>
    public string? ComplianceFailureReason { get; set; }

    /// <summary>
    /// Technical evaluation score (weighted, calculated after blind evaluation).
    /// </summary>
    public decimal? TechnicalScore { get; set; }

    /// <summary>
    /// Whether the proposal passed the technical evaluation threshold.
    /// </summary>
    public bool? PassedTechnicalEvaluation { get; set; }

    /// <summary>
    /// Financial evaluation score (calculated using the approved formula).
    /// </summary>
    public decimal? FinancialScore { get; set; }

    /// <summary>
    /// Final combined score (Technical + Financial).
    /// </summary>
    public decimal? FinalScore { get; set; }

    /// <summary>
    /// Final ranking among all proposals (1 = highest).
    /// </summary>
    public int? FinalRank { get; set; }

    /// <summary>
    /// AI-generated summary of the proposal (stored for committee review).
    /// </summary>
    public string? AiSummaryAr { get; set; }

    /// <summary>
    /// AI-generated summary in English.
    /// </summary>
    public string? AiSummaryEn { get; set; }

    /// <summary>
    /// AI-generated gap analysis result (JSON).
    /// </summary>
    public string? AiGapAnalysisJson { get; set; }

    /// <summary>
    /// AI-generated auto-mapping result (JSON).
    /// </summary>
    public string? AiAutoMappingJson { get; set; }

    /// <summary>
    /// Notes or comments from the coordinator.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Tender Tender { get; set; } = null!;
    public ICollection<ProposalFile> Files { get; set; } = new List<ProposalFile>();
    public ICollection<ComplianceCheckResult> ComplianceResults { get; set; } = new List<ComplianceCheckResult>();
    public ICollection<EvaluationScore> EvaluationScores { get; set; } = new List<EvaluationScore>();
}
