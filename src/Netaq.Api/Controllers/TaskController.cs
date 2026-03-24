using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Application.Tasks.Queries;
using Netaq.Domain.Enums;

namespace Netaq.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TaskController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public TaskController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get current user's tasks (Unified Task Center).
    /// </summary>
    [HttpGet("my-tasks")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<UserTaskDto>>>> GetMyTasks(
        [FromQuery] UserTaskStatus? status = null,
        [FromQuery] TaskPriority? priority = null,
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
            pageSize);

        var result = await _mediator.Send(query);
        return Ok(result);
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
}
