using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Models;
using Netaq.Application.Proposals.Commands;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Proposals.Queries;

// ===== Get Proposals By Tender =====
public record GetProposalsByTenderQuery(
    Guid TenderId,
    string? StatusFilter = null,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<ApiResponse<PaginatedResult<ProposalDto>>>;

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class GetProposalsByTenderQueryHandler : IRequestHandler<GetProposalsByTenderQuery, ApiResponse<PaginatedResult<ProposalDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetProposalsByTenderQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedResult<ProposalDto>>> Handle(GetProposalsByTenderQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Proposals
            .Include(p => p.Files)
            .Where(p => p.TenderId == request.TenderId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.StatusFilter) && Enum.TryParse<ProposalStatus>(request.StatusFilter, out var status))
        {
            query = query.Where(p => p.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var proposals = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = proposals.Select(p => new ProposalDto
        {
            Id = p.Id,
            TenderId = p.TenderId,
            VendorNameAr = p.VendorNameAr,
            VendorNameEn = p.VendorNameEn,
            VendorReferenceNumber = p.VendorReferenceNumber,
            TotalValue = p.TotalValue,
            Status = p.Status.ToString(),
            ReceivedDate = p.ReceivedDate,
            PassedComplianceCheck = p.PassedComplianceCheck,
            ComplianceFailureReason = p.ComplianceFailureReason,
            TechnicalScore = p.TechnicalScore,
            PassedTechnicalEvaluation = p.PassedTechnicalEvaluation,
            FinancialScore = p.FinancialScore,
            FinalScore = p.FinalScore,
            FinalRank = p.FinalRank,
            AiSummaryAr = p.AiSummaryAr,
            AiSummaryEn = p.AiSummaryEn,
            Notes = p.Notes,
            CreatedAt = p.CreatedAt,
            Files = p.Files?.Select(f => new ProposalFileDto
            {
                Id = f.Id,
                OriginalFileName = f.OriginalFileName,
                ContentType = f.ContentType,
                FileSizeBytes = f.FileSizeBytes,
                Category = f.Category.ToString(),
                IsTextExtracted = f.IsTextExtracted,
                CreatedAt = f.CreatedAt
            }).ToList() ?? new()
        }).ToList();

        return ApiResponse<PaginatedResult<ProposalDto>>.Ok(new PaginatedResult<ProposalDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        });
    }
}

// ===== Get Proposal Detail =====
public record GetProposalDetailQuery(Guid ProposalId) : IRequest<ApiResponse<ProposalDto>>;

public class GetProposalDetailQueryHandler : IRequestHandler<GetProposalDetailQuery, ApiResponse<ProposalDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProposalDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<ProposalDto>> Handle(GetProposalDetailQuery request, CancellationToken cancellationToken)
    {
        var proposal = await _context.Proposals
            .Include(p => p.Files)
            .Include(p => p.ComplianceResults)
            .FirstOrDefaultAsync(p => p.Id == request.ProposalId, cancellationToken);

        if (proposal == null)
            return ApiResponse<ProposalDto>.Fail("Proposal not found.");

        return ApiResponse<ProposalDto>.Ok(new ProposalDto
        {
            Id = proposal.Id,
            TenderId = proposal.TenderId,
            VendorNameAr = proposal.VendorNameAr,
            VendorNameEn = proposal.VendorNameEn,
            VendorReferenceNumber = proposal.VendorReferenceNumber,
            TotalValue = proposal.TotalValue,
            Status = proposal.Status.ToString(),
            ReceivedDate = proposal.ReceivedDate,
            PassedComplianceCheck = proposal.PassedComplianceCheck,
            ComplianceFailureReason = proposal.ComplianceFailureReason,
            TechnicalScore = proposal.TechnicalScore,
            PassedTechnicalEvaluation = proposal.PassedTechnicalEvaluation,
            FinancialScore = proposal.FinancialScore,
            FinalScore = proposal.FinalScore,
            FinalRank = proposal.FinalRank,
            AiSummaryAr = proposal.AiSummaryAr,
            AiSummaryEn = proposal.AiSummaryEn,
            Notes = proposal.Notes,
            CreatedAt = proposal.CreatedAt,
            Files = proposal.Files?.Select(f => new ProposalFileDto
            {
                Id = f.Id,
                OriginalFileName = f.OriginalFileName,
                ContentType = f.ContentType,
                FileSizeBytes = f.FileSizeBytes,
                Category = f.Category.ToString(),
                IsTextExtracted = f.IsTextExtracted,
                CreatedAt = f.CreatedAt
            }).ToList() ?? new()
        });
    }
}

// ===== Get Compliance Checklist =====
public record GetComplianceChecklistQuery(Guid TenderId) : IRequest<ApiResponse<List<ComplianceChecklistDto>>>;

public class ComplianceChecklistDto
{
    public Guid Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public bool IsMandatory { get; set; }
    public int OrderIndex { get; set; }
    public bool IsDefault { get; set; }
}

public class GetComplianceChecklistQueryHandler : IRequestHandler<GetComplianceChecklistQuery, ApiResponse<List<ComplianceChecklistDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetComplianceChecklistQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<ComplianceChecklistDto>>> Handle(GetComplianceChecklistQuery request, CancellationToken cancellationToken)
    {
        var items = await _context.ComplianceChecklists
            .Where(c => c.TenderId == request.TenderId)
            .OrderBy(c => c.OrderIndex)
            .Select(c => new ComplianceChecklistDto
            {
                Id = c.Id,
                NameAr = c.NameAr,
                NameEn = c.NameEn,
                DescriptionAr = c.DescriptionAr,
                DescriptionEn = c.DescriptionEn,
                IsMandatory = c.IsMandatory,
                OrderIndex = c.OrderIndex,
                IsDefault = c.IsDefault
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<List<ComplianceChecklistDto>>.Ok(items);
    }
}
