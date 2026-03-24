using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Represents one of the 8 standard sections (doors/chapters) of a tender booklet.
/// Each section contains rich-text content that can be edited by the user.
/// </summary>
public class TenderSection : BaseEntity
{
    /// <summary>
    /// Reference to the parent tender.
    /// </summary>
    public Guid TenderId { get; set; }
    
    /// <summary>
    /// Section type (1 of 8 standard sections).
    /// </summary>
    public BookletSectionType SectionType { get; set; }
    
    /// <summary>
    /// Section title in Arabic.
    /// </summary>
    public string TitleAr { get; set; } = string.Empty;
    
    /// <summary>
    /// Section title in English.
    /// </summary>
    public string TitleEn { get; set; } = string.Empty;
    
    /// <summary>
    /// Rich HTML content of the section.
    /// </summary>
    public string? ContentHtml { get; set; }
    
    /// <summary>
    /// Completion percentage of this section (0-100).
    /// </summary>
    public int CompletionPercentage { get; set; } = 0;
    
    /// <summary>
    /// Display order index.
    /// </summary>
    public int OrderIndex { get; set; }
    
    /// <summary>
    /// Whether this section has been reviewed by AI for compliance.
    /// </summary>
    public bool IsAiReviewed { get; set; } = false;
    
    /// <summary>
    /// Last AI compliance check result (JSON).
    /// </summary>
    public string? AiComplianceResult { get; set; }
    
    /// <summary>
    /// Timestamp of last auto-save.
    /// </summary>
    public DateTime? LastAutoSavedAt { get; set; }
    
    // Navigation properties
    public Tender Tender { get; set; } = null!;
}
