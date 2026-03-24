using MediatR;
using Microsoft.AspNetCore.Mvc;
using Netaq.Application.Ai.Services;

namespace Netaq.Api.Controllers;

/// <summary>
/// API endpoints for AI-powered evaluation features.
/// Provides: summarization, auto-mapping, gap analysis,
/// score suggestions, comparison matrix, and award justification.
/// </summary>
[ApiController]
[Route("api/ai/evaluation")]
public class AiEvaluationController : ControllerBase
{
    private readonly IMediator _mediator;

    public AiEvaluationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Extract text from all proposal files (PDF to text + OCR).
    /// </summary>
    [HttpPost("proposals/{proposalId}/extract-text")]
    public async Task<IActionResult> ExtractText(Guid proposalId)
    {
        var result = await _mediator.Send(new ExtractDocumentTextCommand(proposalId));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// AI summarization of a proposal's technical content.
    /// </summary>
    [HttpPost("proposals/{proposalId}/summarize")]
    public async Task<IActionResult> SummarizeProposal(Guid proposalId)
    {
        var result = await _mediator.Send(new SummarizeProposalCommand(proposalId));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Auto-map proposal content to booklet requirements.
    /// </summary>
    [HttpPost("proposals/{proposalId}/auto-map")]
    public async Task<IActionResult> AutoMapRequirements(Guid proposalId)
    {
        var result = await _mediator.Send(new Application.Ai.Services.SummarizeProposalCommand(proposalId));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Analyze gaps between proposal and booklet requirements.
    /// </summary>
    [HttpPost("proposals/{proposalId}/gap-analysis")]
    public async Task<IActionResult> AnalyzeGaps(Guid proposalId)
    {
        var result = await _mediator.Send(new AnalyzeGapsCommand(proposalId));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// AI-suggested scores with justifications for each criterion.
    /// Scores are advisory only and can be accepted or modified by evaluators.
    /// </summary>
    [HttpPost("proposals/{proposalId}/suggest-scores")]
    public async Task<IActionResult> SuggestScores(Guid proposalId)
    {
        var result = await _mediator.Send(new SuggestScoresCommand(proposalId));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Generate comparison matrix between all eligible proposals.
    /// </summary>
    [HttpPost("tenders/{tenderId}/comparison-matrix")]
    public async Task<IActionResult> GenerateComparisonMatrix(Guid tenderId)
    {
        var result = await _mediator.Send(new GenerateComparisonMatrixCommand(tenderId));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Generate award justification draft for the recommended proposal.
    /// </summary>
    [HttpPost("tenders/{tenderId}/award-justification")]
    public async Task<IActionResult> GenerateAwardJustification(Guid tenderId)
    {
        var result = await _mediator.Send(new GenerateAwardJustificationCommand(tenderId));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
