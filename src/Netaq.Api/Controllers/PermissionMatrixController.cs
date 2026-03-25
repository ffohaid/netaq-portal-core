using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netaq.Application.Permissions.Commands;
using Netaq.Application.Permissions.Queries;
using Netaq.Domain.Enums;

namespace Netaq.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionMatrixController : ControllerBase
{
    private readonly IMediator _mediator;

    public PermissionMatrixController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get the full permission matrix grouped by role.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPermissionMatrix()
    {
        var result = await _mediator.Send(new GetPermissionMatrixQuery());
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Update permission matrix entries (batch update).
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdatePermissionMatrix([FromBody] UpdatePermissionMatrixCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Check if current user has a specific permission for a phase.
    /// </summary>
    [HttpGet("check")]
    public async Task<IActionResult> CheckPermission(
        [FromQuery] TenderPhase phase,
        [FromQuery] string permission)
    {
        var result = await _mediator.Send(new CheckPermissionQuery(phase, permission));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
