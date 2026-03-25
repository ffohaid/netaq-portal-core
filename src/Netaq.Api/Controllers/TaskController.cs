using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Application.Tasks.Commands;
using Netaq.Application.Tasks.Queries;
using Netaq.Domain.Enums;
using Netaq.Infrastructure.Services;

namespace Netaq.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TaskController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditTrailService _auditTrailService;

    public TaskController(IMediator mediator, ICurrentUserService currentUser, IAuditTrailService auditTrailService)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _auditTrailService = auditTrailService;
    }

    /// <summary>
    /// Get current user's tasks (Unified Task Center).
    /// </summary>
    [HttpGet("my-tasks")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<UserTaskDto>>>> GetMyTasks(
        [FromQuery] UserTaskStatus? status = null,
        [FromQuery] TaskPriority? priority = null,
        [FromQuery] string? search = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var query = new GetMyTasksQuery(
            _currentUser.UserId.Value,
            status,
            priority,
            pageNumber,
            pageSize,
            search);

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get task details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TaskDetailDto>>> GetTaskDetail(Guid id)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var query = new GetTaskDetailQuery(id, _currentUser.UserId.Value);
        var result = await _mediator.Send(query);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Get task statistics for the current user.
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<TaskStatisticsDto>>> GetStatistics()
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var query = new GetTaskStatisticsQuery(_currentUser.UserId.Value);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Take action on a task (approve, reject, delegate, complete, etc.).
    /// </summary>
    [HttpPost("{id:guid}/action")]
    public async Task<ActionResult<ApiResponse<bool>>> TakeTaskAction(Guid id, [FromBody] TaskActionRequest request)
    {
        if (!_currentUser.OrganizationId.HasValue || !_currentUser.UserId.HasValue)
            return Unauthorized();

        var command = new TakeTaskActionCommand(
            id,
            _currentUser.OrganizationId.Value,
            _currentUser.UserId.Value,
            request.ActionType,
            request.Justification,
            request.DelegatedToUserId,
            request.Notes);

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            await _auditTrailService.LogAsync(
                _currentUser.OrganizationId.Value, _currentUser.UserId.Value,
                AuditActionCategory.TaskAction, $"TASK_{request.ActionType.ToString().ToUpper()}",
                $"Task action {request.ActionType} on task {id}",
                "UserTask", id,
                ipAddress: _currentUser.IpAddress,
                userAgent: _currentUser.UserAgent);
        }

        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Delegate a task to another user.
    /// </summary>
    [HttpPost("{id:guid}/delegate")]
    public async Task<ActionResult<ApiResponse<bool>>> DelegateTask(Guid id, [FromBody] DelegateTaskRequest request)
    {
        if (!_currentUser.OrganizationId.HasValue || !_currentUser.UserId.HasValue)
            return Unauthorized();

        var command = new TakeTaskActionCommand(
            id,
            _currentUser.OrganizationId.Value,
            _currentUser.UserId.Value,
            TaskActionType.Delegate,
            request.Justification,
            request.DelegatedToUserId,
            request.Notes);

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            await _auditTrailService.LogAsync(
                _currentUser.OrganizationId.Value, _currentUser.UserId.Value,
                AuditActionCategory.TaskAction, "TASK_DELEGATED",
                $"Task {id} delegated to user {request.DelegatedToUserId}",
                "UserTask", id,
                ipAddress: _currentUser.IpAddress,
                userAgent: _currentUser.UserAgent);
        }

        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}

public record TaskActionRequest(
    TaskActionType ActionType,
    string? Justification,
    Guid? DelegatedToUserId,
    string? Notes);

public record DelegateTaskRequest(
    Guid DelegatedToUserId,
    string? Justification,
    string? Notes);
