using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// 4-Dimensional permission matrix: Tender × Stage × Committee Role × User Role.
/// Stored in database, cached in Redis for fast authorization checks.
/// </summary>
public class PermissionMatrix : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    
    // 4 Dimensions
    public TenderPhase TenderPhase { get; set; }
    public OrganizationRole UserRole { get; set; }
    public string? CommitteeRole { get; set; } // Chair, Member, Secretary
    
    // Granular permissions
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
    public bool CanDelegate { get; set; }
    public bool CanExport { get; set; }
    
    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User User { get; set; } = null!;
}
