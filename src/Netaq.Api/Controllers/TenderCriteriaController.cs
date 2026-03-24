using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netaq.Application.Tenders.Commands;

namespace Netaq.Api.Controllers;

[ApiController]
[Route("api/tenders/{tenderId:guid}/criteria")]
[Authorize]
public class TenderCriteriaController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenderCriteriaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Save complete criteria tree for a tender (replaces existing).
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> SaveCriteria(
        Guid tenderId,
        [FromBody] SaveTenderCriteriaCommand command)
    {
        if (tenderId != command.TenderId)
            return BadRequest("Tender ID mismatch.");

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Add a single criterion to a tender.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddCriterion(
        Guid tenderId,
        [FromBody] AddCriterionCommand command)
    {
        if (tenderId != command.TenderId)
            return BadRequest("Tender ID mismatch.");

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(result);
        return CreatedAtAction(null, result);
    }

    /// <summary>
    /// Delete a criterion and its children.
    /// </summary>
    [HttpDelete("{criterionId:guid}")]
    public async Task<IActionResult> DeleteCriterion(Guid tenderId, Guid criterionId)
    {
        var result = await _mediator.Send(new DeleteCriterionCommand(criterionId));
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
