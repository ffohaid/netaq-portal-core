using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Represents an internal electronic signature on an evaluation report.
/// Committee members approve/sign reports through the system.
/// </summary>
public class ReportSignature : BaseEntity
{
    /// <summary>
    /// Reference to the evaluation report.
    /// </summary>
    public Guid EvaluationReportId { get; set; }

    /// <summary>
    /// Reference to the signing user (committee member).
    /// </summary>
    public Guid SignedByUserId { get; set; }

    /// <summary>
    /// Role of the signer in the committee.
    /// </summary>
    public CommitteeMemberRole SignerRole { get; set; }

    /// <summary>
    /// Whether the user has signed/approved.
    /// </summary>
    public bool IsSigned { get; set; } = false;

    /// <summary>
    /// Timestamp of the signature.
    /// </summary>
    public DateTime? SignedAt { get; set; }

    /// <summary>
    /// Optional comments from the signer.
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// Cryptographic hash of the report content at the time of signing.
    /// Ensures the signed version matches the current version.
    /// </summary>
    public string? ContentHash { get; set; }

    /// <summary>
    /// IP address of the signer for audit trail.
    /// </summary>
    public string? SignerIpAddress { get; set; }

    // Navigation properties
    public EvaluationReport EvaluationReport { get; set; } = null!;
    public User SignedByUser { get; set; } = null!;
}
