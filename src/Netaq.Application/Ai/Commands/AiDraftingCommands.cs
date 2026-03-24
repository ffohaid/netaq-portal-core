using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Application.Tenders.Queries;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Ai.Commands;

// ==================== DTOs ====================

public record AiSuggestionDto(
    string Content,
    string Provider,
    string Model,
    double ConfidenceScore
);

public record AiComplianceCheckDto(
    bool IsCompliant,
    List<ComplianceIssue> Issues,
    string Summary,
    string Provider,
    string Model
);

public record ComplianceIssue(
    string SectionTitle,
    string Issue,
    string Suggestion,
    string Severity // High, Medium, Low
);

public record AiCriteriaSuggestionDto(
    List<TenderCriteriaDto> SuggestedCriteria,
    string Rationale,
    string Provider,
    string Model
);

// ==================== Suggest Criteria ====================

public record SuggestCriteriaCommand : IRequest<ApiResponse<AiCriteriaSuggestionDto>>
{
    public Guid TenderId { get; init; }
    public CriteriaType CriteriaType { get; init; }
    public string? AdditionalContext { get; init; }
}

public class SuggestCriteriaCommandHandler : IRequestHandler<SuggestCriteriaCommand, ApiResponse<AiCriteriaSuggestionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAiDraftingService _aiService;

    public SuggestCriteriaCommandHandler(IApplicationDbContext context, IAiDraftingService aiService)
    {
        _context = context;
        _aiService = aiService;
    }

    public async Task<ApiResponse<AiCriteriaSuggestionDto>> Handle(SuggestCriteriaCommand request, CancellationToken cancellationToken)
    {
        var tender = await _context.Tenders
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TenderId, cancellationToken);

        if (tender == null)
            return ApiResponse<AiCriteriaSuggestionDto>.Failure("Tender not found.");

        var result = await _aiService.SuggestCriteriaAsync(
            tender.TitleAr, tender.TitleEn,
            tender.DescriptionAr ?? "", tender.DescriptionEn ?? "",
            tender.TenderType, request.CriteriaType,
            request.AdditionalContext,
            tender.OrganizationId,
            cancellationToken);

        return ApiResponse<AiCriteriaSuggestionDto>.Success(result);
    }
}

// ==================== Check Legal Compliance ====================

public record CheckLegalComplianceCommand : IRequest<ApiResponse<AiComplianceCheckDto>>
{
    public Guid TenderId { get; init; }
    public Guid? SectionId { get; init; } // null = check all sections
}

public class CheckLegalComplianceCommandHandler : IRequestHandler<CheckLegalComplianceCommand, ApiResponse<AiComplianceCheckDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAiDraftingService _aiService;

    public CheckLegalComplianceCommandHandler(IApplicationDbContext context, IAiDraftingService aiService)
    {
        _context = context;
        _aiService = aiService;
    }

    public async Task<ApiResponse<AiComplianceCheckDto>> Handle(CheckLegalComplianceCommand request, CancellationToken cancellationToken)
    {
        var tender = await _context.Tenders
            .Include(t => t.Sections)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TenderId, cancellationToken);

        if (tender == null)
            return ApiResponse<AiComplianceCheckDto>.Failure("Tender not found.");

        var sectionsToCheck = request.SectionId.HasValue
            ? tender.Sections.Where(s => s.Id == request.SectionId.Value).ToList()
            : tender.Sections.ToList();

        if (!sectionsToCheck.Any())
            return ApiResponse<AiComplianceCheckDto>.Failure("No sections found to check.");

        var result = await _aiService.CheckLegalComplianceAsync(
            tender.TitleAr, tender.TitleEn,
            tender.TenderType,
            sectionsToCheck.Select(s => new SectionContent(s.TitleEn, s.ContentHtml ?? "")).ToList(),
            tender.OrganizationId,
            cancellationToken);

        // Update section AI review status
        if (!request.SectionId.HasValue)
        {
            var sectionEntities = await _context.TenderSections
                .Where(s => s.TenderId == request.TenderId)
                .ToListAsync(cancellationToken);

            foreach (var section in sectionEntities)
            {
                section.IsAiReviewed = true;
                section.AiComplianceResult = System.Text.Json.JsonSerializer.Serialize(result);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse<AiComplianceCheckDto>.Success(result);
    }
}

// ==================== Generate Boilerplate Content ====================

public record GenerateBoilerplateCommand : IRequest<ApiResponse<AiSuggestionDto>>
{
    public Guid TenderId { get; init; }
    public BookletSectionType SectionType { get; init; }
    public string? AdditionalContext { get; init; }
}

public class GenerateBoilerplateCommandHandler : IRequestHandler<GenerateBoilerplateCommand, ApiResponse<AiSuggestionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAiDraftingService _aiService;

    public GenerateBoilerplateCommandHandler(IApplicationDbContext context, IAiDraftingService aiService)
    {
        _context = context;
        _aiService = aiService;
    }

    public async Task<ApiResponse<AiSuggestionDto>> Handle(GenerateBoilerplateCommand request, CancellationToken cancellationToken)
    {
        var tender = await _context.Tenders
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TenderId, cancellationToken);

        if (tender == null)
            return ApiResponse<AiSuggestionDto>.Failure("Tender not found.");

        var result = await _aiService.GenerateBoilerplateAsync(
            tender.TitleAr, tender.TitleEn,
            tender.DescriptionAr ?? "", tender.DescriptionEn ?? "",
            tender.TenderType, request.SectionType,
            request.AdditionalContext,
            tender.OrganizationId,
            cancellationToken);

        return ApiResponse<AiSuggestionDto>.Success(result);
    }
}

// ==================== Service Interface ====================

public record SectionContent(string Title, string Content);

public interface IAiDraftingService
{
    Task<AiCriteriaSuggestionDto> SuggestCriteriaAsync(
        string titleAr, string titleEn,
        string descriptionAr, string descriptionEn,
        TenderType tenderType, CriteriaType criteriaType,
        string? additionalContext,
        Guid organizationId,
        CancellationToken cancellationToken);

    Task<AiComplianceCheckDto> CheckLegalComplianceAsync(
        string titleAr, string titleEn,
        TenderType tenderType,
        List<SectionContent> sections,
        Guid organizationId,
        CancellationToken cancellationToken);

    Task<AiSuggestionDto> GenerateBoilerplateAsync(
        string titleAr, string titleEn,
        string descriptionAr, string descriptionEn,
        TenderType tenderType, BookletSectionType sectionType,
        string? additionalContext,
        Guid organizationId,
        CancellationToken cancellationToken);
}
