using MediatR;
using Microsoft.AspNetCore.Mvc;
using Netaq.Application.Proposals.Commands;
using Netaq.Application.Proposals.Queries;
using Netaq.Domain.Enums;
using Netaq.Infrastructure.Storage;

namespace Netaq.Api.Controllers;

/// <summary>
/// API endpoints for proposal (offer) management.
/// Handles manual upload of vendor proposals from Etimad platform.
/// </summary>
[ApiController]
[Route("api/tenders/{tenderId}/proposals")]
public class ProposalsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFileStorageService _fileStorage;

    public ProposalsController(IMediator mediator, IFileStorageService fileStorage)
    {
        _mediator = mediator;
        _fileStorage = fileStorage;
    }

    /// <summary>
    /// Get all proposals for a tender with optional filtering and pagination.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProposals(
        Guid tenderId,
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetProposalsByTenderQuery(tenderId, status, pageNumber, pageSize));
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get proposal details by ID.
    /// </summary>
    [HttpGet("{proposalId}")]
    public async Task<IActionResult> GetProposal(Guid tenderId, Guid proposalId)
    {
        var result = await _mediator.Send(new GetProposalDetailQuery(proposalId));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>
    /// Create a new proposal (manual upload by coordinator).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateProposal(Guid tenderId, [FromBody] CreateProposalRequest request)
    {
        if (tenderId != request.TenderId)
            return BadRequest("Tender ID mismatch.");

        var result = await _mediator.Send(new CreateProposalCommand(
            request.TenderId,
            request.VendorNameAr,
            request.VendorNameEn,
            request.VendorReferenceNumber,
            request.TotalValue,
            request.ReceivedDate,
            request.Notes
        ));

        if (!result.Success) return BadRequest(result);
        return CreatedAtAction(nameof(GetProposal), new { tenderId, proposalId = result.Data!.Id }, result);
    }

    /// <summary>
    /// Upload a file to a proposal (supports up to 100MB PDF files).
    /// Files are stored in MinIO with AES-256 encryption.
    /// </summary>
    [HttpPost("{proposalId}/files")]
    [RequestSizeLimit(104_857_600)] // 100MB
    public async Task<IActionResult> UploadFile(
        Guid tenderId,
        Guid proposalId,
        IFormFile file,
        [FromQuery] ProposalFileCategory category = ProposalFileCategory.TechnicalOffer)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided.");

        if (file.Length > 100 * 1024 * 1024)
            return BadRequest("File size exceeds 100MB limit.");

        // Upload to storage
        using var stream = file.OpenReadStream();
        var uploadResult = await _fileStorage.UploadFileAsync(
            stream, file.FileName, "proposals", file.ContentType);

        var result = await _mediator.Send(new UploadProposalFileCommand(
            proposalId,
            file.FileName,
            file.ContentType,
            file.Length,
            uploadResult.ObjectKey,
            uploadResult.StoredFileName,
            uploadResult.BucketName,
            uploadResult.FileHash,
            category
        ));

        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Close proposal receipt for a tender (no more uploads allowed).
    /// Transitions tender to EvaluationInProgress status.
    /// </summary>
    [HttpPost("close-receipt")]
    public async Task<IActionResult> CloseReceipt(Guid tenderId)
    {
        var result = await _mediator.Send(new CloseProposalReceiptCommand(tenderId));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Get compliance checklist for a tender.
    /// </summary>
    [HttpGet("compliance-checklist")]
    public async Task<IActionResult> GetComplianceChecklist(Guid tenderId)
    {
        var result = await _mediator.Send(new GetComplianceChecklistQuery(tenderId));
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

public class CreateProposalRequest
{
    public Guid TenderId { get; set; }
    public string VendorNameAr { get; set; } = string.Empty;
    public string VendorNameEn { get; set; } = string.Empty;
    public string VendorReferenceNumber { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public string? Notes { get; set; }
}
