using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Records an action taken on a workflow step (approve, reject, delegate, etc.).
/// </summary>
public class WorkflowAction : BaseEntity
{
    public Guid WorkflowInstanceId { get; set; }
    public Guid WorkflowStepId { get; set; }
    public Guid ActorUserId { get; set; }
    
    public WorkflowActionType ActionType { get; set; }
    
    /// <summary>
    /// Mandatory justification for rejection.
    /// </summary>
    public string? Justification { get; set; }
    
    /// <summary>
    /// For delegation: the user being delegated to.
    /// </summary>
    public Guid? DelegatedToUserId { get; set; }
    
    /// <summary>
    /// Additional notes or comments.
    /// </summary>
    public string? Notes { get; set; }
    
    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public WorkflowInstance WorkflowInstance { get; set; } = null!;
    public WorkflowStep WorkflowStep { get; set; } = null!;
    public User ActorUser { get; set; } = null!;
    public User? DelegatedToUser { get; set; }
}
