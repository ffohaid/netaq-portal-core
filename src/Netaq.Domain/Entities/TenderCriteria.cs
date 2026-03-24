using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Hierarchical evaluation criteria for a tender.
/// Supports parent-child relationships for nested criteria trees.
/// Technical and financial criteria are tracked separately.
/// </summary>
public class TenderCriteria : BaseEntity
{
    /// <summary>
    /// Reference to the parent tender.
    /// </summary>
    public Guid TenderId { get; set; }
    
    /// <summary>
    /// Reference to parent criteria for hierarchy (null = root level).
    /// </summary>
    public Guid? ParentId { get; set; }
    
    /// <summary>
    /// Criteria name in Arabic.
    /// </summary>
    public string NameAr { get; set; } = string.Empty;
    
    /// <summary>
    /// Criteria name in English.
    /// </summary>
    public string NameEn { get; set; } = string.Empty;
    
    /// <summary>
    /// Description in Arabic.
    /// </summary>
    public string? DescriptionAr { get; set; }
    
    /// <summary>
    /// Description in English.
    /// </summary>
    public string? DescriptionEn { get; set; }
    
    /// <summary>
    /// Whether this is a technical or financial criterion.
    /// </summary>
    public CriteriaType CriteriaType { get; set; }
    
    /// <summary>
    /// Weight percentage of this criterion (relative to its siblings).
    /// Sum of all root-level criteria of the same type must equal 100.
    /// </summary>
    public decimal Weight { get; set; }
    
    /// <summary>
    /// Minimum passing threshold percentage.
    /// </summary>
    public decimal? PassingThreshold { get; set; }
    
    /// <summary>
    /// Display order among siblings.
    /// </summary>
    public int OrderIndex { get; set; }
    
    /// <summary>
    /// Whether this criterion was suggested by AI.
    /// </summary>
    public bool IsAiSuggested { get; set; } = false;
    
    // Navigation properties
    public Tender Tender { get; set; } = null!;
    public TenderCriteria? Parent { get; set; }
    public ICollection<TenderCriteria> Children { get; set; } = new List<TenderCriteria>();
}
