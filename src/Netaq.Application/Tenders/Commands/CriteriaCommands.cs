using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Application.Tenders.Queries;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Tenders.Commands;

// ==================== Save Criteria (Upsert) ====================

public record SaveTenderCriteriaCommand : IRequest<ApiResponse<List<TenderCriteriaDto>>>
{
    public Guid TenderId { get; init; }
    public List<CriteriaItem> Criteria { get; init; } = new();
}

public record CriteriaItem(
    Guid? Id,
    Guid? ParentId,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    CriteriaType CriteriaType,
    decimal Weight,
    decimal? PassingThreshold,
    int OrderIndex,
    List<CriteriaItem>? Children
);

public class SaveTenderCriteriaCommandValidator : AbstractValidator<SaveTenderCriteriaCommand>
{
    public SaveTenderCriteriaCommandValidator()
    {
        RuleFor(x => x.TenderId).NotEmpty();
        RuleFor(x => x.Criteria).NotEmpty().WithMessage("At least one criterion is required.");
    }
}

public class SaveTenderCriteriaCommandHandler : IRequestHandler<SaveTenderCriteriaCommand, ApiResponse<List<TenderCriteriaDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SaveTenderCriteriaCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<List<TenderCriteriaDto>>> Handle(SaveTenderCriteriaCommand request, CancellationToken cancellationToken)
    {
        var tender = await _context.Tenders.FindAsync(new object[] { request.TenderId }, cancellationToken);
        if (tender == null)
            return ApiResponse<List<TenderCriteriaDto>>.Failure("Tender not found.");

        if (tender.Status != TenderStatus.Draft)
            return ApiResponse<List<TenderCriteriaDto>>.Failure("Criteria can only be edited in draft tenders.");

        // Validate weights per type at root level
        var rootTechnical = request.Criteria.Where(c => c.CriteriaType == CriteriaType.Technical && c.ParentId == null).ToList();
        var rootFinancial = request.Criteria.Where(c => c.CriteriaType == CriteriaType.Financial && c.ParentId == null).ToList();

        if (rootTechnical.Any())
        {
            var techSum = rootTechnical.Sum(c => c.Weight);
            if (techSum != 100)
                return ApiResponse<List<TenderCriteriaDto>>.Failure($"Root-level technical criteria weights must sum to 100. Current: {techSum}");
        }

        if (rootFinancial.Any())
        {
            var finSum = rootFinancial.Sum(c => c.Weight);
            if (finSum != 100)
                return ApiResponse<List<TenderCriteriaDto>>.Failure($"Root-level financial criteria weights must sum to 100. Current: {finSum}");
        }

        // Remove existing criteria for this tender
        var existingCriteria = await _context.TenderCriteria
            .Where(c => c.TenderId == request.TenderId)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingCriteria)
        {
            existing.IsDeleted = true;
            existing.DeletedAt = DateTime.UtcNow;
        }

        // Insert new criteria tree
        var resultList = new List<TenderCriteriaDto>();
        foreach (var item in request.Criteria.Where(c => c.ParentId == null))
        {
            var dto = await SaveCriteriaRecursive(request.TenderId, null, item, cancellationToken);
            resultList.Add(dto);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<List<TenderCriteriaDto>>.Success(resultList, "Criteria saved successfully.");
    }

    private async Task<TenderCriteriaDto> SaveCriteriaRecursive(
        Guid tenderId, Guid? parentId, CriteriaItem item, CancellationToken cancellationToken)
    {
        var entity = new TenderCriteria
        {
            TenderId = tenderId,
            ParentId = parentId,
            NameAr = item.NameAr,
            NameEn = item.NameEn,
            DescriptionAr = item.DescriptionAr,
            DescriptionEn = item.DescriptionEn,
            CriteriaType = item.CriteriaType,
            Weight = item.Weight,
            PassingThreshold = item.PassingThreshold,
            OrderIndex = item.OrderIndex,
            CreatedBy = _currentUser.UserId
        };

        _context.TenderCriteria.Add(entity);

        var children = new List<TenderCriteriaDto>();
        if (item.Children != null)
        {
            foreach (var child in item.Children)
            {
                var childDto = await SaveCriteriaRecursive(tenderId, entity.Id, child, cancellationToken);
                children.Add(childDto);
            }
        }

        return new TenderCriteriaDto(
            entity.Id, entity.ParentId,
            entity.NameAr, entity.NameEn,
            entity.DescriptionAr, entity.DescriptionEn,
            entity.CriteriaType, entity.Weight,
            entity.PassingThreshold, entity.OrderIndex,
            entity.IsAiSuggested, children
        );
    }
}

// ==================== Add Single Criterion ====================

public record AddCriterionCommand : IRequest<ApiResponse<TenderCriteriaDto>>
{
    public Guid TenderId { get; init; }
    public Guid? ParentId { get; init; }
    public string NameAr { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public string? DescriptionAr { get; init; }
    public string? DescriptionEn { get; init; }
    public CriteriaType CriteriaType { get; init; }
    public decimal Weight { get; init; }
    public decimal? PassingThreshold { get; init; }
    public int OrderIndex { get; init; }
    public bool IsAiSuggested { get; init; }
}

public class AddCriterionCommandValidator : AbstractValidator<AddCriterionCommand>
{
    public AddCriterionCommandValidator()
    {
        RuleFor(x => x.TenderId).NotEmpty();
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Weight).InclusiveBetween(0, 100);
        RuleFor(x => x.CriteriaType).IsInEnum();
    }
}

public class AddCriterionCommandHandler : IRequestHandler<AddCriterionCommand, ApiResponse<TenderCriteriaDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AddCriterionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<TenderCriteriaDto>> Handle(AddCriterionCommand request, CancellationToken cancellationToken)
    {
        var tender = await _context.Tenders.FindAsync(new object[] { request.TenderId }, cancellationToken);
        if (tender == null)
            return ApiResponse<TenderCriteriaDto>.Failure("Tender not found.");

        if (tender.Status != TenderStatus.Draft)
            return ApiResponse<TenderCriteriaDto>.Failure("Criteria can only be added to draft tenders.");

        var entity = new TenderCriteria
        {
            TenderId = request.TenderId,
            ParentId = request.ParentId,
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            DescriptionAr = request.DescriptionAr,
            DescriptionEn = request.DescriptionEn,
            CriteriaType = request.CriteriaType,
            Weight = request.Weight,
            PassingThreshold = request.PassingThreshold,
            OrderIndex = request.OrderIndex,
            IsAiSuggested = request.IsAiSuggested,
            CreatedBy = _currentUser.UserId
        };

        _context.TenderCriteria.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new TenderCriteriaDto(
            entity.Id, entity.ParentId,
            entity.NameAr, entity.NameEn,
            entity.DescriptionAr, entity.DescriptionEn,
            entity.CriteriaType, entity.Weight,
            entity.PassingThreshold, entity.OrderIndex,
            entity.IsAiSuggested, new List<TenderCriteriaDto>()
        );
        return ApiResponse<TenderCriteriaDto>.Success(dto, "Criterion added successfully.");
    }
}

// ==================== Delete Criterion ====================

public record DeleteCriterionCommand(Guid CriterionId) : IRequest<ApiResponse<bool>>;

public class DeleteCriterionCommandHandler : IRequestHandler<DeleteCriterionCommand, ApiResponse<bool>>
{
    private readonly IApplicationDbContext _context;

    public DeleteCriterionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteCriterionCommand request, CancellationToken cancellationToken)
    {
        var criterion = await _context.TenderCriteria
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == request.CriterionId, cancellationToken);

        if (criterion == null)
            return ApiResponse<bool>.Failure("Criterion not found.");

        // Soft delete criterion and all children recursively
        await SoftDeleteRecursive(criterion, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Success(true, "Criterion deleted successfully.");
    }

    private async Task SoftDeleteRecursive(TenderCriteria criterion, CancellationToken cancellationToken)
    {
        criterion.IsDeleted = true;
        criterion.DeletedAt = DateTime.UtcNow;

        var children = await _context.TenderCriteria
            .Where(c => c.ParentId == criterion.Id)
            .ToListAsync(cancellationToken);

        foreach (var child in children)
        {
            await SoftDeleteRecursive(child, cancellationToken);
        }
    }
}
