using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netaq.Application.Tenders.Commands;
using Netaq.Application.Tenders.Queries;
using Netaq.Domain.Enums;

namespace Netaq.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TendersController : ControllerBase
{
    private readonly IMediator _mediator;

    public TendersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get paginated list of tenders with optional filters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTenders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] TenderStatus? status = null,
        [FromQuery] TenderType? type = null,
        [FromQuery] string? search = null)
    {
        var query = new GetTendersQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            StatusFilter = status,
            TypeFilter = type,
            SearchTerm = search
        };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get tender details by ID including sections and criteria.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTenderById(Guid id)
    {
        var result = await _mediator.Send(new GetTenderByIdQuery(id));
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    /// <summary>
    /// Create a new tender (from template, AI extraction, or manual entry).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTender([FromBody] CreateTenderCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetTenderById), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// Update tender basic information (only in Draft status).
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTender(Guid id, [FromBody] UpdateTenderCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch.");
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Submit tender for approval workflow.
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> SubmitForApproval(Guid id)
    {
        var result = await _mediator.Send(new SubmitTenderForApprovalCommand(id));
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Cancel a tender with reason.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelTender(Guid id, [FromBody] CancelTenderRequest request)
    {
        var result = await _mediator.Send(new CancelTenderCommand(id, request.Reason));
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}

public record CancelTenderRequest(string Reason);
