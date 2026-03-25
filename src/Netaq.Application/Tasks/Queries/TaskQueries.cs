using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Models;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Tasks.Queries;

// --- DTOs ---
public record UserTaskDto(
    Guid Id,
    string TitleAr,
    string TitleEn,
    string? DescriptionAr,
    string? DescriptionEn,
    UserTaskStatus Status,
    TaskPriority Priority,
    SlaStatus SlaStatus,
    DateTime DueDate,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    Guid? EntityId,
    string? EntityType,
    Guid WorkflowInstanceId,
    Guid WorkflowStepId,
    string? AssignedUserNameAr,
    string? AssignedUserNameEn,
    Guid? DelegatedFromUserId,
    string? DelegatedFromUserNameAr,
    string? DelegatedFromUserNameEn);

public record TaskDetailDto(
    Guid Id,
    string TitleAr,
    string TitleEn,
    string? DescriptionAr,
    string? DescriptionEn,
    UserTaskStatus Status,
    TaskPriority Priority,
    SlaStatus SlaStatus,
    DateTime DueDate,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    Guid? EntityId,
    string? EntityType,
    Guid WorkflowInstanceId,
    Guid WorkflowStepId,
    string? AssignedUserNameAr,
    string? AssignedUserNameEn,
    Guid? DelegatedFromUserId,
    string? DelegatedFromUserNameAr,
    string? DelegatedFromUserNameEn,
    string? WorkflowNameAr,
    string? WorkflowNameEn,
    string? StepNameAr,
    string? StepNameEn,
    List<TaskActionHistoryDto> ActionHistory);

public record TaskActionHistoryDto(
    Guid Id,
    WorkflowActionType ActionType,
    string? Justification,
    string? Notes,
    DateTime ActionDate,
    string? ActorNameAr,
    string? ActorNameEn,
    string? DelegatedToNameAr,
    string? DelegatedToNameEn);

public record TaskStatisticsDto(
    int TotalTasks,
    int PendingTasks,
    int InProgressTasks,
    int CompletedTasks,
    int OverdueTasks,
    int EscalatedTasks,
    int DelegatedTasks);

// --- Get My Tasks Query ---
public record GetMyTasksQuery(
    Guid UserId,
    UserTaskStatus? StatusFilter = null,
    TaskPriority? PriorityFilter = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null) : IRequest<ApiResponse<PaginatedResponse<UserTaskDto>>>;

public class GetMyTasksQueryHandler : IRequestHandler<GetMyTasksQuery, ApiResponse<PaginatedResponse<UserTaskDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetMyTasksQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedResponse<UserTaskDto>>> Handle(GetMyTasksQuery request, CancellationToken cancellationToken)
    {
        var query = _context.UserTasks
            .Include(t => t.AssignedUser)
            .Where(t => t.AssignedUserId == request.UserId);

        if (request.StatusFilter.HasValue)
            query = query.Where(t => t.Status == request.StatusFilter.Value);

        if (request.PriorityFilter.HasValue)
            query = query.Where(t => t.Priority == request.PriorityFilter.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(t =>
                t.TitleAr.ToLower().Contains(search) ||
                t.TitleEn.ToLower().Contains(search) ||
                (t.DescriptionAr != null && t.DescriptionAr.ToLower().Contains(search)) ||
                (t.DescriptionEn != null && t.DescriptionEn.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var tasks = await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new UserTaskDto(
                t.Id,
                t.TitleAr,
                t.TitleEn,
                t.DescriptionAr,
                t.DescriptionEn,
                t.Status,
                t.Priority,
                t.SlaStatus,
                t.DueDate,
                t.CreatedAt,
                t.CompletedAt,
                t.EntityId,
                t.EntityType,
                t.WorkflowInstanceId,
                t.WorkflowStepId,
                t.AssignedUser.FullNameAr,
                t.AssignedUser.FullNameEn,
                t.DelegatedFromUserId,
                null, // DelegatedFromUserNameAr - loaded separately if needed
                null  // DelegatedFromUserNameEn
            ))
            .ToListAsync(cancellationToken);

        return ApiResponse<PaginatedResponse<UserTaskDto>>.Success(new PaginatedResponse<UserTaskDto>
        {
            Items = tasks,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        });
    }
}

// --- Get Task Detail Query ---
public record GetTaskDetailQuery(Guid TaskId, Guid UserId) : IRequest<ApiResponse<TaskDetailDto>>;

public class GetTaskDetailQueryHandler : IRequestHandler<GetTaskDetailQuery, ApiResponse<TaskDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTaskDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<TaskDetailDto>> Handle(GetTaskDetailQuery request, CancellationToken cancellationToken)
    {
        var task = await _context.UserTasks
            .Include(t => t.AssignedUser)
            .Include(t => t.WorkflowInstance)
                .ThenInclude(i => i.WorkflowTemplate)
            .Include(t => t.WorkflowStep)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task == null)
            return ApiResponse<TaskDetailDto>.Failure("Task not found.");

        // Get action history for this workflow instance
        var actionHistory = await _context.WorkflowActions
            .Include(a => a.ActorUser)
            .Include(a => a.DelegatedToUser)
            .Where(a => a.WorkflowInstanceId == task.WorkflowInstanceId)
            .OrderByDescending(a => a.ActionDate)
            .Select(a => new TaskActionHistoryDto(
                a.Id,
                a.ActionType,
                a.Justification,
                a.Notes,
                a.ActionDate,
                a.ActorUser.FullNameAr,
                a.ActorUser.FullNameEn,
                a.DelegatedToUser != null ? a.DelegatedToUser.FullNameAr : null,
                a.DelegatedToUser != null ? a.DelegatedToUser.FullNameEn : null))
            .ToListAsync(cancellationToken);

        // Get delegated from user name
        string? delegatedFromNameAr = null;
        string? delegatedFromNameEn = null;
        if (task.DelegatedFromUserId.HasValue)
        {
            var delegatedFrom = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == task.DelegatedFromUserId.Value, cancellationToken);
            delegatedFromNameAr = delegatedFrom?.FullNameAr;
            delegatedFromNameEn = delegatedFrom?.FullNameEn;
        }

        var detail = new TaskDetailDto(
            task.Id,
            task.TitleAr,
            task.TitleEn,
            task.DescriptionAr,
            task.DescriptionEn,
            task.Status,
            task.Priority,
            task.SlaStatus,
            task.DueDate,
            task.CreatedAt,
            task.CompletedAt,
            task.EntityId,
            task.EntityType,
            task.WorkflowInstanceId,
            task.WorkflowStepId,
            task.AssignedUser.FullNameAr,
            task.AssignedUser.FullNameEn,
            task.DelegatedFromUserId,
            delegatedFromNameAr,
            delegatedFromNameEn,
            task.WorkflowInstance?.WorkflowTemplate?.NameAr,
            task.WorkflowInstance?.WorkflowTemplate?.NameEn,
            task.WorkflowStep?.NameAr,
            task.WorkflowStep?.NameEn,
            actionHistory);

        return ApiResponse<TaskDetailDto>.Success(detail);
    }
}

// --- Get Task Statistics Query ---
public record GetTaskStatisticsQuery(Guid UserId) : IRequest<ApiResponse<TaskStatisticsDto>>;

public class GetTaskStatisticsQueryHandler : IRequestHandler<GetTaskStatisticsQuery, ApiResponse<TaskStatisticsDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTaskStatisticsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<TaskStatisticsDto>> Handle(GetTaskStatisticsQuery request, CancellationToken cancellationToken)
    {
        var tasks = await _context.UserTasks
            .Where(t => t.AssignedUserId == request.UserId)
            .ToListAsync(cancellationToken);

        var stats = new TaskStatisticsDto(
            TotalTasks: tasks.Count,
            PendingTasks: tasks.Count(t => t.Status == UserTaskStatus.Pending),
            InProgressTasks: tasks.Count(t => t.Status == UserTaskStatus.InProgress),
            CompletedTasks: tasks.Count(t => t.Status == UserTaskStatus.Completed),
            OverdueTasks: tasks.Count(t => t.SlaStatus == SlaStatus.Overdue),
            EscalatedTasks: tasks.Count(t => t.Status == UserTaskStatus.Escalated),
            DelegatedTasks: tasks.Count(t => t.Status == UserTaskStatus.Delegated));

        return ApiResponse<TaskStatisticsDto>.Success(stats);
    }
}
