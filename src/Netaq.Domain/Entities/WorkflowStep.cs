using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// A step within a workflow template. Supports sequential, parallel, and conditional execution.
/// </summary>
public class WorkflowStep : BaseEntity
{
    public Guid WorkflowTemplateId { get; set; }
    
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    
    public int Order { get; set; }
    public WorkflowStepType StepType { get; set; } = WorkflowStepType.Sequential;
    
    /// <summary>
    /// The role required to execute this step.
    /// </summary>
    public OrganizationRole RequiredRole { get; set; }
    
    /// <summary>
    /// Specific user assigned to this step (optional, overrides role).
    /// </summary>
    public Guid? AssignedUserId { get; set; }
    
    /// <summary>
    /// SLA duration in hours for this step.
    /// </summary>
    public int SlaDurationHours { get; set; } = 48;
    
    /// <summary>
    /// For parallel steps: the group identifier to synchronize at join node.
    /// </summary>
    public string? ParallelGroupId { get; set; }
    
    /// <summary>
    /// For conditional steps: the condition expression (e.g., "Tender.EstimatedValue > 5000000").
    /// </summary>
    public string? ConditionExpression { get; set; }
    
    /// <summary>
    /// For conditional steps: the step to go to if condition is true.
    /// </summary>
    public Guid? TrueNextStepId { get; set; }
    
    /// <summary>
    /// For conditional steps: the step to go to if condition is false.
    /// </summary>
    public Guid? FalseNextStepId { get; set; }
    
    /// <summary>
    /// Escalation target user ID when SLA is exceeded.
    /// </summary>
    public Guid? EscalationTargetUserId { get; set; }
    
    // Navigation properties
    public WorkflowTemplate WorkflowTemplate { get; set; } = null!;
    public User? AssignedUser { get; set; }
    public ICollection<SlaTracking> SlaTrackings { get; set; } = new List<SlaTracking>();
}
