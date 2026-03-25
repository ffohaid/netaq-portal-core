using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netaq.Application.Inquiries.Commands;
using Netaq.Application.Inquiries.Queries;
using Netaq.Domain.Enums;

namespace Netaq.Api.Controllers;

[ApiController]
[Route("api/inquiries")]
[Authorize]
public class InquiryController : ControllerBase
{
    private readonly IMediator _mediator;

    public InquiryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get paginated list of inquiries with optional filters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetInquiries(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? tenderId = null,
        [FromQuery] InquiryStatus? status = null,
        [FromQuery] InquiryCategory? category = null,
        [FromQuery] InquiryPriority? priority = null,
        [FromQuery] string? search = null)
    {
        var result = await _mediator.Send(new GetInquiriesQuery(pageNumber, pageSize, tenderId, status, category, search));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get inquiry details.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetInquiry(Guid id)
    {
        var result = await _mediator.Send(new GetInquiryDetailQuery(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Get inquiry statistics.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] Guid? tenderId = null)
    {
        var result = await _mediator.Send(new GetInquiryStatsQuery(tenderId));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Create a new inquiry for a tender.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateInquiry([FromBody] CreateInquiryCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? CreatedAtAction(nameof(GetInquiry), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    /// <summary>
    /// Respond to an inquiry (draft response, pending approval).
    /// </summary>
    [HttpPut("{id:guid}/respond")]
    public async Task<IActionResult> RespondToInquiry(Guid id, [FromBody] RespondRequest request)
    {
        var result = await _mediator.Send(new RespondToInquiryCommand(id, request.ResponseAr, request.ResponseEn));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Close an inquiry.
    /// </summary>
    [HttpPut("{id:guid}/close")]
    public async Task<IActionResult> CloseInquiry(Guid id)
    {
        var result = await _mediator.Send(new CloseInquiryCommand(id));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Assign an inquiry to a user for review.
    /// </summary>
    [HttpPut("{id:guid}/assign")]
    public async Task<IActionResult> AssignInquiry(Guid id, [FromBody] AssignRequest request)
    {
        var result = await _mediator.Send(new AssignInquiryCommand(id, request.AssignedToUserId));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Escalate an inquiry to a higher authority.
    /// </summary>
    [HttpPut("{id:guid}/escalate")]
    public async Task<IActionResult> EscalateInquiry(Guid id, [FromBody] EscalateRequest request)
    {
        var result = await _mediator.Send(new EscalateInquiryCommand(id, request.EscalatedToUserId, request.Reason));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Reopen a closed inquiry.
    /// </summary>
    [HttpPut("{id:guid}/reopen")]
    public async Task<IActionResult> ReopenInquiry(Guid id)
    {
        var result = await _mediator.Send(new ReopenInquiryCommand(id));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Add an internal note to an inquiry (visible only to staff).
    /// </summary>
    [HttpPost("{id:guid}/notes")]
    public async Task<IActionResult> AddInternalNote(Guid id, [FromBody] AddNoteRequest request)
    {
        var result = await _mediator.Send(new AddInquiryNoteCommand(id, request.NoteAr, request.NoteEn));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Export inquiries as CSV for a specific tender or all.
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportInquiries(
        [FromQuery] Guid? tenderId = null,
        [FromQuery] InquiryStatus? status = null)
    {
        var result = await _mediator.Send(new ExportInquiriesQuery(tenderId, status));
        if (!result.IsSuccess)
            return BadRequest(result);
        return File(result.Data!, "text/csv", $"inquiries_export_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}

public record RespondRequest(string ResponseAr, string ResponseEn);
public record AssignRequest(Guid AssignedToUserId);
public record EscalateRequest(Guid EscalatedToUserId, string Reason);
public record AddNoteRequest(string NoteAr, string NoteEn);
