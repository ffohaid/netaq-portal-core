using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Encrypted invitation for new users. Sent by System Admin via email.
/// No self-registration allowed.
/// </summary>
public class Invitation : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    
    public string Email { get; set; } = string.Empty;
    public string? FullNameAr { get; set; }
    public string? FullNameEn { get; set; }
    public OrganizationRole AssignedRole { get; set; } = OrganizationRole.Viewer;
    
    /// <summary>
    /// Encrypted invitation token sent via email.
    /// </summary>
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    
    public Guid? AcceptedByUserId { get; set; }
    public DateTime? AcceptedAt { get; set; }
    
    // Navigation properties
    public Organization Organization { get; set; } = null!;
}
