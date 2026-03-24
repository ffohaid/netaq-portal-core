using Netaq.Domain.Common;

namespace Netaq.Domain.Entities;

/// <summary>
/// Records the result of a single compliance checklist item for a specific proposal.
/// Each proposal is checked against all checklist items of its tender.
/// </summary>
public class ComplianceCheckResult : BaseEntity
{
    /// <summary>
    /// Reference to the proposal being checked.
    /// </summary>
    public Guid ProposalId { get; set; }

    /// <summary>
    /// Reference to the checklist item being evaluated.
    /// </summary>
    public Guid ChecklistItemId { get; set; }

    /// <summary>
    /// Whether the proposal passed this checklist item.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Detailed reason for failure (mandatory when Passed = false).
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Additional notes from the inspector.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// User who performed this check.
    /// </summary>
    public Guid CheckedByUserId { get; set; }

    /// <summary>
    /// Timestamp when the check was performed.
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Proposal Proposal { get; set; } = null!;
    public ComplianceChecklist ChecklistItem { get; set; } = null!;
    public User CheckedByUser { get; set; } = null!;
}
