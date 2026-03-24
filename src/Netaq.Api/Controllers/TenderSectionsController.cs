using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netaq.Application.Tenders.Commands;

namespace Netaq.Api.Controllers;

[ApiController]
[Route("api/tenders/{tenderId:guid}/sections")]
[Authorize]
public class TenderSectionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenderSectionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Update a single section content (auto-save endpoint).
    /// </summary>
    [HttpPut("{sectionId:guid}")]
    public async Task<IActionResult> UpdateSection(
        Guid tenderId,
        Guid sectionId,
        [FromBody] UpdateSectionContentCommand command)
    {
        if (sectionId != command.SectionId)
            return BadRequest("Section ID mismatch.");

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Batch update multiple sections at once.
    /// </summary>
    [HttpPut("batch")]
    public async Task<IActionResult> BatchUpdateSections(
        Guid tenderId,
        [FromBody] BatchUpdateSectionsCommand command)
    {
        if (tenderId != command.TenderId)
            return BadRequest("Tender ID mismatch.");

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
