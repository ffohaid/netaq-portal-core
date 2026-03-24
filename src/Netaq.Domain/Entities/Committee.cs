using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Represents a committee formed for a specific tender.
/// Committees can be permanent (org-level) or temporary (tender-specific).
/// </summary>
public class Committee : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    
    /// <summary>
    /// Reference to the tender (null for permanent committees).
    /// </summary>
    public Guid? TenderId { get; set; }
    
    /// <summary>
    /// Committee name in Arabic.
    /// </summary>
    public string NameAr { get; set; } = string.Empty;
    
    /// <summary>
    /// Committee name in English.
    /// </summary>
    public string NameEn { get; set; } = string.Empty;
    
    /// <summary>
    /// Committee type (Permanent or Temporary).
    /// </summary>
    public CommitteeType Type { get; set; }
    
    /// <summary>
    /// Purpose/mandate of the committee.
    /// </summary>
    public string? PurposeAr { get; set; }
    
    /// <summary>
    /// Purpose in English.
    /// </summary>
    public string? PurposeEn { get; set; }
    
    /// <summary>
    /// Whether this committee is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Formation date.
    /// </summary>
    public DateTime? FormedAt { get; set; }
    
    /// <summary>
    /// Dissolution date.
    /// </summary>
    public DateTime? DissolvedAt { get; set; }
    
    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Tender? Tender { get; set; }
    public ICollection<CommitteeMember> Members { get; set; } = new List<CommitteeMember>();
}
