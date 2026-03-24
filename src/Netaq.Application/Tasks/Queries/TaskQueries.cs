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
    string? AssignedUserNameEn);

public record TaskStatisticsDto(
    int TotalTasks,
    int PendingTasks,
    int CompletedTasks,
    int OverdueTasks,
    int EscalatedTasks);

// --- Get My Tasks Query ---
public record GetMyTasksQuery(
    Guid UserId,
    UserTaskStatus? StatusFilter = null,
    TaskPriority? PriorityFilter = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<ApiResponse<PaginatedResponse<UserTaskDto>>>;

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
                t.AssignedUser.FullNameEn))
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
            CompletedTasks: tasks.Count(t => t.Status == UserTaskStatus.Completed),
            OverdueTasks: tasks.Count(t => t.SlaStatus == SlaStatus.Overdue),
            EscalatedTasks: tasks.Count(t => t.Status == UserTaskStatus.Escalated));

        return ApiResponse<TaskStatisticsDto>.Success(stats);
    }
}
