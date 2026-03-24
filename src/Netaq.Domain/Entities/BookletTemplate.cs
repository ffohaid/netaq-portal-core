using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Approved booklet template. Global templates have null OrganizationId.
/// Organization-specific templates belong to a single tenant.
/// Covers 21 template types across 7 categories.
/// </summary>
public class BookletTemplate : BaseEntity
{
    /// <summary>
    /// Null for global (system-wide) templates, set for organization-specific templates.
    /// </summary>
    public Guid? OrganizationId { get; set; }
    
    /// <summary>
    /// Template name in Arabic.
    /// </summary>
    public string NameAr { get; set; } = string.Empty;
    
    /// <summary>
    /// Template name in English.
    /// </summary>
    public string NameEn { get; set; } = string.Empty;
    
    /// <summary>
    /// Template category (Supply, Services, Consulting, etc.).
    /// </summary>
    public TemplateCategory Category { get; set; }
    
    /// <summary>
    /// Applicable tender type for this template.
    /// </summary>
    public TenderType ApplicableTenderType { get; set; }
    
    /// <summary>
    /// Description in Arabic.
    /// </summary>
    public string? DescriptionAr { get; set; }
    
    /// <summary>
    /// Description in English.
    /// </summary>
    public string? DescriptionEn { get; set; }
    
    /// <summary>
    /// Whether this template is currently active and available for use.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Template version string.
    /// </summary>
    public string Version { get; set; } = "1.0";
    
    // Navigation properties
    public Organization? Organization { get; set; }
    public ICollection<BookletTemplateSection> Sections { get; set; } = new List<BookletTemplateSection>();
    public ICollection<Tender> Tenders { get; set; } = new List<Tender>();
}
