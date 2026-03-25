using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Models;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Tasks.Commands;

/// <summary>
/// Task action types available in the Unified Task Center.
/// </summary>
public enum TaskActionType
{
    Approve = 1,
    Reject = 2,
    Complete = 3,
    Delegate = 4,
    Escalate = 5,
    ReturnForClarification = 6
}

// --- Take Task Action Command ---
public record TakeTaskActionCommand(
    Guid TaskId,
    Guid OrganizationId,
    Guid ActorUserId,
    TaskActionType ActionType,
    string? Justification,
    Guid? DelegatedToUserId,
    string? Notes) : IRequest<ApiResponse<bool>>;

public class TakeTaskActionCommandValidator : AbstractValidator<TakeTaskActionCommand>
{
    public TakeTaskActionCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.ActionType).IsInEnum();
        RuleFor(x => x.Justification)
            .NotEmpty()
            .When(x => x.ActionType == TaskActionType.Reject)
            .WithMessage("Justification is mandatory for rejection.");
        RuleFor(x => x.DelegatedToUserId)
            .NotEmpty()
            .When(x => x.ActionType == TaskActionType.Delegate)
            .WithMessage("Delegated user is required for delegation.");
    }
}

public class TakeTaskActionCommandHandler : IRequestHandler<TakeTaskActionCommand, ApiResponse<bool>>
{
    private readonly IApplicationDbContext _context;

    public TakeTaskActionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<bool>> Handle(TakeTaskActionCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.UserTasks
            .Include(t => t.WorkflowInstance)
                .ThenInclude(i => i.WorkflowTemplate)
                    .ThenInclude(t => t.Steps.OrderBy(s => s.Order))
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task == null)
            return ApiResponse<bool>.Failure("Task not found.");

        if (task.AssignedUserId != request.ActorUserId)
            return ApiResponse<bool>.Failure("You are not assigned to this task.");

        if (task.Status != UserTaskStatus.Pending && task.Status != UserTaskStatus.InProgress)
            return ApiResponse<bool>.Failure("Task is not in a state that allows actions.");

        switch (request.ActionType)
        {
            case TaskActionType.Approve:
                return await HandleApprove(task, request, cancellationToken);

            case TaskActionType.Reject:
                return await HandleReject(task, request, cancellationToken);

            case TaskActionType.Complete:
                return await HandleComplete(task, request, cancellationToken);

            case TaskActionType.Delegate:
                return await HandleDelegate(task, request, cancellationToken);

            case TaskActionType.Escalate:
                return await HandleEscalate(task, request, cancellationToken);

            case TaskActionType.ReturnForClarification:
                return await HandleReturnForClarification(task, request, cancellationToken);

            default:
                return ApiResponse<bool>.Failure("Unknown action type.");
        }
    }

    private async Task<ApiResponse<bool>> HandleApprove(UserTask task, TakeTaskActionCommand request, CancellationToken cancellationToken)
    {
        task.Status = UserTaskStatus.Completed;
        task.CompletedAt = DateTime.UtcNow;

        // Record workflow action
        await RecordWorkflowAction(task, WorkflowActionType.Approve, request, cancellationToken);

        // Advance workflow to next step
        await AdvanceWorkflow(task, request, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<bool>.Success(true);
    }

    private async Task<ApiResponse<bool>> HandleReject(UserTask task, TakeTaskActionCommand request, CancellationToken cancellationToken)
    {
        task.Status = UserTaskStatus.Rejected;
        task.CompletedAt = DateTime.UtcNow;

        // Record workflow action
        await RecordWorkflowAction(task, WorkflowActionType.Reject, request, cancellationToken);

        // Reject workflow instance
        if (task.WorkflowInstance != null)
        {
            task.WorkflowInstance.Status = WorkflowInstanceStatus.Rejected;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<bool>.Success(true);
    }

    private async Task<ApiResponse<bool>> HandleComplete(UserTask task, TakeTaskActionCommand request, CancellationToken cancellationToken)
    {
        task.Status = UserTaskStatus.Completed;
        task.CompletedAt = DateTime.UtcNow;

        // Record workflow action as Approve (completing = approving current step)
        await RecordWorkflowAction(task, WorkflowActionType.Approve, request, cancellationToken);

        // Advance workflow
        await AdvanceWorkflow(task, request, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<bool>.Success(true);
    }

    private async Task<ApiResponse<bool>> HandleDelegate(UserTask task, TakeTaskActionCommand request, CancellationToken cancellationToken)
    {
        if (!request.DelegatedToUserId.HasValue)
            return ApiResponse<bool>.Failure("Delegated user is required.");

        // Verify delegated user exists and is active
        var delegatedUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.DelegatedToUserId.Value
                && u.OrganizationId == request.OrganizationId
                && u.Status == UserStatus.Active, cancellationToken);

        if (delegatedUser == null)
            return ApiResponse<bool>.Failure("Delegated user not found or inactive.");

        task.Status = UserTaskStatus.Delegated;
        task.CompletedAt = DateTime.UtcNow;

        // Create new task for delegated user
        var delegatedTask = new UserTask
        {
            OrganizationId = task.OrganizationId,
            AssignedUserId = request.DelegatedToUserId.Value,
            WorkflowInstanceId = task.WorkflowInstanceId,
            WorkflowStepId = task.WorkflowStepId,
            TitleAr = task.TitleAr,
            TitleEn = task.TitleEn,
            DescriptionAr = task.DescriptionAr,
            DescriptionEn = task.DescriptionEn,
            Status = UserTaskStatus.Pending,
            Priority = task.Priority,
            EntityId = task.EntityId,
            EntityType = task.EntityType,
            DueDate = task.DueDate,
            SlaStatus = task.SlaStatus,
            DelegatedFromUserId = request.ActorUserId,
            CreatedBy = request.ActorUserId
        };
        _context.UserTasks.Add(delegatedTask);

        // Record workflow action
        await RecordWorkflowAction(task, WorkflowActionType.Delegate, request, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<bool>.Success(true);
    }

    private async Task<ApiResponse<bool>> HandleEscalate(UserTask task, TakeTaskActionCommand request, CancellationToken cancellationToken)
    {
        task.Status = UserTaskStatus.Escalated;

        // Find escalation target from workflow step
        var step = task.WorkflowInstance?.WorkflowTemplate?.Steps
            .FirstOrDefault(s => s.Id == task.WorkflowStepId);

        if (step?.EscalationTargetUserId != null)
        {
            // Create escalated task for the escalation target
            var escalatedTask = new UserTask
            {
                OrganizationId = task.OrganizationId,
                AssignedUserId = step.EscalationTargetUserId.Value,
                WorkflowInstanceId = task.WorkflowInstanceId,
                WorkflowStepId = task.WorkflowStepId,
                TitleAr = $"[تصعيد] {task.TitleAr}",
                TitleEn = $"[Escalated] {task.TitleEn}",
                DescriptionAr = task.DescriptionAr,
                DescriptionEn = task.DescriptionEn,
                Status = UserTaskStatus.Pending,
                Priority = TaskPriority.Critical,
                EntityId = task.EntityId,
                EntityType = task.EntityType,
                DueDate = task.DueDate,
                SlaStatus = SlaStatus.Overdue,
                DelegatedFromUserId = request.ActorUserId,
                CreatedBy = request.ActorUserId
            };
            _context.UserTasks.Add(escalatedTask);
        }

        await RecordWorkflowAction(task, WorkflowActionType.Escalate, request, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<bool>.Success(true);
    }

    private async Task<ApiResponse<bool>> HandleReturnForClarification(UserTask task, TakeTaskActionCommand request, CancellationToken cancellationToken)
    {
        task.Status = UserTaskStatus.ReturnedForClarification;
        task.CompletedAt = DateTime.UtcNow;

        await RecordWorkflowAction(task, WorkflowActionType.ReturnForClarification, request, cancellationToken);

        // Return to previous step
        if (task.WorkflowInstance != null)
        {
            var allSteps = task.WorkflowInstance.WorkflowTemplate.Steps.OrderBy(s => s.Order).ToList();
            var currentIndex = allSteps.FindIndex(s => s.Id == task.WorkflowStepId);

            if (currentIndex > 0)
            {
                var previousStep = allSteps[currentIndex - 1];
                task.WorkflowInstance.CurrentStepId = previousStep.Id;

                // Create SLA tracking for previous step
                var sla = new SlaTracking
                {
                    WorkflowInstanceId = task.WorkflowInstanceId,
                    WorkflowStepId = previousStep.Id,
                    StartedAt = DateTime.UtcNow,
                    Deadline = DateTime.UtcNow.AddHours(previousStep.SlaDurationHours),
                    Status = SlaStatus.OnTrack
                };
                _context.SlaTrackings.Add(sla);

                // Find assignee for previous step
                var assignedUserId = previousStep.AssignedUserId;
                if (!assignedUserId.HasValue)
                {
                    var assignee = await _context.Users
                        .FirstOrDefaultAsync(u => u.OrganizationId == request.OrganizationId
                            && u.Role == previousStep.RequiredRole
                            && u.Status == UserStatus.Active, cancellationToken);
                    assignedUserId = assignee?.Id;
                }

                if (assignedUserId.HasValue)
                {
                    var returnTask = new UserTask
                    {
                        OrganizationId = request.OrganizationId,
                        AssignedUserId = assignedUserId.Value,
                        WorkflowInstanceId = task.WorkflowInstanceId,
                        WorkflowStepId = previousStep.Id,
                        TitleAr = $"[إعادة للتوضيح] {previousStep.NameAr}",
                        TitleEn = $"[Returned] {previousStep.NameEn}",
                        DescriptionAr = request.Notes ?? previousStep.DescriptionAr,
                        DescriptionEn = request.Notes ?? previousStep.DescriptionEn,
                        Status = UserTaskStatus.Pending,
                        Priority = TaskPriority.High,
                        EntityId = task.EntityId,
                        EntityType = task.EntityType,
                        DueDate = sla.Deadline,
                        SlaStatus = SlaStatus.OnTrack,
                        CreatedBy = request.ActorUserId
                    };
                    _context.UserTasks.Add(returnTask);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<bool>.Success(true);
    }

    private async Task RecordWorkflowAction(UserTask task, WorkflowActionType actionType, TakeTaskActionCommand request, CancellationToken cancellationToken)
    {
        var action = new WorkflowAction
        {
            WorkflowInstanceId = task.WorkflowInstanceId,
            WorkflowStepId = task.WorkflowStepId,
            ActorUserId = request.ActorUserId,
            ActionType = actionType,
            Justification = request.Justification,
            DelegatedToUserId = request.DelegatedToUserId,
            Notes = request.Notes,
            ActionDate = DateTime.UtcNow,
            CreatedBy = request.ActorUserId
        };
        _context.WorkflowActions.Add(action);
    }

    private async Task AdvanceWorkflow(UserTask task, TakeTaskActionCommand request, CancellationToken cancellationToken)
    {
        if (task.WorkflowInstance == null) return;

        var allSteps = task.WorkflowInstance.WorkflowTemplate.Steps.OrderBy(s => s.Order).ToList();
        var currentStep = allSteps.FirstOrDefault(s => s.Id == task.WorkflowStepId);
        if (currentStep == null) return;

        var currentIndex = allSteps.FindIndex(s => s.Id == currentStep.Id);

        // Handle conditional steps
        if (currentStep.StepType == WorkflowStepType.Conditional && currentStep.TrueNextStepId.HasValue)
        {
            var nextStep = allSteps.FirstOrDefault(s => s.Id == currentStep.TrueNextStepId.Value);
            if (nextStep != null)
            {
                await CreateStepTaskAndSla(task.WorkflowInstance, nextStep, request, cancellationToken);
                return;
            }
        }

        // Handle parallel steps - check if all in group are done
        if (currentStep.StepType == WorkflowStepType.Parallel && !string.IsNullOrEmpty(currentStep.ParallelGroupId))
        {
            var parallelSteps = allSteps.Where(s => s.ParallelGroupId == currentStep.ParallelGroupId).ToList();
            var completedActions = await _context.WorkflowActions
                .Where(a => a.WorkflowInstanceId == task.WorkflowInstanceId
                    && parallelSteps.Select(s => s.Id).Contains(a.WorkflowStepId)
                    && a.ActionType == WorkflowActionType.Approve)
                .ToListAsync(cancellationToken);

            if (completedActions.Count < parallelSteps.Count)
                return; // Wait for all parallel steps
        }

        // Sequential: move to next step
        if (currentIndex + 1 < allSteps.Count)
        {
            var nextStep = allSteps[currentIndex + 1];

            if (nextStep.StepType == WorkflowStepType.Parallel && !string.IsNullOrEmpty(nextStep.ParallelGroupId))
            {
                var parallelSteps = allSteps.Where(s => s.ParallelGroupId == nextStep.ParallelGroupId).ToList();
                foreach (var pStep in parallelSteps)
                {
                    await CreateStepTaskAndSla(task.WorkflowInstance, pStep, request, cancellationToken);
                }
                task.WorkflowInstance.CurrentStepId = nextStep.Id;
            }
            else
            {
                await CreateStepTaskAndSla(task.WorkflowInstance, nextStep, request, cancellationToken);
            }
        }
        else
        {
            // Workflow completed
            task.WorkflowInstance.Status = WorkflowInstanceStatus.Completed;
            task.WorkflowInstance.CompletedAt = DateTime.UtcNow;
        }
    }

    private async Task CreateStepTaskAndSla(WorkflowInstance instance, WorkflowStep step, TakeTaskActionCommand request, CancellationToken cancellationToken)
    {
        instance.CurrentStepId = step.Id;

        var sla = new SlaTracking
        {
            WorkflowInstanceId = instance.Id,
            WorkflowStepId = step.Id,
            StartedAt = DateTime.UtcNow,
            Deadline = DateTime.UtcNow.AddHours(step.SlaDurationHours),
            Status = SlaStatus.OnTrack
        };
        _context.SlaTrackings.Add(sla);

        var assignedUserId = step.AssignedUserId;
        if (!assignedUserId.HasValue)
        {
            var assignee = await _context.Users
                .FirstOrDefaultAsync(u => u.OrganizationId == request.OrganizationId
                    && u.Role == step.RequiredRole
                    && u.Status == UserStatus.Active, cancellationToken);
            assignedUserId = assignee?.Id;
        }

        if (assignedUserId.HasValue)
        {
            var newTask = new UserTask
            {
                OrganizationId = request.OrganizationId,
                AssignedUserId = assignedUserId.Value,
                WorkflowInstanceId = instance.Id,
                WorkflowStepId = step.Id,
                TitleAr = step.NameAr,
                TitleEn = step.NameEn,
                DescriptionAr = step.DescriptionAr,
                DescriptionEn = step.DescriptionEn,
                Status = UserTaskStatus.Pending,
                Priority = TaskPriority.Medium,
                EntityId = instance.EntityId,
                EntityType = instance.EntityType,
                DueDate = sla.Deadline,
                SlaStatus = SlaStatus.OnTrack,
                CreatedBy = request.ActorUserId
            };
            _context.UserTasks.Add(newTask);
        }
    }
}
