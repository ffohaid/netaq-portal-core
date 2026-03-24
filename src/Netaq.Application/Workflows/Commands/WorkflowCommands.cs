using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Models;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Workflows.Commands;

// --- DTOs ---
public record CreateWorkflowTemplateRequest(
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    List<CreateWorkflowStepRequest> Steps);

public record CreateWorkflowStepRequest(
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    int Order,
    WorkflowStepType StepType,
    OrganizationRole RequiredRole,
    Guid? AssignedUserId,
    int SlaDurationHours,
    string? ParallelGroupId,
    string? ConditionExpression,
    Guid? EscalationTargetUserId);

public record StartWorkflowRequest(
    Guid WorkflowTemplateId,
    Guid? EntityId,
    string? EntityType);

public record WorkflowActionRequest(
    Guid WorkflowInstanceId,
    WorkflowActionType ActionType,
    string? Justification,
    Guid? DelegatedToUserId,
    string? Notes);

// --- Create Workflow Template Command ---
public record CreateWorkflowTemplateCommand(
    Guid OrganizationId,
    Guid CreatedByUserId,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    List<CreateWorkflowStepRequest> Steps) : IRequest<ApiResponse<Guid>>;

public class CreateWorkflowTemplateCommandValidator : AbstractValidator<CreateWorkflowTemplateCommand>
{
    public CreateWorkflowTemplateCommandValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Steps).NotEmpty().WithMessage("Workflow must have at least one step.");
        RuleForEach(x => x.Steps).ChildRules(step =>
        {
            step.RuleFor(s => s.NameAr).NotEmpty();
            step.RuleFor(s => s.NameEn).NotEmpty();
            step.RuleFor(s => s.Order).GreaterThan(0);
            step.RuleFor(s => s.SlaDurationHours).GreaterThan(0);
        });
    }
}

public class CreateWorkflowTemplateCommandHandler : IRequestHandler<CreateWorkflowTemplateCommand, ApiResponse<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateWorkflowTemplateCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<Guid>> Handle(CreateWorkflowTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = new WorkflowTemplate
        {
            OrganizationId = request.OrganizationId,
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            DescriptionAr = request.DescriptionAr,
            DescriptionEn = request.DescriptionEn,
            IsActive = true,
            Version = 1,
            CreatedBy = request.CreatedByUserId
        };

        foreach (var stepReq in request.Steps.OrderBy(s => s.Order))
        {
            template.Steps.Add(new WorkflowStep
            {
                NameAr = stepReq.NameAr,
                NameEn = stepReq.NameEn,
                DescriptionAr = stepReq.DescriptionAr,
                DescriptionEn = stepReq.DescriptionEn,
                Order = stepReq.Order,
                StepType = stepReq.StepType,
                RequiredRole = stepReq.RequiredRole,
                AssignedUserId = stepReq.AssignedUserId,
                SlaDurationHours = stepReq.SlaDurationHours,
                ParallelGroupId = stepReq.ParallelGroupId,
                ConditionExpression = stepReq.ConditionExpression,
                EscalationTargetUserId = stepReq.EscalationTargetUserId,
                CreatedBy = request.CreatedByUserId
            });
        }

        _context.WorkflowTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<Guid>.Success(template.Id);
    }
}

// --- Start Workflow Instance Command ---
public record StartWorkflowCommand(
    Guid OrganizationId,
    Guid StartedByUserId,
    Guid WorkflowTemplateId,
    Guid? EntityId,
    string? EntityType) : IRequest<ApiResponse<Guid>>;

public class StartWorkflowCommandHandler : IRequestHandler<StartWorkflowCommand, ApiResponse<Guid>>
{
    private readonly IApplicationDbContext _context;

    public StartWorkflowCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<Guid>> Handle(StartWorkflowCommand request, CancellationToken cancellationToken)
    {
        var template = await _context.WorkflowTemplates
            .Include(t => t.Steps.OrderBy(s => s.Order))
            .FirstOrDefaultAsync(t => t.Id == request.WorkflowTemplateId && t.IsActive, cancellationToken);

        if (template == null)
            return ApiResponse<Guid>.Failure("Workflow template not found or inactive.");

        var firstStep = template.Steps.FirstOrDefault();
        if (firstStep == null)
            return ApiResponse<Guid>.Failure("Workflow template has no steps.");

        // Create workflow instance
        var instance = new WorkflowInstance
        {
            OrganizationId = request.OrganizationId,
            WorkflowTemplateId = request.WorkflowTemplateId,
            EntityId = request.EntityId,
            EntityType = request.EntityType,
            Status = WorkflowInstanceStatus.Active,
            CurrentStepId = firstStep.Id,
            CreatedBy = request.StartedByUserId
        };

        _context.WorkflowInstances.Add(instance);

        // Create SLA tracking for first step
        var slaTracking = new SlaTracking
        {
            WorkflowInstanceId = instance.Id,
            WorkflowStepId = firstStep.Id,
            StartedAt = DateTime.UtcNow,
            Deadline = DateTime.UtcNow.AddHours(firstStep.SlaDurationHours),
            Status = SlaStatus.OnTrack
        };
        _context.SlaTrackings.Add(slaTracking);

        // Create user task for the first step
        var assignedUserId = firstStep.AssignedUserId;
        if (!assignedUserId.HasValue)
        {
            // Find a user with the required role in the organization
            var assignee = await _context.Users
                .FirstOrDefaultAsync(u => u.OrganizationId == request.OrganizationId 
                    && u.Role == firstStep.RequiredRole 
                    && u.Status == UserStatus.Active, cancellationToken);
            assignedUserId = assignee?.Id;
        }

        if (assignedUserId.HasValue)
        {
            var task = new UserTask
            {
                OrganizationId = request.OrganizationId,
                AssignedUserId = assignedUserId.Value,
                WorkflowInstanceId = instance.Id,
                WorkflowStepId = firstStep.Id,
                TitleAr = firstStep.NameAr,
                TitleEn = firstStep.NameEn,
                DescriptionAr = firstStep.DescriptionAr,
                DescriptionEn = firstStep.DescriptionEn,
                Status = UserTaskStatus.Pending,
                Priority = TaskPriority.Medium,
                EntityId = request.EntityId,
                EntityType = request.EntityType,
                DueDate = slaTracking.Deadline,
                SlaStatus = SlaStatus.OnTrack,
                CreatedBy = request.StartedByUserId
            };
            _context.UserTasks.Add(task);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<Guid>.Success(instance.Id);
    }
}

// --- Take Workflow Action Command ---
public record TakeWorkflowActionCommand(
    Guid OrganizationId,
    Guid ActorUserId,
    Guid WorkflowInstanceId,
    WorkflowActionType ActionType,
    string? Justification,
    Guid? DelegatedToUserId,
    string? Notes) : IRequest<ApiResponse<bool>>;

public class TakeWorkflowActionCommandValidator : AbstractValidator<TakeWorkflowActionCommand>
{
    public TakeWorkflowActionCommandValidator()
    {
        RuleFor(x => x.WorkflowInstanceId).NotEmpty();
        RuleFor(x => x.ActionType).IsInEnum();
        RuleFor(x => x.Justification)
            .NotEmpty()
            .When(x => x.ActionType == WorkflowActionType.Reject)
            .WithMessage("Justification is mandatory for rejection.");
        RuleFor(x => x.DelegatedToUserId)
            .NotEmpty()
            .When(x => x.ActionType == WorkflowActionType.Delegate)
            .WithMessage("Delegated user is required for delegation.");
    }
}

public class TakeWorkflowActionCommandHandler : IRequestHandler<TakeWorkflowActionCommand, ApiResponse<bool>>
{
    private readonly IApplicationDbContext _context;

    public TakeWorkflowActionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<bool>> Handle(TakeWorkflowActionCommand request, CancellationToken cancellationToken)
    {
        var instance = await _context.WorkflowInstances
            .Include(i => i.WorkflowTemplate)
                .ThenInclude(t => t.Steps.OrderBy(s => s.Order))
            .FirstOrDefaultAsync(i => i.Id == request.WorkflowInstanceId && i.Status == WorkflowInstanceStatus.Active, cancellationToken);

        if (instance == null)
            return ApiResponse<bool>.Failure("Workflow instance not found or not active.");

        var currentStep = instance.WorkflowTemplate.Steps.FirstOrDefault(s => s.Id == instance.CurrentStepId);
        if (currentStep == null)
            return ApiResponse<bool>.Failure("Current workflow step not found.");

        // Record the action
        var action = new WorkflowAction
        {
            WorkflowInstanceId = instance.Id,
            WorkflowStepId = currentStep.Id,
            ActorUserId = request.ActorUserId,
            ActionType = request.ActionType,
            Justification = request.Justification,
            DelegatedToUserId = request.DelegatedToUserId,
            Notes = request.Notes,
            ActionDate = DateTime.UtcNow,
            CreatedBy = request.ActorUserId
        };
        _context.WorkflowActions.Add(action);

        // Update the current user task
        var currentTask = await _context.UserTasks
            .FirstOrDefaultAsync(t => t.WorkflowInstanceId == instance.Id 
                && t.WorkflowStepId == currentStep.Id 
                && t.Status == UserTaskStatus.Pending, cancellationToken);

        switch (request.ActionType)
        {
            case WorkflowActionType.Approve:
                if (currentTask != null)
                {
                    currentTask.Status = UserTaskStatus.Completed;
                    currentTask.CompletedAt = DateTime.UtcNow;
                }
                await AdvanceToNextStep(instance, currentStep, request, cancellationToken);
                break;

            case WorkflowActionType.Reject:
                if (currentTask != null)
                {
                    currentTask.Status = UserTaskStatus.Rejected;
                    currentTask.CompletedAt = DateTime.UtcNow;
                }
                instance.Status = WorkflowInstanceStatus.Rejected;
                break;

            case WorkflowActionType.Delegate:
                if (currentTask != null && request.DelegatedToUserId.HasValue)
                {
                    currentTask.Status = UserTaskStatus.Delegated;
                    currentTask.CompletedAt = DateTime.UtcNow;

                    // Create new task for delegated user
                    var delegatedTask = new UserTask
                    {
                        OrganizationId = request.OrganizationId,
                        AssignedUserId = request.DelegatedToUserId.Value,
                        WorkflowInstanceId = instance.Id,
                        WorkflowStepId = currentStep.Id,
                        TitleAr = currentStep.NameAr,
                        TitleEn = currentStep.NameEn,
                        DescriptionAr = currentStep.DescriptionAr,
                        DescriptionEn = currentStep.DescriptionEn,
                        Status = UserTaskStatus.Pending,
                        Priority = currentTask.Priority,
                        EntityId = instance.EntityId,
                        EntityType = instance.EntityType,
                        DueDate = currentTask.DueDate,
                        SlaStatus = currentTask.SlaStatus,
                        DelegatedFromUserId = request.ActorUserId,
                        CreatedBy = request.ActorUserId
                    };
                    _context.UserTasks.Add(delegatedTask);
                }
                break;

            case WorkflowActionType.ReturnForClarification:
                if (currentTask != null)
                {
                    currentTask.Status = UserTaskStatus.ReturnedForClarification;
                    currentTask.CompletedAt = DateTime.UtcNow;
                }
                // Return to previous step
                await ReturnToPreviousStep(instance, currentStep, request, cancellationToken);
                break;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<bool>.Success(true);
    }

    private async Task AdvanceToNextStep(
        WorkflowInstance instance,
        WorkflowStep currentStep,
        TakeWorkflowActionCommand request,
        CancellationToken cancellationToken)
    {
        var allSteps = instance.WorkflowTemplate.Steps.OrderBy(s => s.Order).ToList();
        var currentIndex = allSteps.FindIndex(s => s.Id == currentStep.Id);

        // Handle conditional steps
        if (currentStep.StepType == WorkflowStepType.Conditional)
        {
            // For now, use TrueNextStepId (condition evaluation to be enhanced)
            var nextStepId = currentStep.TrueNextStepId;
            if (nextStepId.HasValue)
            {
                var nextStep = allSteps.FirstOrDefault(s => s.Id == nextStepId.Value);
                if (nextStep != null)
                {
                    await CreateStepTaskAndSla(instance, nextStep, request, cancellationToken);
                    return;
                }
            }
        }

        // Handle parallel steps
        if (currentStep.StepType == WorkflowStepType.Parallel && !string.IsNullOrEmpty(currentStep.ParallelGroupId))
        {
            // Check if all parallel tasks in the group are completed
            var parallelSteps = allSteps.Where(s => s.ParallelGroupId == currentStep.ParallelGroupId).ToList();
            var completedActions = await _context.WorkflowActions
                .Where(a => a.WorkflowInstanceId == instance.Id 
                    && parallelSteps.Select(s => s.Id).Contains(a.WorkflowStepId)
                    && a.ActionType == WorkflowActionType.Approve)
                .ToListAsync(cancellationToken);

            if (completedActions.Count < parallelSteps.Count)
                return; // Wait for all parallel steps to complete (Join Node)
        }

        // Sequential: move to next step
        if (currentIndex + 1 < allSteps.Count)
        {
            var nextStep = allSteps[currentIndex + 1];
            
            // Handle parallel group: create tasks for all steps in the group
            if (nextStep.StepType == WorkflowStepType.Parallel && !string.IsNullOrEmpty(nextStep.ParallelGroupId))
            {
                var parallelSteps = allSteps.Where(s => s.ParallelGroupId == nextStep.ParallelGroupId).ToList();
                foreach (var pStep in parallelSteps)
                {
                    await CreateStepTaskAndSla(instance, pStep, request, cancellationToken);
                }
                instance.CurrentStepId = nextStep.Id;
            }
            else
            {
                await CreateStepTaskAndSla(instance, nextStep, request, cancellationToken);
            }
        }
        else
        {
            // Workflow completed
            instance.Status = WorkflowInstanceStatus.Completed;
            instance.CompletedAt = DateTime.UtcNow;
        }
    }

    private async Task CreateStepTaskAndSla(
        WorkflowInstance instance,
        WorkflowStep step,
        TakeWorkflowActionCommand request,
        CancellationToken cancellationToken)
    {
        instance.CurrentStepId = step.Id;

        // Create SLA tracking
        var sla = new SlaTracking
        {
            WorkflowInstanceId = instance.Id,
            WorkflowStepId = step.Id,
            StartedAt = DateTime.UtcNow,
            Deadline = DateTime.UtcNow.AddHours(step.SlaDurationHours),
            Status = SlaStatus.OnTrack
        };
        _context.SlaTrackings.Add(sla);

        // Find assignee
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
            var task = new UserTask
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
            _context.UserTasks.Add(task);
        }
    }

    private async Task ReturnToPreviousStep(
        WorkflowInstance instance,
        WorkflowStep currentStep,
        TakeWorkflowActionCommand request,
        CancellationToken cancellationToken)
    {
        var allSteps = instance.WorkflowTemplate.Steps.OrderBy(s => s.Order).ToList();
        var currentIndex = allSteps.FindIndex(s => s.Id == currentStep.Id);

        if (currentIndex > 0)
        {
            var previousStep = allSteps[currentIndex - 1];
            await CreateStepTaskAndSla(instance, previousStep, request, cancellationToken);
        }
    }
}
