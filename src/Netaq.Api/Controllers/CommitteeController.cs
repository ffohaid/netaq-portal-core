using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netaq.Application.Committees.Commands;
using Netaq.Application.Committees.Queries;
using Netaq.Domain.Enums;

namespace Netaq.Api.Controllers;

[ApiController]
[Route("api/committees")]
[Authorize]
public class CommitteeController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommitteeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get paginated list of committees with optional filters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCommittees(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] CommitteeType? type = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null)
    {
        var result = await _mediator.Send(new GetCommitteesQuery(pageNumber, pageSize, type, isActive, search));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get committee details with members.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCommittee(Guid id)
    {
        var result = await _mediator.Send(new GetCommitteeDetailQuery(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Create a new committee (permanent or temporary).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCommittee([FromBody] CreateCommitteeCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? CreatedAtAction(nameof(GetCommittee), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    /// <summary>
    /// Update committee details.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCommittee(Guid id, [FromBody] UpdateCommitteeCommand command)
    {
        if (id != command.CommitteeId)
            return BadRequest("Committee ID mismatch.");

        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Dissolve (soft delete) a committee.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DissolveCommittee(Guid id)
    {
        var result = await _mediator.Send(new DissolveCommitteeCommand(id));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Add a member to a committee with a specific role.
    /// </summary>
    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddMemberRequest request)
    {
        var result = await _mediator.Send(new AddCommitteeMemberCommand(id, request.UserId, request.Role));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Remove a member from a committee.
    /// </summary>
    [HttpDelete("{id:guid}/members/{memberId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid memberId)
    {
        var result = await _mediator.Send(new RemoveCommitteeMemberCommand(id, memberId));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Change a member's role within a committee.
    /// </summary>
    [HttpPut("{id:guid}/members/{memberId:guid}/role")]
    public async Task<IActionResult> ChangeMemberRole(Guid id, Guid memberId, [FromBody] ChangeRoleRequest request)
    {
        var result = await _mediator.Send(new ChangeMemberRoleCommand(id, memberId, request.NewRole));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}

public record AddMemberRequest(Guid UserId, CommitteeMemberRole Role);
public record ChangeRoleRequest(CommitteeMemberRole NewRole);
