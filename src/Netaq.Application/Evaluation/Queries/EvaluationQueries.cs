using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Application.Evaluation.Commands;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Evaluation.Queries;

// ===== Get My Scores (Blind - only current user's scores) =====
public record GetMyScoresQuery(Guid ProposalId) : IRequest<ApiResponse<List<EvaluationScoreDto>>>;

public class GetMyScoresQueryHandler : IRequestHandler<GetMyScoresQuery, ApiResponse<List<EvaluationScoreDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyScoresQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<List<EvaluationScoreDto>>> Handle(GetMyScoresQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        // Blind evaluation: only return current user's scores
        var scores = await _context.EvaluationScores
            .Include(s => s.Criteria)
            .Where(s => s.ProposalId == request.ProposalId && s.EvaluatorUserId == userId)
            .OrderBy(s => s.Criteria.OrderIndex)
            .Select(s => new EvaluationScoreDto
            {
                Id = s.Id,
                ProposalId = s.ProposalId,
                CriteriaId = s.CriteriaId,
                CriteriaNameAr = s.Criteria.NameAr,
                CriteriaNameEn = s.Criteria.NameEn,
                Score = s.Score,
                Justification = s.Justification,
                AiSuggestedScore = s.AiSuggestedScore,
                AiJustification = s.AiJustification,
                IsFinalized = s.IsFinalized,
                FinalizedScore = s.FinalizedScore
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<List<EvaluationScoreDto>>.Ok(scores);
    }
}

// ===== Get Evaluation Summary (Chair Only) =====
public record GetEvaluationSummaryQuery(Guid TenderId, Guid ProposalId) : IRequest<ApiResponse<EvaluationSummaryDto>>;

public class GetEvaluationSummaryQueryHandler : IRequestHandler<GetEvaluationSummaryQuery, ApiResponse<EvaluationSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetEvaluationSummaryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<EvaluationSummaryDto>> Handle(GetEvaluationSummaryQuery request, CancellationToken cancellationToken)
    {
        var proposal = await _context.Proposals
            .FirstOrDefaultAsync(p => p.Id == request.ProposalId && p.TenderId == request.TenderId, cancellationToken);

        if (proposal == null)
            return ApiResponse<EvaluationSummaryDto>.Fail("Proposal not found.");

        var allScores = await _context.EvaluationScores
            .Include(s => s.Criteria)
            .Include(s => s.EvaluatorUser)
            .Where(s => s.ProposalId == request.ProposalId && s.EvaluationType == CriteriaType.Technical)
            .ToListAsync(cancellationToken);

        if (!allScores.Any())
            return ApiResponse<EvaluationSummaryDto>.Fail("No evaluation scores found for this proposal.");

        var criteriaGroups = allScores.GroupBy(s => s.CriteriaId);
        var criteriaSummaries = new List<CriteriaSummaryDto>();

        foreach (var group in criteriaGroups)
        {
            var scores = group.ToList();
            var avgScore = scores.Average(s => s.Score);
            var minScore = scores.Min(s => s.Score);
            var maxScore = scores.Max(s => s.Score);
            var variance = scores.Count > 1
                ? scores.Sum(s => (s.Score - avgScore) * (s.Score - avgScore)) / (scores.Count - 1)
                : 0;

            criteriaSummaries.Add(new CriteriaSummaryDto
            {
                CriteriaId = group.Key,
                CriteriaNameAr = scores.First().Criteria.NameAr,
                AverageScore = Math.Round(avgScore, 2),
                MinScore = minScore,
                MaxScore = maxScore,
                Variance = Math.Round(variance, 2),
                MemberScores = scores.Select(s => new MemberScoreDto
                {
                    UserId = s.EvaluatorUserId,
                    UserName = s.EvaluatorUser?.FullNameAr ?? "Unknown",
                    Score = s.Score,
                    Justification = s.Justification
                }).ToList()
            });
        }

        var overallAvg = criteriaSummaries.Average(c => c.AverageScore);
        var overallVariance = criteriaSummaries.Count > 1
            ? criteriaSummaries.Sum(c => (c.AverageScore - overallAvg) * (c.AverageScore - overallAvg)) / (criteriaSummaries.Count - 1)
            : 0;

        return ApiResponse<EvaluationSummaryDto>.Ok(new EvaluationSummaryDto
        {
            ProposalId = proposal.Id,
            VendorNameAr = proposal.VendorNameAr,
            VendorReferenceNumber = proposal.VendorReferenceNumber,
            CriteriaSummaries = criteriaSummaries,
            AverageTotal = Math.Round(overallAvg, 2),
            Variance = Math.Round(overallVariance, 2)
        });
    }
}

// ===== Get Final Rankings =====
public record GetFinalRankingsQuery(Guid TenderId) : IRequest<ApiResponse<List<FinalRankingDto>>>;

public class FinalRankingDto
{
    public Guid ProposalId { get; set; }
    public string VendorNameAr { get; set; } = string.Empty;
    public string VendorNameEn { get; set; } = string.Empty;
    public string VendorReferenceNumber { get; set; } = string.Empty;
    public decimal TechnicalScore { get; set; }
    public decimal FinancialScore { get; set; }
    public decimal FinalScore { get; set; }
    public int FinalRank { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class GetFinalRankingsQueryHandler : IRequestHandler<GetFinalRankingsQuery, ApiResponse<List<FinalRankingDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetFinalRankingsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<FinalRankingDto>>> Handle(GetFinalRankingsQuery request, CancellationToken cancellationToken)
    {
        var proposals = await _context.Proposals
            .Where(p => p.TenderId == request.TenderId && p.FinalRank != null)
            .OrderBy(p => p.FinalRank)
            .Select(p => new FinalRankingDto
            {
                ProposalId = p.Id,
                VendorNameAr = p.VendorNameAr,
                VendorNameEn = p.VendorNameEn,
                VendorReferenceNumber = p.VendorReferenceNumber,
                TechnicalScore = p.TechnicalScore ?? 0,
                FinancialScore = p.FinancialScore ?? 0,
                FinalScore = p.FinalScore ?? 0,
                FinalRank = p.FinalRank ?? 0,
                Status = p.Status.ToString()
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<List<FinalRankingDto>>.Ok(proposals);
    }
}
