using MediatR;
using Microsoft.AspNetCore.Mvc;
using Netaq.Application.Evaluation.Commands;
using Netaq.Application.Evaluation.Queries;
using Netaq.Domain.Enums;

namespace Netaq.Api.Controllers;

/// <summary>
/// API endpoints for tender evaluation operations.
/// Handles compliance checking, technical evaluation (blind),
/// financial evaluation, score finalization, and report generation.
/// </summary>
[ApiController]
[Route("api/tenders/{tenderId}/evaluation")]
public class EvaluationController : ControllerBase
{
    private readonly IMediator _mediator;

    public EvaluationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ===== Compliance Check =====

    /// <summary>
    /// Submit compliance check results for a proposal.
    /// </summary>
    [HttpPost("proposals/{proposalId}/compliance")]
    public async Task<IActionResult> SubmitComplianceCheck(
        Guid tenderId,
        Guid proposalId,
        [FromBody] SubmitComplianceCheckRequest request)
    {
        var result = await _mediator.Send(new SubmitComplianceCheckCommand(
            proposalId,
            request.Items.Select(i => new ComplianceCheckItemInput(
                i.ChecklistItemId, i.Passed, i.FailureReason, i.Notes
            )).ToList()
        ));

        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // ===== Technical Evaluation (Blind) =====

    /// <summary>
    /// Submit technical evaluation scores for a proposal.
    /// Each committee member submits independently (blind evaluation).
    /// API-level filtering ensures members only see their own scores.
    /// </summary>
    [HttpPost("proposals/{proposalId}/technical-scores")]
    public async Task<IActionResult> SubmitTechnicalScores(
        Guid tenderId,
        Guid proposalId,
        [FromBody] SubmitTechnicalScoreRequest request)
    {
        var result = await _mediator.Send(new SubmitTechnicalScoreCommand(
            proposalId,
            request.Scores.Select(s => new ScoreInput(s.CriteriaId, s.Score, s.Justification)).ToList()
        ));

        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Get current user's scores for a proposal (blind - only own scores).
    /// </summary>
    [HttpGet("proposals/{proposalId}/my-scores")]
    public async Task<IActionResult> GetMyScores(Guid tenderId, Guid proposalId)
    {
        var result = await _mediator.Send(new GetMyScoresQuery(proposalId));
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get evaluation summary for a proposal (chair only).
    /// Shows average scores, variance, and all member scores.
    /// </summary>
    [HttpGet("proposals/{proposalId}/summary")]
    public async Task<IActionResult> GetEvaluationSummary(Guid tenderId, Guid proposalId)
    {
        var result = await _mediator.Send(new GetEvaluationSummaryQuery(tenderId, proposalId));
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Finalize technical scores for a proposal (chair only).
    /// Chair unifies scores after reviewing all members' evaluations.
    /// </summary>
    [HttpPost("proposals/{proposalId}/finalize-technical")]
    public async Task<IActionResult> FinalizeTechnicalScores(
        Guid tenderId,
        Guid proposalId,
        [FromBody] FinalizeTechnicalScoresRequest request)
    {
        var result = await _mediator.Send(new FinalizeTechnicalScoresCommand(
            tenderId,
            proposalId,
            request.FinalizedScores.Select(s => new FinalizedScoreInput(
                s.CriteriaId, s.FinalScore, s.Notes
            )).ToList()
        ));

        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // ===== Financial Evaluation =====

    /// <summary>
    /// Submit financial evaluation for all technically passed proposals.
    /// Formula: Financial Score = (Lowest Financial / Current Financial) x Financial Weight
    /// </summary>
    [HttpPost("financial")]
    public async Task<IActionResult> SubmitFinancialEvaluation(
        Guid tenderId,
        [FromBody] SubmitFinancialEvaluationRequest request)
    {
        var result = await _mediator.Send(new SubmitFinancialEvaluationCommand(
            tenderId,
            request.FinancialValues.Select(f => new FinancialInput(f.ProposalId, f.FinancialValue)).ToList()
        ));

        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Get final rankings for a tender.
    /// </summary>
    [HttpGet("rankings")]
    public async Task<IActionResult> GetFinalRankings(Guid tenderId)
    {
        var result = await _mediator.Send(new GetFinalRankingsQuery(tenderId));
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ===== Reports =====

    /// <summary>
    /// Generate an evaluation report (compliance, technical, or final).
    /// </summary>
    [HttpPost("reports/generate")]
    public async Task<IActionResult> GenerateReport(
        Guid tenderId,
        [FromBody] GenerateReportRequest request)
    {
        if (!Enum.TryParse<EvaluationReportType>(request.ReportType, out var reportType))
            return BadRequest("Invalid report type.");

        var result = await _mediator.Send(new GenerateEvaluationReportCommand(tenderId, reportType));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Sign an evaluation report.
    /// </summary>
    [HttpPost("reports/{reportId}/sign")]
    public async Task<IActionResult> SignReport(
        Guid tenderId,
        Guid reportId,
        [FromBody] SignReportRequest request)
    {
        var result = await _mediator.Send(new SignReportCommand(reportId, request.Comments));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}

// ===== Request Models =====
public class SubmitComplianceCheckRequest
{
    public List<ComplianceCheckItemRequest> Items { get; set; } = new();
}

public class ComplianceCheckItemRequest
{
    public Guid ChecklistItemId { get; set; }
    public bool Passed { get; set; }
    public string? FailureReason { get; set; }
    public string? Notes { get; set; }
}

public class SubmitTechnicalScoreRequest
{
    public List<ScoreInputRequest> Scores { get; set; } = new();
}

public class ScoreInputRequest
{
    public Guid CriteriaId { get; set; }
    public decimal Score { get; set; }
    public string? Justification { get; set; }
}

public class FinalizeTechnicalScoresRequest
{
    public List<FinalizedScoreInputRequest> FinalizedScores { get; set; } = new();
}

public class FinalizedScoreInputRequest
{
    public Guid CriteriaId { get; set; }
    public decimal FinalScore { get; set; }
    public string? Notes { get; set; }
}

public class SubmitFinancialEvaluationRequest
{
    public List<FinancialInputRequest> FinancialValues { get; set; } = new();
}

public class FinancialInputRequest
{
    public Guid ProposalId { get; set; }
    public decimal FinancialValue { get; set; }
}

public class GenerateReportRequest
{
    public string ReportType { get; set; } = string.Empty;
}

public class SignReportRequest
{
    public string? Comments { get; set; }
}
