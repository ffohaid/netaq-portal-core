using Microsoft.EntityFrameworkCore;
using Netaq.Api.Hubs;
using Netaq.Domain.Enums;
using Netaq.Infrastructure.Persistence;

namespace Netaq.Api.Workers;

/// <summary>
/// Background worker that monitors SLA deadlines every 5 minutes.
/// Updates SLA status (OnTrack → AtRisk → Overdue) and triggers escalation.
/// </summary>
public class SlaTrackingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlaTrackingWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public SlaTrackingWorker(IServiceScopeFactory scopeFactory, ILogger<SlaTrackingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SLA Tracking Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSlaTracking(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SLA Tracking Worker.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessSlaTracking(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;

        // Get active SLA trackings
        var activeSlaTrackings = await context.SlaTrackings
            .Include(s => s.WorkflowStep)
            .Include(s => s.WorkflowInstance)
            .Where(s => !s.IsDeleted && s.Status != SlaStatus.Overdue)
            .ToListAsync(cancellationToken);

        foreach (var sla in activeSlaTrackings)
        {
            var timeRemaining = sla.Deadline - now;
            var totalDuration = sla.Deadline - sla.StartedAt;
            var percentElapsed = totalDuration.TotalMinutes > 0 
                ? (1 - timeRemaining.TotalMinutes / totalDuration.TotalMinutes) * 100 
                : 100;

            SlaStatus newStatus;
            if (now >= sla.Deadline)
            {
                newStatus = SlaStatus.Overdue;
            }
            else if (percentElapsed >= 75) // 75% of time elapsed
            {
                newStatus = SlaStatus.AtRisk;
            }
            else
            {
                newStatus = SlaStatus.OnTrack;
            }

            if (newStatus != sla.Status)
            {
                sla.Status = newStatus;

                // Update corresponding user tasks
                var relatedTasks = await context.UserTasks
                    .Where(t => t.WorkflowInstanceId == sla.WorkflowInstanceId 
                        && t.WorkflowStepId == sla.WorkflowStepId 
                        && t.Status == UserTaskStatus.Pending)
                    .ToListAsync(cancellationToken);

                foreach (var task in relatedTasks)
                {
                    task.SlaStatus = newStatus;

                    // Send real-time notification
                    await notificationService.SendToUserAsync(task.AssignedUserId, "SlaStatusChanged", new
                    {
                        TaskId = task.Id,
                        NewStatus = newStatus.ToString(),
                        DueDate = sla.Deadline
                    });
                }

                // Handle escalation for overdue items
                if (newStatus == SlaStatus.Overdue && !sla.IsEscalated && sla.WorkflowStep?.EscalationTargetUserId != null)
                {
                    sla.IsEscalated = true;
                    sla.EscalatedAt = now;
                    sla.EscalatedToUserId = sla.WorkflowStep.EscalationTargetUserId;

                    // Notify escalation target
                    await notificationService.SendToUserAsync(
                        sla.WorkflowStep.EscalationTargetUserId.Value,
                        "TaskEscalated",
                        new
                        {
                            WorkflowInstanceId = sla.WorkflowInstanceId,
                            StepName = sla.WorkflowStep.NameEn,
                            Deadline = sla.Deadline
                        });

                    _logger.LogWarning("SLA escalated for WorkflowInstance {InstanceId}, Step {StepId}",
                        sla.WorkflowInstanceId, sla.WorkflowStepId);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
