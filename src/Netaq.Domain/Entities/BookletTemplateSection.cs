using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Default section content within a booklet template.
/// When a tender is created from a template, these sections are copied
/// to create the initial TenderSection records.
/// </summary>
public class BookletTemplateSection : BaseEntity
{
    /// <summary>
    /// Reference to the parent template.
    /// </summary>
    public Guid BookletTemplateId { get; set; }
    
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
    /// Default rich HTML content for this section.
    /// </summary>
    public string? DefaultContentHtml { get; set; }
    
    /// <summary>
    /// Display order index.
    /// </summary>
    public int OrderIndex { get; set; }
    
    /// <summary>
    /// Guidance notes for the user filling this section.
    /// </summary>
    public string? GuidanceNotesAr { get; set; }
    
    /// <summary>
    /// Guidance notes in English.
    /// </summary>
    public string? GuidanceNotesEn { get; set; }
    
    // Navigation properties
    public BookletTemplate BookletTemplate { get; set; } = null!;
}
