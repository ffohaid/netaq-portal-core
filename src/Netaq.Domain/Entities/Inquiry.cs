using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Represents an inquiry/clarification request on a tender.
/// Supports the inquiry workflow: Submit → Review → Respond → Close.
/// </summary>
public class Inquiry : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Reference to the tender this inquiry is about.
    /// </summary>
    public Guid TenderId { get; set; }

    /// <summary>
    /// User who submitted the inquiry.
    /// </summary>
    public Guid SubmittedByUserId { get; set; }

    /// <summary>
    /// User assigned to respond to the inquiry.
    /// </summary>
    public Guid? AssignedToUserId { get; set; }

    /// <summary>
    /// Inquiry subject in Arabic.
    /// </summary>
    public string SubjectAr { get; set; } = string.Empty;

    /// <summary>
    /// Inquiry subject in English.
    /// </summary>
    public string SubjectEn { get; set; } = string.Empty;

    /// <summary>
    /// Inquiry body/question in Arabic.
    /// </summary>
    public string QuestionAr { get; set; } = string.Empty;

    /// <summary>
    /// Inquiry body/question in English.
    /// </summary>
    public string QuestionEn { get; set; } = string.Empty;

    /// <summary>
    /// Response/answer in Arabic.
    /// </summary>
    public string? ResponseAr { get; set; }

    /// <summary>
    /// Response/answer in English.
    /// </summary>
    public string? ResponseEn { get; set; }

    /// <summary>
    /// Current status of the inquiry.
    /// </summary>
    public InquiryStatus Status { get; set; } = InquiryStatus.Submitted;

    /// <summary>
    /// Priority level.
    /// </summary>
    public InquiryPriority Priority { get; set; } = InquiryPriority.Normal;

    /// <summary>
    /// Category/type of inquiry.
    /// </summary>
    public InquiryCategory Category { get; set; }

    /// <summary>
    /// Reference to a specific section of the tender booklet (optional).
    /// </summary>
    public Guid? TenderSectionId { get; set; }

    /// <summary>
    /// Deadline for responding.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// When the response was provided.
    /// </summary>
    public DateTime? RespondedAt { get; set; }

    /// <summary>
    /// When the inquiry was closed.
    /// </summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// Internal notes in Arabic (visible only to staff).
    /// </summary>
    public string? InternalNotesAr { get; set; }

    /// <summary>
    /// Internal notes in English (visible only to staff).
    /// </summary>
    public string? InternalNotesEn { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Tender Tender { get; set; } = null!;
    public User SubmittedByUser { get; set; } = null!;
    public User? AssignedToUser { get; set; }
    public TenderSection? TenderSection { get; set; }
}
