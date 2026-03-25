using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Tenders.Commands;

// ==================== DTOs ====================

public record TenderDto(
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
    Guid? CreatedBy
);

// ==================== Create Tender ====================

public record CreateTenderCommand : IRequest<ApiResponse<TenderDto>>
{
    public string TitleAr { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
    public string? DescriptionAr { get; init; }
    public string? DescriptionEn { get; init; }
    public TenderType TenderType { get; init; }
    public decimal EstimatedValue { get; init; }
    public BookletCreationMethod CreationMethod { get; init; } = BookletCreationMethod.FromTemplate;
    public Guid? BookletTemplateId { get; init; }
    public DateTime? SubmissionOpenDate { get; init; }
    public DateTime? SubmissionCloseDate { get; init; }
    public DateTime? ProjectStartDate { get; init; }
    public DateTime? ProjectEndDate { get; init; }
    public decimal TechnicalWeight { get; init; } = 60;
    public decimal FinancialWeight { get; init; } = 40;
}

public class CreateTenderCommandValidator : AbstractValidator<CreateTenderCommand>
{
    public CreateTenderCommandValidator()
    {
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.EstimatedValue).GreaterThan(0);
        RuleFor(x => x.TenderType).IsInEnum();
        RuleFor(x => x.CreationMethod).IsInEnum();
        RuleFor(x => x.TechnicalWeight).InclusiveBetween(0, 100);
        RuleFor(x => x.FinancialWeight).InclusiveBetween(0, 100);
        RuleFor(x => x)
            .Must(x => x.TechnicalWeight + x.FinancialWeight == 100)
            .WithMessage("Technical and financial weights must sum to 100.");
        RuleFor(x => x.BookletTemplateId)
            .NotNull()
            .When(x => x.CreationMethod == BookletCreationMethod.FromTemplate)
            .WithMessage("Template ID is required when creating from template.");
    }
}

public class CreateTenderCommandHandler : IRequestHandler<CreateTenderCommand, ApiResponse<TenderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateTenderCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<TenderDto>> Handle(CreateTenderCommand request, CancellationToken cancellationToken)
    {
        var orgId = _currentUser.OrganizationId;
        if (orgId == null)
            return ApiResponse<TenderDto>.Failure("Organization context is required.");

        // Generate reference number
        var count = await _context.Tenders.CountAsync(cancellationToken) + 1;
        var referenceNumber = $"NTQ-{DateTime.UtcNow:yyyy}-{count:D5}";

        var tender = new Tender
        {
            OrganizationId = orgId.Value,
            TitleAr = request.TitleAr,
            TitleEn = request.TitleEn,
            ReferenceNumber = referenceNumber,
            DescriptionAr = request.DescriptionAr,
            DescriptionEn = request.DescriptionEn,
            TenderType = request.TenderType,
            EstimatedValue = request.EstimatedValue,
            Status = TenderStatus.Draft,
            CreationMethod = request.CreationMethod,
            BookletTemplateId = request.BookletTemplateId,
            SubmissionOpenDate = request.SubmissionOpenDate,
            SubmissionCloseDate = request.SubmissionCloseDate,
            ProjectStartDate = request.ProjectStartDate,
            ProjectEndDate = request.ProjectEndDate,
            TechnicalWeight = request.TechnicalWeight,
            FinancialWeight = request.FinancialWeight,
            CreatedBy = _currentUser.UserId
        };

        _context.Tenders.Add(tender);

        // If creating from template, copy template sections
        if (request.CreationMethod == BookletCreationMethod.FromTemplate && request.BookletTemplateId.HasValue)
        {
            var templateSections = await _context.BookletTemplateSections
                .Where(s => s.BookletTemplateId == request.BookletTemplateId.Value)
                .OrderBy(s => s.OrderIndex)
                .ToListAsync(cancellationToken);

            foreach (var ts in templateSections)
            {
                var section = new TenderSection
                {
                    TenderId = tender.Id,
                    SectionType = ts.SectionType,
                    TitleAr = ts.TitleAr,
                    TitleEn = ts.TitleEn,
                    ContentHtml = ts.DefaultContentHtml,
                    OrderIndex = ts.OrderIndex,
                    CompletionPercentage = string.IsNullOrWhiteSpace(ts.DefaultContentHtml) ? 0 : 10,
                    CreatedBy = _currentUser.UserId
                };
                _context.TenderSections.Add(section);
            }
        }
        else
        {
            // Create empty sections for all 8 standard types
            var sectionTypes = Enum.GetValues<BookletSectionType>();
            for (int i = 0; i < sectionTypes.Length; i++)
            {
                var sectionType = sectionTypes[i];
                var section = new TenderSection
                {
                    TenderId = tender.Id,
                    SectionType = sectionType,
                    TitleAr = GetDefaultSectionTitleAr(sectionType),
                    TitleEn = GetDefaultSectionTitleEn(sectionType),
                    OrderIndex = i + 1,
                    CreatedBy = _currentUser.UserId
                };
                _context.TenderSections.Add(section);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(tender);
        return ApiResponse<TenderDto>.Success(dto, "Tender created successfully.");
    }

    private static string GetDefaultSectionTitleAr(BookletSectionType type) => type switch
    {
        BookletSectionType.GeneralTermsAndConditions => "الباب الأول: الشروط والأحكام العامة",
        BookletSectionType.TechnicalScopeAndSpecifications => "الباب الثاني: النطاق الفني والمواصفات",
        BookletSectionType.QualificationRequirements => "الباب الثالث: متطلبات التأهيل",
        BookletSectionType.EvaluationCriteria => "الباب الرابع: معايير التقييم",
        BookletSectionType.FinancialTerms => "الباب الخامس: الشروط المالية",
        BookletSectionType.ContractualTerms => "الباب السادس: الشروط التعاقدية",
        BookletSectionType.LocalContentRequirements => "الباب السابع: متطلبات المحتوى المحلي",
        BookletSectionType.AppendicesAndForms => "الباب الثامن: الملاحق والنماذج",
        _ => "باب غير محدد"
    };

    private static string GetDefaultSectionTitleEn(BookletSectionType type) => type switch
    {
        BookletSectionType.GeneralTermsAndConditions => "Chapter 1: General Terms and Conditions",
        BookletSectionType.TechnicalScopeAndSpecifications => "Chapter 2: Technical Scope and Specifications",
        BookletSectionType.QualificationRequirements => "Chapter 3: Qualification Requirements",
        BookletSectionType.EvaluationCriteria => "Chapter 4: Evaluation Criteria",
        BookletSectionType.FinancialTerms => "Chapter 5: Financial Terms",
        BookletSectionType.ContractualTerms => "Chapter 6: Contractual Terms",
        BookletSectionType.LocalContentRequirements => "Chapter 7: Local Content Requirements",
        BookletSectionType.AppendicesAndForms => "Chapter 8: Appendices and Forms",
        _ => "Undefined Chapter"
    };

    private static TenderDto MapToDto(Tender t) => new(
        t.Id, t.TitleAr, t.TitleEn, t.ReferenceNumber,
        t.DescriptionAr, t.DescriptionEn, t.TenderType,
        t.EstimatedValue, t.Status, t.CreationMethod,
        t.BookletTemplateId, t.SubmissionOpenDate, t.SubmissionCloseDate,
        t.ProjectStartDate, t.ProjectEndDate, t.CompletionPercentage,
        t.TechnicalWeight, t.FinancialWeight, t.CreatedAt, t.CreatedBy
    );
}

// ==================== Update Tender ====================

public record UpdateTenderCommand : IRequest<ApiResponse<TenderDto>>
{
    public Guid Id { get; init; }
    public string TitleAr { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
    public string? DescriptionAr { get; init; }
    public string? DescriptionEn { get; init; }
    public TenderType TenderType { get; init; }
    public decimal EstimatedValue { get; init; }
    public DateTime? SubmissionOpenDate { get; init; }
    public DateTime? SubmissionCloseDate { get; init; }
    public DateTime? ProjectStartDate { get; init; }
    public DateTime? ProjectEndDate { get; init; }
    public decimal TechnicalWeight { get; init; }
    public decimal FinancialWeight { get; init; }
}

public class UpdateTenderCommandValidator : AbstractValidator<UpdateTenderCommand>
{
    public UpdateTenderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.EstimatedValue).GreaterThan(0);
        RuleFor(x => x.TechnicalWeight).InclusiveBetween(0, 100);
        RuleFor(x => x.FinancialWeight).InclusiveBetween(0, 100);
        RuleFor(x => x)
            .Must(x => x.TechnicalWeight + x.FinancialWeight == 100)
            .WithMessage("Technical and financial weights must sum to 100.");
    }
}

public class UpdateTenderCommandHandler : IRequestHandler<UpdateTenderCommand, ApiResponse<TenderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateTenderCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<TenderDto>> Handle(UpdateTenderCommand request, CancellationToken cancellationToken)
    {
        var tender = await _context.Tenders.FindAsync(new object[] { request.Id }, cancellationToken);
        if (tender == null)
            return ApiResponse<TenderDto>.Failure("Tender not found.");

        if (tender.Status != TenderStatus.Draft)
            return ApiResponse<TenderDto>.Failure("Only draft tenders can be updated.");

        tender.TitleAr = request.TitleAr;
        tender.TitleEn = request.TitleEn;
        tender.DescriptionAr = request.DescriptionAr;
        tender.DescriptionEn = request.DescriptionEn;
        tender.TenderType = request.TenderType;
        tender.EstimatedValue = request.EstimatedValue;
        tender.SubmissionOpenDate = request.SubmissionOpenDate;
        tender.SubmissionCloseDate = request.SubmissionCloseDate;
        tender.ProjectStartDate = request.ProjectStartDate;
        tender.ProjectEndDate = request.ProjectEndDate;
        tender.TechnicalWeight = request.TechnicalWeight;
        tender.FinancialWeight = request.FinancialWeight;
        tender.UpdatedAt = DateTime.UtcNow;
        tender.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new TenderDto(
            tender.Id, tender.TitleAr, tender.TitleEn, tender.ReferenceNumber,
            tender.DescriptionAr, tender.DescriptionEn, tender.TenderType,
            tender.EstimatedValue, tender.Status, tender.CreationMethod,
            tender.BookletTemplateId, tender.SubmissionOpenDate, tender.SubmissionCloseDate,
            tender.ProjectStartDate, tender.ProjectEndDate, tender.CompletionPercentage,
            tender.TechnicalWeight, tender.FinancialWeight, tender.CreatedAt, tender.CreatedBy
        );
        return ApiResponse<TenderDto>.Success(dto, "Tender updated successfully.");
    }
}

// ==================== Submit Tender For Approval ====================

public record SubmitTenderForApprovalCommand(Guid TenderId) : IRequest<ApiResponse<TenderDto>>;

public class SubmitTenderForApprovalCommandHandler : IRequestHandler<SubmitTenderForApprovalCommand, ApiResponse<TenderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SubmitTenderForApprovalCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<TenderDto>> Handle(SubmitTenderForApprovalCommand request, CancellationToken cancellationToken)
    {
        var tender = await _context.Tenders
            .Include(t => t.Sections)
            .Include(t => t.Criteria)
            .FirstOrDefaultAsync(t => t.Id == request.TenderId, cancellationToken);

        if (tender == null)
            return ApiResponse<TenderDto>.Failure("Tender not found.");

        if (tender.Status != TenderStatus.Draft)
            return ApiResponse<TenderDto>.Failure("Only draft tenders can be submitted for approval.");

        // Validate completeness
        if (!tender.Sections.Any())
            return ApiResponse<TenderDto>.Failure("Tender must have at least one section.");

        var emptySections = tender.Sections.Where(s => string.IsNullOrWhiteSpace(s.ContentHtml)).ToList();
        if (emptySections.Any())
            return ApiResponse<TenderDto>.Failure($"The following sections are empty: {string.Join(", ", emptySections.Select(s => s.TitleEn))}");

        // Validate criteria weights
        var technicalCriteria = tender.Criteria.Where(c => c.CriteriaType == CriteriaType.Technical && c.ParentId == null).ToList();
        var financialCriteria = tender.Criteria.Where(c => c.CriteriaType == CriteriaType.Financial && c.ParentId == null).ToList();

        if (!technicalCriteria.Any())
            return ApiResponse<TenderDto>.Failure("At least one technical evaluation criterion is required.");

        var techSum = technicalCriteria.Sum(c => c.Weight);
        if (techSum != 100)
            return ApiResponse<TenderDto>.Failure($"Technical criteria weights must sum to 100. Current sum: {techSum}");

        if (financialCriteria.Any())
        {
            var finSum = financialCriteria.Sum(c => c.Weight);
            if (finSum != 100)
                return ApiResponse<TenderDto>.Failure($"Financial criteria weights must sum to 100. Current sum: {finSum}");
        }

        // Transition state
        tender.PreviousStatus = tender.Status;
        tender.Status = TenderStatus.PendingApproval;
        tender.UpdatedAt = DateTime.UtcNow;
        tender.UpdatedBy = _currentUser.UserId;

        // === Workflow Integration ===
        // Find an active workflow template for this organization
        var workflowTemplate = await _context.WorkflowTemplates
            .Include(wt => wt.Steps)
            .Where(wt => wt.OrganizationId == tender.OrganizationId && wt.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (workflowTemplate != null && workflowTemplate.Steps.Any())
        {
            var firstStep = workflowTemplate.Steps.OrderBy(s => s.Order).First();

            // Create WorkflowInstance
            var workflowInstance = new WorkflowInstance
            {
                Id = Guid.NewGuid(),
                OrganizationId = tender.OrganizationId,
                WorkflowTemplateId = workflowTemplate.Id,
                EntityId = tender.Id,
                EntityType = "Tender",
                Status = WorkflowInstanceStatus.Active,
                CurrentStepId = firstStep.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId
            };
            _context.WorkflowInstances.Add(workflowInstance);

            // Find the user to assign the first step to
            Guid? assigneeId = firstStep.AssignedUserId;
            if (assigneeId == null)
            {
                // Find first active user with the required role
                var assignee = await _context.Users
                    .Where(u => u.OrganizationId == tender.OrganizationId
                        && u.Role == firstStep.RequiredRole
                        && u.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);
                assigneeId = assignee?.Id;
            }

            if (assigneeId != null)
            {
                // Create UserTask for the approver
                var userTask = new UserTask
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = tender.OrganizationId,
                    AssignedUserId = assigneeId.Value,
                    WorkflowInstanceId = workflowInstance.Id,
                    WorkflowStepId = firstStep.Id,
                    TitleAr = $"\u0627\u0639\u062a\u0645\u0627\u062f \u0643\u0631\u0627\u0633\u0629 \u0627\u0644\u0634\u0631\u0648\u0637: {tender.TitleAr}",
                    TitleEn = $"Approve Tender Booklet: {tender.TitleEn}",
                    DescriptionAr = $"\u0645\u0637\u0644\u0648\u0628 \u0627\u0639\u062a\u0645\u0627\u062f \u0643\u0631\u0627\u0633\u0629 \u0627\u0644\u0634\u0631\u0648\u0637 \u0648\u0627\u0644\u0645\u0648\u0627\u0635\u0641\u0627\u062a \u0644\u0644\u0645\u0646\u0627\u0641\u0633\u0629 {tender.ReferenceNumber}",
                    DescriptionEn = $"Approval required for tender booklet {tender.ReferenceNumber}",
                    Status = UserTaskStatus.Pending,
                    Priority = TaskPriority.High,
                    EntityId = tender.Id,
                    EntityType = "Tender",
                    DueDate = DateTime.UtcNow.AddHours(firstStep.SlaDurationHours),
                    SlaStatus = SlaStatus.OnTrack,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUser.UserId
                };
                _context.UserTasks.Add(userTask);

                // Create SLA Tracking
                var slaTracking = new SlaTracking
                {
                    Id = Guid.NewGuid(),
                    WorkflowInstanceId = workflowInstance.Id,
                    WorkflowStepId = firstStep.Id,
                    StartedAt = DateTime.UtcNow,
                    Deadline = DateTime.UtcNow.AddHours(firstStep.SlaDurationHours),
                    Status = SlaStatus.OnTrack,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SlaTrackings.Add(slaTracking);
            }
        }

        // === Audit Log ===
        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            OrganizationId = tender.OrganizationId,
            UserId = _currentUser.UserId,
            ActionCategory = AuditActionCategory.TenderManagement,
            ActionType = "SubmitForApproval",
            ActionDescription = $"Tender {tender.ReferenceNumber} submitted for approval",
            EntityType = "Tender",
            EntityId = tender.Id,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { tender.Status, tender.ReferenceNumber }),
            Timestamp = DateTime.UtcNow,
            IpAddress = "system",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        });

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new TenderDto(
            tender.Id, tender.TitleAr, tender.TitleEn, tender.ReferenceNumber,
            tender.DescriptionAr, tender.DescriptionEn, tender.TenderType,
            tender.EstimatedValue, tender.Status, tender.CreationMethod,
            tender.BookletTemplateId, tender.SubmissionOpenDate, tender.SubmissionCloseDate,
            tender.ProjectStartDate, tender.ProjectEndDate, tender.CompletionPercentage,
            tender.TechnicalWeight, tender.FinancialWeight, tender.CreatedAt, tender.CreatedBy
        );
        return ApiResponse<TenderDto>.Success(dto, "Tender submitted for approval.");
    }
}

// ==================== Cancel Tender ====================

public record CancelTenderCommand(Guid TenderId, string Reason) : IRequest<ApiResponse<TenderDto>>;

public class CancelTenderCommandHandler : IRequestHandler<CancelTenderCommand, ApiResponse<TenderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CancelTenderCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<TenderDto>> Handle(CancelTenderCommand request, CancellationToken cancellationToken)
    {
        var tender = await _context.Tenders.FindAsync(new object[] { request.TenderId }, cancellationToken);
        if (tender == null)
            return ApiResponse<TenderDto>.Failure("Tender not found.");

        if (tender.Status == TenderStatus.Cancelled || tender.Status == TenderStatus.Archived)
            return ApiResponse<TenderDto>.Failure("Tender is already cancelled or archived.");

        tender.PreviousStatus = tender.Status;
        tender.Status = TenderStatus.Cancelled;
        tender.CancellationReason = request.Reason;
        tender.CancelledAt = DateTime.UtcNow;
        tender.CancelledBy = _currentUser.UserId;
        tender.UpdatedAt = DateTime.UtcNow;
        tender.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new TenderDto(
            tender.Id, tender.TitleAr, tender.TitleEn, tender.ReferenceNumber,
            tender.DescriptionAr, tender.DescriptionEn, tender.TenderType,
            tender.EstimatedValue, tender.Status, tender.CreationMethod,
            tender.BookletTemplateId, tender.SubmissionOpenDate, tender.SubmissionCloseDate,
            tender.ProjectStartDate, tender.ProjectEndDate, tender.CompletionPercentage,
            tender.TechnicalWeight, tender.FinancialWeight, tender.CreatedAt, tender.CreatedBy
        );
        return ApiResponse<TenderDto>.Success(dto, "Tender cancelled successfully.");
    }
}
