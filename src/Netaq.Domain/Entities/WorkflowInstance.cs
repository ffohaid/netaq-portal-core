using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// An active copy of a workflow template for a specific tender or entity.
/// Tracks the current state of the approval path.
/// </summary>
public class WorkflowInstance : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid WorkflowTemplateId { get; set; }
    
    /// <summary>
    /// The entity this workflow is attached to (e.g., Tender ID).
    /// </summary>
    public Guid? EntityId { get; set; }
    
    /// <summary>
    /// The type of entity (e.g., "Tender", "Document").
    /// </summary>
    public string? EntityType { get; set; }
    
    public WorkflowInstanceStatus Status { get; set; } = WorkflowInstanceStatus.Active;
    
    /// <summary>
    /// The current step being executed.
    /// </summary>
    public Guid? CurrentStepId { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public WorkflowTemplate WorkflowTemplate { get; set; } = null!;
    public WorkflowStep? CurrentStep { get; set; }
    public ICollection<WorkflowAction> Actions { get; set; } = new List<WorkflowAction>();
    public ICollection<UserTask> Tasks { get; set; } = new List<UserTask>();
    public ICollection<SlaTracking> SlaTrackings { get; set; } = new List<SlaTracking>();
}
