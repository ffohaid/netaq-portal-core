using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Evaluation.Commands;

// ===== DTOs =====
public class ComplianceCheckDto
{
    public Guid ProposalId { get; set; }
    public string VendorNameAr { get; set; } = string.Empty;
    public string VendorReferenceNumber { get; set; } = string.Empty;
    public bool OverallPassed { get; set; }
    public List<ComplianceItemResultDto> Results { get; set; } = new();
}

public class ComplianceItemResultDto
{
    public Guid ChecklistItemId { get; set; }
    public string ItemNameAr { get; set; } = string.Empty;
    public string ItemNameEn { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? FailureReason { get; set; }
    public string? Notes { get; set; }
}

public class EvaluationScoreDto
{
    public Guid Id { get; set; }
    public Guid ProposalId { get; set; }
    public Guid CriteriaId { get; set; }
    public string CriteriaNameAr { get; set; } = string.Empty;
    public string CriteriaNameEn { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string? Justification { get; set; }
    public decimal? AiSuggestedScore { get; set; }
    public string? AiJustification { get; set; }
    public bool IsFinalized { get; set; }
    public decimal? FinalizedScore { get; set; }
}

public class EvaluationSummaryDto
{
    public Guid ProposalId { get; set; }
    public string VendorNameAr { get; set; } = string.Empty;
    public string VendorReferenceNumber { get; set; } = string.Empty;
    public List<CriteriaSummaryDto> CriteriaSummaries { get; set; } = new();
    public decimal AverageTotal { get; set; }
    public decimal Variance { get; set; }
}

public class CriteriaSummaryDto
{
    public Guid CriteriaId { get; set; }
    public string CriteriaNameAr { get; set; } = string.Empty;
    public decimal AverageScore { get; set; }
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Variance { get; set; }
    public List<MemberScoreDto> MemberScores { get; set; } = new();
}

public class MemberScoreDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string? Justification { get; set; }
}

// ===== Submit Compliance Check Command =====
public record SubmitComplianceCheckCommand(
    Guid ProposalId,
    List<ComplianceCheckItemInput> Items
) : IRequest<ApiResponse<ComplianceCheckDto>>;

public record ComplianceCheckItemInput(
    Guid ChecklistItemId,
    bool Passed,
    string? FailureReason,
    string? Notes
);

public class SubmitComplianceCheckCommandHandler : IRequestHandler<SubmitComplianceCheckCommand, ApiResponse<ComplianceCheckDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SubmitComplianceCheckCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<ComplianceCheckDto>> Handle(SubmitComplianceCheckCommand request, CancellationToken cancellationToken)
    {
        var proposal = await _context.Proposals
            .Include(p => p.ComplianceResults)
            .FirstOrDefaultAsync(p => p.Id == request.ProposalId, cancellationToken);

        if (proposal == null)
            return ApiResponse<ComplianceCheckDto>.Fail("Proposal not found.");

        if (proposal.Status != ProposalStatus.Received)
            return ApiResponse<ComplianceCheckDto>.Fail("Compliance check can only be performed on received proposals.");

        // Remove existing results and replace
        var existingResults = proposal.ComplianceResults.ToList();
        foreach (var r in existingResults)
        {
            r.IsDeleted = true;
            r.DeletedAt = DateTime.UtcNow;
        }

        var allPassed = true;
        var resultDtos = new List<ComplianceItemResultDto>();

        foreach (var item in request.Items)
        {
            // Validate: failure reason is mandatory when not passed
            if (!item.Passed && string.IsNullOrWhiteSpace(item.FailureReason))
                return ApiResponse<ComplianceCheckDto>.Fail($"Failure reason is mandatory for failed checklist item {item.ChecklistItemId}.");

            var checklistItem = await _context.ComplianceChecklists
                .FirstOrDefaultAsync(c => c.Id == item.ChecklistItemId, cancellationToken);

            if (checklistItem == null)
                return ApiResponse<ComplianceCheckDto>.Fail($"Checklist item {item.ChecklistItemId} not found.");

            var result = new ComplianceCheckResult
            {
                ProposalId = request.ProposalId,
                ChecklistItemId = item.ChecklistItemId,
                Passed = item.Passed,
                FailureReason = item.FailureReason,
                Notes = item.Notes,
                CheckedByUserId = _currentUser.UserId ?? Guid.Empty,
                CheckedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId
            };

            _context.ComplianceCheckResults.Add(result);

            if (!item.Passed && checklistItem.IsMandatory)
                allPassed = false;

            resultDtos.Add(new ComplianceItemResultDto
            {
                ChecklistItemId = item.ChecklistItemId,
                ItemNameAr = checklistItem.NameAr,
                ItemNameEn = checklistItem.NameEn,
                Passed = item.Passed,
                FailureReason = item.FailureReason,
                Notes = item.Notes
            });
        }

        // Update proposal status
        proposal.PassedComplianceCheck = allPassed;
        proposal.Status = allPassed ? ProposalStatus.CompliancePassed : ProposalStatus.ComplianceFailed;
        if (!allPassed)
        {
            proposal.ComplianceFailureReason = string.Join("; ",
                request.Items.Where(i => !i.Passed).Select(i => i.FailureReason));
        }
        proposal.UpdatedAt = DateTime.UtcNow;
        proposal.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<ComplianceCheckDto>.Ok(new ComplianceCheckDto
        {
            ProposalId = proposal.Id,
            VendorNameAr = proposal.VendorNameAr,
            VendorReferenceNumber = proposal.VendorReferenceNumber,
            OverallPassed = allPassed,
            Results = resultDtos
        });
    }
}

// ===== Submit Technical Score Command (Blind Evaluation) =====
public record SubmitTechnicalScoreCommand(
    Guid ProposalId,
    List<ScoreInput> Scores
) : IRequest<ApiResponse<List<EvaluationScoreDto>>>;

public record ScoreInput(
    Guid CriteriaId,
    decimal Score,
    string? Justification
);

public class SubmitTechnicalScoreCommandHandler : IRequestHandler<SubmitTechnicalScoreCommand, ApiResponse<List<EvaluationScoreDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SubmitTechnicalScoreCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<List<EvaluationScoreDto>>> Handle(SubmitTechnicalScoreCommand request, CancellationToken cancellationToken)
    {
        var proposal = await _context.Proposals
            .FirstOrDefaultAsync(p => p.Id == request.ProposalId, cancellationToken);

        if (proposal == null)
            return ApiResponse<List<EvaluationScoreDto>>.Fail("Proposal not found.");

        if (proposal.Status != ProposalStatus.CompliancePassed && proposal.Status != ProposalStatus.TechnicalEvaluationInProgress)
            return ApiResponse<List<EvaluationScoreDto>>.Fail("Technical evaluation can only be performed on compliance-passed proposals.");

        var userId = _currentUser.UserId ?? Guid.Empty;
        var resultDtos = new List<EvaluationScoreDto>();

        foreach (var scoreInput in request.Scores)
        {
            var criteria = await _context.TenderCriteria
                .FirstOrDefaultAsync(c => c.Id == scoreInput.CriteriaId && c.TenderId == proposal.TenderId, cancellationToken);

            if (criteria == null)
                return ApiResponse<List<EvaluationScoreDto>>.Fail($"Criteria {scoreInput.CriteriaId} not found for this tender.");

            // Validate score range
            if (scoreInput.Score < 0 || scoreInput.Score > 100)
                return ApiResponse<List<EvaluationScoreDto>>.Fail($"Score must be between 0 and 100 for criteria '{criteria.NameAr}'.");

            // Validate justification requirement for low scores
            if (criteria.PassingThreshold.HasValue && scoreInput.Score < criteria.PassingThreshold.Value
                && string.IsNullOrWhiteSpace(scoreInput.Justification))
            {
                return ApiResponse<List<EvaluationScoreDto>>.Fail(
                    $"Justification is mandatory when score ({scoreInput.Score}) is below threshold ({criteria.PassingThreshold.Value}) for criteria '{criteria.NameAr}'.");
            }

            // Check if score already exists (upsert)
            var existing = await _context.EvaluationScores
                .FirstOrDefaultAsync(s => s.ProposalId == request.ProposalId
                    && s.CriteriaId == scoreInput.CriteriaId
                    && s.EvaluatorUserId == userId, cancellationToken);

            if (existing != null)
            {
                existing.Score = scoreInput.Score;
                existing.Justification = scoreInput.Justification;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = _currentUser.UserId;
            }
            else
            {
                existing = new EvaluationScore
                {
                    ProposalId = request.ProposalId,
                    CriteriaId = scoreInput.CriteriaId,
                    EvaluatorUserId = userId,
                    Score = scoreInput.Score,
                    Justification = scoreInput.Justification,
                    EvaluationType = CriteriaType.Technical,
                    CreatedBy = _currentUser.UserId
                };
                _context.EvaluationScores.Add(existing);
            }

            resultDtos.Add(new EvaluationScoreDto
            {
                Id = existing.Id,
                ProposalId = request.ProposalId,
                CriteriaId = scoreInput.CriteriaId,
                CriteriaNameAr = criteria.NameAr,
                CriteriaNameEn = criteria.NameEn,
                Score = scoreInput.Score,
                Justification = scoreInput.Justification,
                AiSuggestedScore = existing.AiSuggestedScore,
                AiJustification = existing.AiJustification,
                IsFinalized = existing.IsFinalized,
                FinalizedScore = existing.FinalizedScore
            });
        }

        // Update proposal status
        if (proposal.Status == ProposalStatus.CompliancePassed)
        {
            proposal.Status = ProposalStatus.TechnicalEvaluationInProgress;
            proposal.UpdatedAt = DateTime.UtcNow;
            proposal.UpdatedBy = _currentUser.UserId;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<List<EvaluationScoreDto>>.Ok(resultDtos);
    }
}

// ===== Finalize Technical Scores Command (Chair Only) =====
public record FinalizeTechnicalScoresCommand(
    Guid TenderId,
    Guid ProposalId,
    List<FinalizedScoreInput> FinalizedScores
) : IRequest<ApiResponse<string>>;

public record FinalizedScoreInput(
    Guid CriteriaId,
    decimal FinalScore,
    string? Notes
);

public class FinalizeTechnicalScoresCommandHandler : IRequestHandler<FinalizeTechnicalScoresCommand, ApiResponse<string>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public FinalizeTechnicalScoresCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<string>> Handle(FinalizeTechnicalScoresCommand request, CancellationToken cancellationToken)
    {
        var proposal = await _context.Proposals
            .FirstOrDefaultAsync(p => p.Id == request.ProposalId && p.TenderId == request.TenderId, cancellationToken);

        if (proposal == null)
            return ApiResponse<string>.Fail("Proposal not found.");

        var tender = await _context.Tenders
            .FirstOrDefaultAsync(t => t.Id == request.TenderId, cancellationToken);

        if (tender == null)
            return ApiResponse<string>.Fail("Tender not found.");

        // Get all leaf criteria for this tender (technical only)
        var technicalCriteria = await _context.TenderCriteria
            .Where(c => c.TenderId == request.TenderId && c.CriteriaType == CriteriaType.Technical)
            .ToListAsync(cancellationToken);

        var leafCriteria = technicalCriteria.Where(c => !technicalCriteria.Any(ch => ch.ParentId == c.Id)).ToList();

        decimal totalWeightedScore = 0;
        decimal totalWeight = 0;

        foreach (var finalInput in request.FinalizedScores)
        {
            var criteria = leafCriteria.FirstOrDefault(c => c.Id == finalInput.CriteriaId);
            if (criteria == null) continue;

            // Update all scores for this criteria/proposal with finalized value
            var scores = await _context.EvaluationScores
                .Where(s => s.ProposalId == request.ProposalId && s.CriteriaId == finalInput.CriteriaId)
                .ToListAsync(cancellationToken);

            foreach (var score in scores)
            {
                score.IsFinalized = true;
                score.FinalizedScore = finalInput.FinalScore;
                score.FinalizationNotes = finalInput.Notes;
                score.UpdatedAt = DateTime.UtcNow;
                score.UpdatedBy = _currentUser.UserId;
            }

            totalWeightedScore += finalInput.FinalScore * (criteria.Weight / 100m);
            totalWeight += criteria.Weight;
        }

        // Calculate technical score (weighted average normalized to technical weight)
        var technicalScore = totalWeight > 0 ? (totalWeightedScore / totalWeight) * 100m : 0;
        proposal.TechnicalScore = Math.Round(technicalScore * (tender.TechnicalWeight / 100m), 4);
        proposal.PassedTechnicalEvaluation = technicalScore >= 60; // Default passing threshold
        proposal.Status = proposal.PassedTechnicalEvaluation == true
            ? ProposalStatus.TechnicalEvaluationCompleted
            : ProposalStatus.TechnicallyDisqualified;
        proposal.UpdatedAt = DateTime.UtcNow;
        proposal.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<string>.Ok($"Technical scores finalized. Technical score: {proposal.TechnicalScore:F2}");
    }
}

// ===== Submit Financial Evaluation Command =====
public record SubmitFinancialEvaluationCommand(
    Guid TenderId,
    List<FinancialInput> FinancialValues
) : IRequest<ApiResponse<List<FinancialResultDto>>>;

public record FinancialInput(Guid ProposalId, decimal FinancialValue);

public class FinancialResultDto
{
    public Guid ProposalId { get; set; }
    public string VendorNameAr { get; set; } = string.Empty;
    public decimal TechnicalScore { get; set; }
    public decimal FinancialValue { get; set; }
    public decimal FinancialScore { get; set; }
    public decimal FinalScore { get; set; }
    public int FinalRank { get; set; }
}

public class SubmitFinancialEvaluationCommandHandler : IRequestHandler<SubmitFinancialEvaluationCommand, ApiResponse<List<FinancialResultDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SubmitFinancialEvaluationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<List<FinancialResultDto>>> Handle(SubmitFinancialEvaluationCommand request, CancellationToken cancellationToken)
    {
        var tender = await _context.Tenders
            .FirstOrDefaultAsync(t => t.Id == request.TenderId, cancellationToken);

        if (tender == null)
            return ApiResponse<List<FinancialResultDto>>.Fail("Tender not found.");

        // Get technically passed proposals
        var passedProposals = await _context.Proposals
            .Where(p => p.TenderId == request.TenderId && p.Status == ProposalStatus.TechnicalEvaluationCompleted)
            .ToListAsync(cancellationToken);

        if (!passedProposals.Any())
            return ApiResponse<List<FinancialResultDto>>.Fail("No technically passed proposals found.");

        // Validate all passed proposals have financial values
        foreach (var fi in request.FinancialValues)
        {
            if (!passedProposals.Any(p => p.Id == fi.ProposalId))
                return ApiResponse<List<FinancialResultDto>>.Fail($"Proposal {fi.ProposalId} is not technically passed.");
        }

        // Find minimum financial value
        var minFinancialValue = request.FinancialValues.Min(f => f.FinancialValue);

        if (minFinancialValue <= 0)
            return ApiResponse<List<FinancialResultDto>>.Fail("Financial values must be greater than zero.");

        var results = new List<FinancialResultDto>();

        foreach (var fi in request.FinancialValues)
        {
            var proposal = passedProposals.First(p => p.Id == fi.ProposalId);

            // Formula: Financial Score = (Lowest Financial / Current Financial) × Financial Weight
            var financialScore = (minFinancialValue / fi.FinancialValue) * tender.FinancialWeight;
            proposal.FinancialScore = Math.Round(financialScore, 4);
            proposal.TotalValue = fi.FinancialValue;

            // Final Score = Technical Score + Financial Score
            proposal.FinalScore = Math.Round((proposal.TechnicalScore ?? 0) + financialScore, 4);
            proposal.Status = ProposalStatus.FinancialEvaluationCompleted;
            proposal.UpdatedAt = DateTime.UtcNow;
            proposal.UpdatedBy = _currentUser.UserId;

            results.Add(new FinancialResultDto
            {
                ProposalId = proposal.Id,
                VendorNameAr = proposal.VendorNameAr,
                TechnicalScore = proposal.TechnicalScore ?? 0,
                FinancialValue = fi.FinancialValue,
                FinancialScore = proposal.FinancialScore ?? 0,
                FinalScore = proposal.FinalScore ?? 0
            });
        }

        // Rank proposals by final score (descending)
        var ranked = results.OrderByDescending(r => r.FinalScore).ToList();
        for (int i = 0; i < ranked.Count; i++)
        {
            ranked[i].FinalRank = i + 1;
            var proposal = passedProposals.First(p => p.Id == ranked[i].ProposalId);
            proposal.FinalRank = i + 1;
            proposal.Status = ProposalStatus.Ranked;
        }

        // Mark the top-ranked as recommended
        if (ranked.Any())
        {
            var topProposal = passedProposals.First(p => p.Id == ranked[0].ProposalId);
            topProposal.Status = ProposalStatus.Recommended;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<List<FinancialResultDto>>.Ok(ranked);
    }
}
