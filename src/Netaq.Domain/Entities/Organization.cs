using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Represents a government entity (tenant) in the multi-tenant system.
/// Parent entity for users, workflows, and all tenant-scoped data.
/// </summary>
public class Organization : BaseEntity
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public bool ShowPlatformLogo { get; set; } = true;
    
    // Authentication settings
    public AuthProviderType ActiveAuthProvider { get; set; } = AuthProviderType.CustomAuth;
    public bool IsOtpEnabled { get; set; } = true;
    
    // SSO Configuration (encrypted)
    public string? SsoEndpoint { get; set; }
    public string? SsoClientId { get; set; }
    public string? SsoClientSecretEncrypted { get; set; }
    
    // AD Configuration
    public string? AdDomain { get; set; }
    public string? AdLdapUrl { get; set; }
    
    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<WorkflowTemplate> WorkflowTemplates { get; set; } = new List<WorkflowTemplate>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();
    public ICollection<AiConfiguration> AiConfigurations { get; set; } = new List<AiConfiguration>();
    public ICollection<PermissionMatrix> PermissionMatrices { get; set; } = new List<PermissionMatrix>();
    public ICollection<Tender> Tenders { get; set; } = new List<Tender>();
    public ICollection<Committee> Committees { get; set; } = new List<Committee>();
}
