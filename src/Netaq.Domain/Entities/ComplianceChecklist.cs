using Netaq.Domain.Common;

namespace Netaq.Domain.Entities;

/// <summary>
/// Represents a compliance checklist item for a tender.
/// Each tender has a customizable set of compliance requirements
/// that all proposals must satisfy during systematic inspection.
/// Default items include: preliminary guarantee, zakat certificate,
/// social insurance, commercial register, local content, contractor classification.
/// </summary>
public class ComplianceChecklist : BaseEntity
{
    /// <summary>
    /// Reference to the parent tender.
    /// </summary>
    public Guid TenderId { get; set; }

    /// <summary>
    /// Checklist item name in Arabic.
    /// </summary>
    public string NameAr { get; set; } = string.Empty;

    /// <summary>
    /// Checklist item name in English.
    /// </summary>
    public string NameEn { get; set; } = string.Empty;

    /// <summary>
    /// Description or guidance in Arabic.
    /// </summary>
    public string? DescriptionAr { get; set; }

    /// <summary>
    /// Description or guidance in English.
    /// </summary>
    public string? DescriptionEn { get; set; }

    /// <summary>
    /// Whether this item is mandatory for passing the compliance check.
    /// </summary>
    public bool IsMandatory { get; set; } = true;

    /// <summary>
    /// Display order in the checklist.
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// Whether this is a default system item or custom-added.
    /// </summary>
    public bool IsDefault { get; set; } = true;

    // Navigation properties
    public Tender Tender { get; set; } = null!;
    public ICollection<ComplianceCheckResult> Results { get; set; } = new List<ComplianceCheckResult>();
}
