using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Internal government employee user. No vendor/external users allowed.
/// </summary>
public class User : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    
    public string FullNameAr { get; set; } = string.Empty;
    public string FullNameEn { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? JobTitleAr { get; set; }
    public string? JobTitleEn { get; set; }
    public string? DepartmentAr { get; set; }
    public string? DepartmentEn { get; set; }
    
    // Authentication
    public string? PasswordHash { get; set; }
    public string? PasswordSalt { get; set; }
    public string Locale { get; set; } = "ar";
    public UserStatus Status { get; set; } = UserStatus.Invited;
    public OrganizationRole Role { get; set; } = OrganizationRole.Viewer;
    
    // OTP
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiresAt { get; set; }
    
    // SSO
    public string? ExternalId { get; set; } // Nafath or AD identifier
    
    public DateTime? LastLoginAt { get; set; }
    public string? AvatarUrl { get; set; }
    
    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<UserTask> AssignedTasks { get; set; } = new List<UserTask>();
    public ICollection<WorkflowAction> WorkflowActions { get; set; } = new List<WorkflowAction>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<PermissionMatrix> PermissionMatrices { get; set; } = new List<PermissionMatrix>();
}
