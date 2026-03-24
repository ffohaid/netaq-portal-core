using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netaq.Application.Templates.Queries;
using Netaq.Domain.Enums;

namespace Netaq.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public TemplatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get list of booklet templates with optional filters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTemplates(
        [FromQuery] TemplateCategory? category = null,
        [FromQuery] TenderType? tenderType = null,
        [FromQuery] bool activeOnly = true)
    {
        var query = new GetTemplatesQuery
        {
            CategoryFilter = category,
            TenderTypeFilter = tenderType,
            ActiveOnly = activeOnly
        };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get template details by ID including sections.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTemplateById(Guid id)
    {
        var result = await _mediator.Send(new GetTemplateByIdQuery(id));
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }
}
