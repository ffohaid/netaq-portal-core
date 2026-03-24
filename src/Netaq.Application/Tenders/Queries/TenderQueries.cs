using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Models;
using Netaq.Application.Tenders.Commands;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Tenders.Queries;

// ==================== Get Tender By Id ====================

public record GetTenderByIdQuery(Guid Id) : IRequest<ApiResponse<TenderDetailDto>>;

public record TenderSectionDto(
    Guid Id,
    BookletSectionType SectionType,
    string TitleAr,
    string TitleEn,
    string? ContentHtml,
    int CompletionPercentage,
    int OrderIndex,
    bool IsAiReviewed,
    DateTime? LastAutoSavedAt
);

public record TenderCriteriaDto(
    Guid Id,
    Guid? ParentId,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    CriteriaType CriteriaType,
    decimal Weight,
    decimal? PassingThreshold,
    int OrderIndex,
    bool IsAiSuggested,
    List<TenderCriteriaDto> Children
);

public record TenderDetailDto(
    Guid Id,
    string TitleAr,
    string TitleEn,
    string ReferenceNumber,
    string? DescriptionAr,
    string? DescriptionEn,
    TenderType TenderType,
    decimal EstimatedValue,
    TenderStatus Status,
    BookletCreationMethod CreationMethod,
    Guid? BookletTemplateId,
    DateTime? SubmissionOpenDate,
    DateTime? SubmissionCloseDate,
    DateTime? ProjectStartDate,
    DateTime? ProjectEndDate,
    int CompletionPercentage,
    decimal TechnicalWeight,
    decimal FinancialWeight,
    DateTime CreatedAt,
    Guid? CreatedBy,
    List<TenderSectionDto> Sections,
    List<TenderCriteriaDto> Criteria
);

public class GetTenderByIdQueryHandler : IRequestHandler<GetTenderByIdQuery, ApiResponse<TenderDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTenderByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<TenderDetailDto>> Handle(GetTenderByIdQuery request, CancellationToken cancellationToken)
    {
        var tender = await _context.Tenders
            .Include(t => t.Sections.OrderBy(s => s.OrderIndex))
            .Include(t => t.Criteria)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (tender == null)
            return ApiResponse<TenderDetailDto>.Failure("Tender not found.");

        var sections = tender.Sections.Select(s => new TenderSectionDto(
            s.Id, s.SectionType, s.TitleAr, s.TitleEn,
            s.ContentHtml, s.CompletionPercentage, s.OrderIndex,
            s.IsAiReviewed, s.LastAutoSavedAt
        )).ToList();

        var allCriteria = tender.Criteria.ToList();
        var rootCriteria = allCriteria.Where(c => c.ParentId == null).OrderBy(c => c.OrderIndex).ToList();
        var criteriaTree = rootCriteria.Select(c => BuildCriteriaTree(c, allCriteria)).ToList();

        var dto = new TenderDetailDto(
            tender.Id, tender.TitleAr, tender.TitleEn, tender.ReferenceNumber,
            tender.DescriptionAr, tender.DescriptionEn, tender.TenderType,
            tender.EstimatedValue, tender.Status, tender.CreationMethod,
            tender.BookletTemplateId, tender.SubmissionOpenDate, tender.SubmissionCloseDate,
            tender.ProjectStartDate, tender.ProjectEndDate, tender.CompletionPercentage,
            tender.TechnicalWeight, tender.FinancialWeight, tender.CreatedAt, tender.CreatedBy,
            sections, criteriaTree
        );

        return ApiResponse<TenderDetailDto>.Success(dto);
    }

    private static TenderCriteriaDto BuildCriteriaTree(
        Domain.Entities.TenderCriteria criteria,
        List<Domain.Entities.TenderCriteria> allCriteria)
    {
        var children = allCriteria
            .Where(c => c.ParentId == criteria.Id)
            .OrderBy(c => c.OrderIndex)
            .Select(c => BuildCriteriaTree(c, allCriteria))
            .ToList();

        return new TenderCriteriaDto(
            criteria.Id, criteria.ParentId,
            criteria.NameAr, criteria.NameEn,
            criteria.DescriptionAr, criteria.DescriptionEn,
            criteria.CriteriaType, criteria.Weight,
            criteria.PassingThreshold, criteria.OrderIndex,
            criteria.IsAiSuggested, children
        );
    }
}

// ==================== Get Tenders List (Paginated) ====================

public record GetTendersQuery : IRequest<ApiResponse<PaginatedList<TenderDto>>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public TenderStatus? StatusFilter { get; init; }
    public TenderType? TypeFilter { get; init; }
    public string? SearchTerm { get; init; }
}

public record PaginatedList<T>(
    List<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public class GetTendersQueryHandler : IRequestHandler<GetTendersQuery, ApiResponse<PaginatedList<TenderDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetTendersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PaginatedList<TenderDto>>> Handle(GetTendersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Tenders.AsNoTracking().AsQueryable();

        // Apply filters
        if (request.StatusFilter.HasValue)
            query = query.Where(t => t.Status == request.StatusFilter.Value);

        if (request.TypeFilter.HasValue)
            query = query.Where(t => t.TenderType == request.TypeFilter.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(t =>
                t.TitleAr.ToLower().Contains(term) ||
                t.TitleEn.ToLower().Contains(term) ||
                t.ReferenceNumber.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TenderDto(
                t.Id, t.TitleAr, t.TitleEn, t.ReferenceNumber,
                t.DescriptionAr, t.DescriptionEn, t.TenderType,
                t.EstimatedValue, t.Status, t.CreationMethod,
                t.BookletTemplateId, t.SubmissionOpenDate, t.SubmissionCloseDate,
                t.ProjectStartDate, t.ProjectEndDate, t.CompletionPercentage,
                t.TechnicalWeight, t.FinancialWeight, t.CreatedAt, t.CreatedBy
            ))
            .ToListAsync(cancellationToken);

        var result = new PaginatedList<TenderDto>(items, totalCount, request.PageNumber, request.PageSize);
        return ApiResponse<PaginatedList<TenderDto>>.Success(result);
    }
}
