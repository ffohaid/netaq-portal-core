using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Models;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Templates.Queries;

// ==================== DTOs ====================

public record BookletTemplateDto(
    Guid Id,
    string NameAr,
    string NameEn,
    TemplateCategory Category,
    TenderType ApplicableTenderType,
    string? DescriptionAr,
    string? DescriptionEn,
    bool IsActive,
    string Version,
    int SectionCount
);

public record BookletTemplateSectionDto(
    Guid Id,
    BookletSectionType SectionType,
    string TitleAr,
    string TitleEn,
    string? DefaultContentHtml,
    int OrderIndex,
    string? GuidanceNotesAr,
    string? GuidanceNotesEn
);

public record BookletTemplateDetailDto(
    Guid Id,
    string NameAr,
    string NameEn,
    TemplateCategory Category,
    TenderType ApplicableTenderType,
    string? DescriptionAr,
    string? DescriptionEn,
    bool IsActive,
    string Version,
    List<BookletTemplateSectionDto> Sections
);

// ==================== Get Templates List ====================

public record GetTemplatesQuery : IRequest<ApiResponse<List<BookletTemplateDto>>>
{
    public TemplateCategory? CategoryFilter { get; init; }
    public TenderType? TenderTypeFilter { get; init; }
    public bool ActiveOnly { get; init; } = true;
}

public class GetTemplatesQueryHandler : IRequestHandler<GetTemplatesQuery, ApiResponse<List<BookletTemplateDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetTemplatesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<BookletTemplateDto>>> Handle(GetTemplatesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.BookletTemplates
            .Include(t => t.Sections)
            .AsNoTracking()
            .AsQueryable();

        if (request.ActiveOnly)
            query = query.Where(t => t.IsActive);

        if (request.CategoryFilter.HasValue)
            query = query.Where(t => t.Category == request.CategoryFilter.Value);

        if (request.TenderTypeFilter.HasValue)
            query = query.Where(t => t.ApplicableTenderType == request.TenderTypeFilter.Value);

        var templates = await query
            .OrderBy(t => t.Category)
            .ThenBy(t => t.NameEn)
            .Select(t => new BookletTemplateDto(
                t.Id, t.NameAr, t.NameEn, t.Category,
                t.ApplicableTenderType, t.DescriptionAr, t.DescriptionEn,
                t.IsActive, t.Version, t.Sections.Count
            ))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<BookletTemplateDto>>.Success(templates);
    }
}

// ==================== Get Template By Id ====================

public record GetTemplateByIdQuery(Guid Id) : IRequest<ApiResponse<BookletTemplateDetailDto>>;

public class GetTemplateByIdQueryHandler : IRequestHandler<GetTemplateByIdQuery, ApiResponse<BookletTemplateDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTemplateByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<BookletTemplateDetailDto>> Handle(GetTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        var template = await _context.BookletTemplates
            .Include(t => t.Sections.OrderBy(s => s.OrderIndex))
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (template == null)
            return ApiResponse<BookletTemplateDetailDto>.Failure("Template not found.");

        var sections = template.Sections.Select(s => new BookletTemplateSectionDto(
            s.Id, s.SectionType, s.TitleAr, s.TitleEn,
            s.DefaultContentHtml, s.OrderIndex,
            s.GuidanceNotesAr, s.GuidanceNotesEn
        )).ToList();

        var dto = new BookletTemplateDetailDto(
            template.Id, template.NameAr, template.NameEn, template.Category,
            template.ApplicableTenderType, template.DescriptionAr, template.DescriptionEn,
            template.IsActive, template.Version, sections
        );

        return ApiResponse<BookletTemplateDetailDto>.Success(dto);
    }
}
