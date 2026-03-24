using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Application.Tenders.Queries;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Tenders.Commands;

// ==================== Update Section Content (Auto-Save) ====================

public record UpdateSectionContentCommand : IRequest<ApiResponse<TenderSectionDto>>
{
    public Guid SectionId { get; init; }
    public string? ContentHtml { get; init; }
    public int CompletionPercentage { get; init; }
}

public class UpdateSectionContentCommandValidator : AbstractValidator<UpdateSectionContentCommand>
{
    public UpdateSectionContentCommandValidator()
    {
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.CompletionPercentage).InclusiveBetween(0, 100);
    }
}

public class UpdateSectionContentCommandHandler : IRequestHandler<UpdateSectionContentCommand, ApiResponse<TenderSectionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateSectionContentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<TenderSectionDto>> Handle(UpdateSectionContentCommand request, CancellationToken cancellationToken)
    {
        var section = await _context.TenderSections
            .Include(s => s.Tender)
            .FirstOrDefaultAsync(s => s.Id == request.SectionId, cancellationToken);

        if (section == null)
            return ApiResponse<TenderSectionDto>.Failure("Section not found.");

        if (section.Tender.Status != TenderStatus.Draft)
            return ApiResponse<TenderSectionDto>.Failure("Sections can only be edited in draft tenders.");

        section.ContentHtml = request.ContentHtml;
        section.CompletionPercentage = request.CompletionPercentage;
        section.LastAutoSavedAt = DateTime.UtcNow;
        section.UpdatedAt = DateTime.UtcNow;
        section.UpdatedBy = _currentUser.UserId;

        // Recalculate tender completion percentage
        var allSections = await _context.TenderSections
            .Where(s => s.TenderId == section.TenderId)
            .ToListAsync(cancellationToken);

        // Update the current section in the list
        var idx = allSections.FindIndex(s => s.Id == section.Id);
        if (idx >= 0) allSections[idx] = section;

        var tenderCompletion = allSections.Any()
            ? (int)Math.Round(allSections.Average(s => s.CompletionPercentage))
            : 0;

        section.Tender.CompletionPercentage = tenderCompletion;
        section.Tender.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new TenderSectionDto(
            section.Id, section.SectionType, section.TitleAr, section.TitleEn,
            section.ContentHtml, section.CompletionPercentage, section.OrderIndex,
            section.IsAiReviewed, section.LastAutoSavedAt
        );
        return ApiResponse<TenderSectionDto>.Success(dto, "Section saved successfully.");
    }
}

// ==================== Batch Update Sections ====================

public record BatchUpdateSectionsCommand : IRequest<ApiResponse<List<TenderSectionDto>>>
{
    public Guid TenderId { get; init; }
    public List<SectionUpdateItem> Sections { get; init; } = new();
}

public record SectionUpdateItem(
    Guid SectionId,
    string? ContentHtml,
    int CompletionPercentage
);

public class BatchUpdateSectionsCommandHandler : IRequestHandler<BatchUpdateSectionsCommand, ApiResponse<List<TenderSectionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public BatchUpdateSectionsCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<List<TenderSectionDto>>> Handle(BatchUpdateSectionsCommand request, CancellationToken cancellationToken)
    {
        var tender = await _context.Tenders.FindAsync(new object[] { request.TenderId }, cancellationToken);
        if (tender == null)
            return ApiResponse<List<TenderSectionDto>>.Failure("Tender not found.");

        if (tender.Status != TenderStatus.Draft)
            return ApiResponse<List<TenderSectionDto>>.Failure("Sections can only be edited in draft tenders.");

        var sections = await _context.TenderSections
            .Where(s => s.TenderId == request.TenderId)
            .ToListAsync(cancellationToken);

        var result = new List<TenderSectionDto>();

        foreach (var update in request.Sections)
        {
            var section = sections.FirstOrDefault(s => s.Id == update.SectionId);
            if (section == null) continue;

            section.ContentHtml = update.ContentHtml;
            section.CompletionPercentage = update.CompletionPercentage;
            section.LastAutoSavedAt = DateTime.UtcNow;
            section.UpdatedAt = DateTime.UtcNow;
            section.UpdatedBy = _currentUser.UserId;

            result.Add(new TenderSectionDto(
                section.Id, section.SectionType, section.TitleAr, section.TitleEn,
                section.ContentHtml, section.CompletionPercentage, section.OrderIndex,
                section.IsAiReviewed, section.LastAutoSavedAt
            ));
        }

        // Recalculate tender completion
        tender.CompletionPercentage = sections.Any()
            ? (int)Math.Round(sections.Average(s => s.CompletionPercentage))
            : 0;
        tender.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<List<TenderSectionDto>>.Success(result, "Sections saved successfully.");
    }
}
