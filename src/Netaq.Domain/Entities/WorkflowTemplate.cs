using Netaq.Domain.Common;

namespace Netaq.Domain.Entities;

/// <summary>
/// Dynamic workflow template supporting sequential, parallel, and conditional approval paths.
/// Configured by System Admin per organization.
/// </summary>
public class WorkflowTemplate : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    
    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
    public ICollection<WorkflowInstance> Instances { get; set; } = new List<WorkflowInstance>();
}
