using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Immutable audit trail with cryptographic hash chain.
/// Hash = SHA-256(CurrentEntryData + PreviousEntryHash + UserIdentity + Timestamp)
/// Cannot be modified or deleted by anyone, including DB admins.
/// </summary>
public class AuditLog : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid? UserId { get; set; }
    
    public AuditActionCategory ActionCategory { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ActionDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// The entity affected by this action.
    /// </summary>
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    
    /// <summary>
    /// JSON snapshot of old values (before change).
    /// </summary>
    public string? OldValues { get; set; }
    
    /// <summary>
    /// JSON snapshot of new values (after change).
    /// </summary>
    public string? NewValues { get; set; }
    
    /// <summary>
    /// Client IP address.
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// User agent string.
    /// </summary>
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// Cryptographic timestamp for non-repudiation.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// SHA-256 hash of this entry for chain integrity.
    /// Hash(CurrentEntryData + PreviousEntryHash + UserIdentity + Timestamp)
    /// </summary>
    public string Hash { get; set; } = string.Empty;
    
    /// <summary>
    /// Hash of the previous entry in the chain.
    /// </summary>
    public string PreviousHash { get; set; } = string.Empty;
    
    /// <summary>
    /// Sequential number for ordering and chain verification.
    /// </summary>
    public long SequenceNumber { get; set; }
    
    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User? User { get; set; }
}
