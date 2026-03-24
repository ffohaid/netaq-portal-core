using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Represents a member of a committee with a specific role.
/// </summary>
public class CommitteeMember : BaseEntity
{
    /// <summary>
    /// Reference to the committee.
    /// </summary>
    public Guid CommitteeId { get; set; }
    
    /// <summary>
    /// Reference to the user.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Role within the committee.
    /// </summary>
    public CommitteeMemberRole Role { get; set; }
    
    /// <summary>
    /// Whether this member is currently active in the committee.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Date the member joined the committee.
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Date the member left the committee.
    /// </summary>
    public DateTime? LeftAt { get; set; }
    
    // Navigation properties
    public Committee Committee { get; set; } = null!;
    public User User { get; set; } = null!;
}
