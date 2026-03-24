using Netaq.Domain.Common;
using Netaq.Domain.Enums;

namespace Netaq.Domain.Entities;

/// <summary>
/// Tracks SLA deadlines for workflow steps. Monitored by SlaTrackingWorker every 5 minutes.
/// </summary>
public class SlaTracking : BaseEntity
{
    public Guid WorkflowInstanceId { get; set; }
    public Guid WorkflowStepId { get; set; }
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime Deadline { get; set; }
    public SlaStatus Status { get; set; } = SlaStatus.OnTrack;
    
    public bool IsEscalated { get; set; } = false;
    public DateTime? EscalatedAt { get; set; }
    public Guid? EscalatedToUserId { get; set; }
    
    // Navigation properties
    public WorkflowInstance WorkflowInstance { get; set; } = null!;
    public WorkflowStep WorkflowStep { get; set; } = null!;
}
