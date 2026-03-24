using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Core entity representing a government tender/competition.
/// Each tender belongs to one Organization and follows a strict State Machine lifecycle.
/// </summary>
public class Tender : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    
    /// <summary>
    /// Tender title in Arabic.
    /// </summary>
    public string TitleAr { get; set; } = string.Empty;
    
    /// <summary>
    /// Tender title in English.
    /// </summary>
    public string TitleEn { get; set; } = string.Empty;
    
    /// <summary>
    /// Auto-generated reference number for the tender.
    /// </summary>
    public string ReferenceNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description in Arabic.
    /// </summary>
    public string? DescriptionAr { get; set; }
    
    /// <summary>
    /// Detailed description in English.
    /// </summary>
    public string? DescriptionEn { get; set; }
    
    /// <summary>
    /// Tender type (Supply, Services, Construction, IT, etc.).
    /// </summary>
    public TenderType TenderType { get; set; }
    
    /// <summary>
    /// Estimated value in SAR.
    /// </summary>
    public decimal EstimatedValue { get; set; }
    
    /// <summary>
    /// Current lifecycle status (State Machine).
    /// </summary>
    public TenderStatus Status { get; set; } = TenderStatus.Draft;
    
    /// <summary>
    /// Previous status before the current transition (for audit trail).
    /// </summary>
    public TenderStatus? PreviousStatus { get; set; }
    
    /// <summary>
    /// Method used to create this tender's booklet.
    /// </summary>
    public BookletCreationMethod CreationMethod { get; set; } = BookletCreationMethod.FromTemplate;
    
    /// <summary>
    /// Reference to the template used (if created from template).
    /// </summary>
    public Guid? BookletTemplateId { get; set; }
    
    /// <summary>
    /// Reference to the workflow instance for approval process.
    /// </summary>
    public Guid? WorkflowInstanceId { get; set; }
    
    /// <summary>
    /// Target date for submission opening.
    /// </summary>
    public DateTime? SubmissionOpenDate { get; set; }
    
    /// <summary>
    /// Target date for submission closing.
    /// </summary>
    public DateTime? SubmissionCloseDate { get; set; }
    
    /// <summary>
    /// Target date for project execution start.
    /// </summary>
    public DateTime? ProjectStartDate { get; set; }
    
    /// <summary>
    /// Target date for project execution end.
    /// </summary>
    public DateTime? ProjectEndDate { get; set; }
    
    /// <summary>
    /// Overall booklet completion percentage (calculated from sections).
    /// </summary>
    public int CompletionPercentage { get; set; } = 0;
    
    /// <summary>
    /// Technical evaluation weight (e.g., 60%).
    /// </summary>
    public decimal TechnicalWeight { get; set; } = 60;
    
    /// <summary>
    /// Financial evaluation weight (e.g., 40%).
    /// </summary>
    public decimal FinancialWeight { get; set; } = 40;
    
    /// <summary>
    /// Reason for cancellation (if cancelled).
    /// </summary>
    public string? CancellationReason { get; set; }
    
    /// <summary>
    /// Date when the tender was cancelled.
    /// </summary>
    public DateTime? CancelledAt { get; set; }
    
    /// <summary>
    /// User who cancelled the tender.
    /// </summary>
    public Guid? CancelledBy { get; set; }
    
    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public BookletTemplate? BookletTemplate { get; set; }
    public WorkflowInstance? WorkflowInstance { get; set; }
    public ICollection<TenderSection> Sections { get; set; } = new List<TenderSection>();
    public ICollection<TenderCriteria> Criteria { get; set; } = new List<TenderCriteria>();
    
    // Sprint 3 - Evaluation
    public ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();
    public ICollection<ComplianceChecklist> ComplianceChecklists { get; set; } = new List<ComplianceChecklist>();
    public ICollection<EvaluationReport> EvaluationReports { get; set; } = new List<EvaluationReport>();
    
    /// <summary>
    /// Whether the proposal receipt has been closed (no more uploads allowed).
    /// </summary>
    public bool IsReceiptClosed { get; set; } = false;
    
    /// <summary>
    /// Date when proposal receipt was closed.
    /// </summary>
    public DateTime? ReceiptClosedAt { get; set; }
    
    /// <summary>
    /// User who closed the proposal receipt.
    /// </summary>
    public Guid? ReceiptClosedBy { get; set; }
}
