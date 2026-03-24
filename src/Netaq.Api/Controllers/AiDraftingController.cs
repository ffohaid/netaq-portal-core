using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netaq.Application.Ai.Commands;
using Netaq.Domain.Enums;

namespace Netaq.Api.Controllers;

[ApiController]
[Route("api/ai/drafting")]
[Authorize]
public class AiDraftingController : ControllerBase
{
    private readonly IMediator _mediator;

    public AiDraftingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// AI-powered criteria suggestion based on tender type and description.
    /// </summary>
    [HttpPost("suggest-criteria")]
    public async Task<IActionResult> SuggestCriteria([FromBody] SuggestCriteriaCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// AI-powered legal compliance check for tender booklet.
    /// </summary>
    [HttpPost("compliance-check")]
    public async Task<IActionResult> CheckCompliance([FromBody] CheckLegalComplianceCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// AI-powered boilerplate content generation for a specific section.
    /// </summary>
    [HttpPost("generate-boilerplate")]
    public async Task<IActionResult> GenerateBoilerplate([FromBody] GenerateBoilerplateCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }
}
