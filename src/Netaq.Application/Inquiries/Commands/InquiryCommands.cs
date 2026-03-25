using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Committees.Commands;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Inquiries.Commands;

// ==================== DTOs ====================
public record InquiryDto(
    Guid Id,
    Guid TenderId,
    string? TenderTitleAr,
    string? TenderTitleEn,
    string SubjectAr,
    string SubjectEn,
    string QuestionAr,
    string QuestionEn,
    string? ResponseAr,
    string? ResponseEn,
    InquiryStatus Status,
    InquiryPriority Priority,
    InquiryCategory Category,
    Guid SubmittedByUserId,
    string? SubmittedByUserNameAr,
    string? SubmittedByUserNameEn,
    Guid? AssignedToUserId,
    string? AssignedToUserNameAr,
    string? AssignedToUserNameEn,
    Guid? TenderSectionId,
    DateTime? DueDate,
    DateTime? RespondedAt,
    DateTime? ClosedAt,
    DateTime CreatedAt
);

// ==================== Create Inquiry ====================
public record CreateInquiryCommand(
    Guid TenderId,
    string SubjectAr,
    string SubjectEn,
    string QuestionAr,
    string QuestionEn,
    InquiryPriority Priority,
    InquiryCategory Category,
    Guid? TenderSectionId,
    Guid? AssignedToUserId
) : IRequest<ApiResponse<InquiryDto>>;

public class CreateInquiryCommandValidator : AbstractValidator<CreateInquiryCommand>
{
    public CreateInquiryCommandValidator()
    {
        RuleFor(x => x.TenderId).NotEmpty();
        RuleFor(x => x.SubjectAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.SubjectEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.QuestionAr).NotEmpty();
        RuleFor(x => x.QuestionEn).NotEmpty();
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Category).IsInEnum();
    }
}

public class CreateInquiryCommandHandler : IRequestHandler<CreateInquiryCommand, ApiResponse<InquiryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateInquiryCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<InquiryDto>> Handle(CreateInquiryCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.OrganizationId == null || _currentUser.UserId == null)
            return ApiResponse<InquiryDto>.Failure("Authentication required.");

        var tender = await _context.Tenders.FindAsync(new object[] { request.TenderId }, cancellationToken);
        if (tender == null)
            return ApiResponse<InquiryDto>.Failure("Tender not found.");

        var inquiry = new Inquiry
        {
            OrganizationId = _currentUser.OrganizationId.Value,
            TenderId = request.TenderId,
            SubmittedByUserId = _currentUser.UserId.Value,
            AssignedToUserId = request.AssignedToUserId,
            SubjectAr = request.SubjectAr,
            SubjectEn = request.SubjectEn,
            QuestionAr = request.QuestionAr,
            QuestionEn = request.QuestionEn,
            Status = InquiryStatus.Submitted,
            Priority = request.Priority,
            Category = request.Category,
            TenderSectionId = request.TenderSectionId,
            DueDate = DateTime.UtcNow.AddDays(request.Priority == InquiryPriority.Urgent ? 1 : request.Priority == InquiryPriority.High ? 3 : 5),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };

        _context.Inquiries.Add(inquiry);

        // Create a task for the assigned user if specified
        if (request.AssignedToUserId.HasValue)
        {
            // Find or create a default workflow for inquiries
            var workflowInstance = await GetOrCreateInquiryWorkflowInstance(_context, _currentUser, inquiry, cancellationToken);

            if (workflowInstance != null)
            {
                var task = new UserTask
                {
                    OrganizationId = _currentUser.OrganizationId.Value,
                    AssignedUserId = request.AssignedToUserId.Value,
                    WorkflowInstanceId = workflowInstance.Id,
                    WorkflowStepId = workflowInstance.CurrentStepId ?? Guid.NewGuid(),
                    TitleAr = $"استفسار جديد: {request.SubjectAr}",
                    TitleEn = $"New Inquiry: {request.SubjectEn}",
                    DescriptionAr = request.QuestionAr,
                    DescriptionEn = request.QuestionEn,
                    Status = UserTaskStatus.Pending,
                    Priority = request.Priority == InquiryPriority.Urgent ? TaskPriority.Critical :
                               request.Priority == InquiryPriority.High ? TaskPriority.High : TaskPriority.Medium,
                    EntityId = inquiry.Id,
                    EntityType = "Inquiry",
                    DueDate = inquiry.DueDate ?? DateTime.UtcNow.AddDays(5),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUser.UserId
                };
                _context.UserTasks.Add(task);
            }
        }

        await CreateCommitteeCommandHandler.LogAuditAsync(_context, _currentUser, "InquiryCreated",
            $"Inquiry '{request.SubjectEn}' created for tender '{tender.TitleEn}'",
            "Inquiry", inquiry.Id, null, inquiry, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(inquiry, tender, null, null);
        return ApiResponse<InquiryDto>.Success(dto, "Inquiry created successfully.");
    }

    private static async Task<WorkflowInstance?> GetOrCreateInquiryWorkflowInstance(
        IApplicationDbContext context, ICurrentUserService currentUser, Inquiry inquiry, CancellationToken ct)
    {
        // Find an existing inquiry workflow template
        var template = await context.WorkflowTemplates
            .Include(t => t.Steps)
            .FirstOrDefaultAsync(t => t.NameEn == "Inquiry Workflow" && t.OrganizationId == currentUser.OrganizationId, ct);

        if (template == null || !template.Steps.Any())
            return null;

        var instance = new WorkflowInstance
        {
            OrganizationId = currentUser.OrganizationId!.Value,
            WorkflowTemplateId = template.Id,
            EntityId = inquiry.Id,
            EntityType = "Inquiry",
            Status = WorkflowInstanceStatus.Active,
            CurrentStepId = template.Steps.OrderBy(s => s.Order).First().Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.UserId
        };

        context.WorkflowInstances.Add(instance);
        return instance;
    }

    internal static InquiryDto MapToDto(Inquiry inquiry, Tender? tender, User? submitter, User? assignee)
    {
        return new InquiryDto(
            inquiry.Id, inquiry.TenderId,
            tender?.TitleAr, tender?.TitleEn,
            inquiry.SubjectAr, inquiry.SubjectEn,
            inquiry.QuestionAr, inquiry.QuestionEn,
            inquiry.ResponseAr, inquiry.ResponseEn,
            inquiry.Status, inquiry.Priority, inquiry.Category,
            inquiry.SubmittedByUserId,
            submitter?.FullNameAr, submitter?.FullNameEn,
            inquiry.AssignedToUserId,
            assignee?.FullNameAr, assignee?.FullNameEn,
            inquiry.TenderSectionId,
            inquiry.DueDate, inquiry.RespondedAt, inquiry.ClosedAt,
            inquiry.CreatedAt
        );
    }
}

// ==================== Respond to Inquiry ====================
public record RespondToInquiryCommand(
    Guid InquiryId,
    string ResponseAr,
    string ResponseEn
) : IRequest<ApiResponse<InquiryDto>>;

public class RespondToInquiryCommandValidator : AbstractValidator<RespondToInquiryCommand>
{
    public RespondToInquiryCommandValidator()
    {
        RuleFor(x => x.InquiryId).NotEmpty();
        RuleFor(x => x.ResponseAr).NotEmpty();
        RuleFor(x => x.ResponseEn).NotEmpty();
    }
}

public class RespondToInquiryCommandHandler : IRequestHandler<RespondToInquiryCommand, ApiResponse<InquiryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public RespondToInquiryCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<InquiryDto>> Handle(RespondToInquiryCommand request, CancellationToken cancellationToken)
    {
        var inquiry = await _context.Inquiries
            .Include(i => i.Tender)
            .Include(i => i.SubmittedByUser)
            .Include(i => i.AssignedToUser)
            .FirstOrDefaultAsync(i => i.Id == request.InquiryId, cancellationToken);

        if (inquiry == null)
            return ApiResponse<InquiryDto>.Failure("Inquiry not found.");

        if (inquiry.Status == InquiryStatus.Closed)
            return ApiResponse<InquiryDto>.Failure("Cannot respond to a closed inquiry.");

        inquiry.ResponseAr = request.ResponseAr;
        inquiry.ResponseEn = request.ResponseEn;
        inquiry.Status = InquiryStatus.Responded;
        inquiry.RespondedAt = DateTime.UtcNow;
        inquiry.UpdatedAt = DateTime.UtcNow;
        inquiry.UpdatedBy = _currentUser.UserId;

        await CreateCommitteeCommandHandler.LogAuditAsync(_context, _currentUser, "InquiryResponded",
            $"Inquiry '{inquiry.SubjectEn}' responded",
            "Inquiry", inquiry.Id, null, inquiry, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var dto = CreateInquiryCommandHandler.MapToDto(inquiry, inquiry.Tender, inquiry.SubmittedByUser, inquiry.AssignedToUser);
        return ApiResponse<InquiryDto>.Success(dto, "Inquiry responded successfully.");
    }
}

// ==================== Close Inquiry ====================
public record CloseInquiryCommand(Guid InquiryId) : IRequest<ApiResponse<InquiryDto>>;

public class CloseInquiryCommandHandler : IRequestHandler<CloseInquiryCommand, ApiResponse<InquiryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CloseInquiryCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<InquiryDto>> Handle(CloseInquiryCommand request, CancellationToken cancellationToken)
    {
        var inquiry = await _context.Inquiries
            .Include(i => i.Tender)
            .Include(i => i.SubmittedByUser)
            .Include(i => i.AssignedToUser)
            .FirstOrDefaultAsync(i => i.Id == request.InquiryId, cancellationToken);

        if (inquiry == null)
            return ApiResponse<InquiryDto>.Failure("Inquiry not found.");

        inquiry.Status = InquiryStatus.Closed;
        inquiry.ClosedAt = DateTime.UtcNow;
        inquiry.UpdatedAt = DateTime.UtcNow;
        inquiry.UpdatedBy = _currentUser.UserId;

        await CreateCommitteeCommandHandler.LogAuditAsync(_context, _currentUser, "InquiryClosed",
            $"Inquiry '{inquiry.SubjectEn}' closed",
            "Inquiry", inquiry.Id, null, inquiry, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var dto = CreateInquiryCommandHandler.MapToDto(inquiry, inquiry.Tender, inquiry.SubmittedByUser, inquiry.AssignedToUser);
        return ApiResponse<InquiryDto>.Success(dto, "Inquiry closed successfully.");
    }
}

// ==================== Assign Inquiry ====================
public record AssignInquiryCommand(Guid InquiryId, Guid AssignedToUserId) : IRequest<ApiResponse<InquiryDto>>;

public class AssignInquiryCommandHandler : IRequestHandler<AssignInquiryCommand, ApiResponse<InquiryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AssignInquiryCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<InquiryDto>> Handle(AssignInquiryCommand request, CancellationToken cancellationToken)
    {
        var inquiry = await _context.Inquiries
            .Include(i => i.Tender)
            .Include(i => i.SubmittedByUser)
            .FirstOrDefaultAsync(i => i.Id == request.InquiryId, cancellationToken);

        if (inquiry == null)
            return ApiResponse<InquiryDto>.Failure("Inquiry not found.");

        var assignee = await _context.Users.FindAsync(new object[] { request.AssignedToUserId }, cancellationToken);
        if (assignee == null)
            return ApiResponse<InquiryDto>.Failure("Assigned user not found.");

        inquiry.AssignedToUserId = request.AssignedToUserId;
        inquiry.Status = InquiryStatus.UnderReview;
        inquiry.UpdatedAt = DateTime.UtcNow;
        inquiry.UpdatedBy = _currentUser.UserId;

        await CreateCommitteeCommandHandler.LogAuditAsync(_context, _currentUser, "InquiryAssigned",
            $"Inquiry '{inquiry.SubjectEn}' assigned to '{assignee.Email}'",
            "Inquiry", inquiry.Id, null, inquiry, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var dto = CreateInquiryCommandHandler.MapToDto(inquiry, inquiry.Tender, inquiry.SubmittedByUser, assignee);
        return ApiResponse<InquiryDto>.Success(dto, "Inquiry assigned successfully.");
    }
}

// ==================== Escalate Inquiry ====================
public record EscalateInquiryCommand(Guid InquiryId, Guid EscalatedToUserId, string Reason) : IRequest<ApiResponse<InquiryDto>>;

public class EscalateInquiryCommandHandler : IRequestHandler<EscalateInquiryCommand, ApiResponse<InquiryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public EscalateInquiryCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<InquiryDto>> Handle(EscalateInquiryCommand request, CancellationToken cancellationToken)
    {
        var inquiry = await _context.Inquiries
            .Include(i => i.Tender)
            .Include(i => i.SubmittedByUser)
            .FirstOrDefaultAsync(i => i.Id == request.InquiryId, cancellationToken);

        if (inquiry == null)
            return ApiResponse<InquiryDto>.Failure("Inquiry not found.");

        if (inquiry.Status == InquiryStatus.Closed)
            return ApiResponse<InquiryDto>.Failure("Cannot escalate a closed inquiry.");

        var escalateTo = await _context.Users.FindAsync(new object[] { request.EscalatedToUserId }, cancellationToken);
        if (escalateTo == null)
            return ApiResponse<InquiryDto>.Failure("Escalation target user not found.");

        inquiry.AssignedToUserId = request.EscalatedToUserId;
        inquiry.Status = InquiryStatus.Escalated;
        inquiry.Priority = InquiryPriority.Urgent;
        inquiry.UpdatedAt = DateTime.UtcNow;
        inquiry.UpdatedBy = _currentUser.UserId;

        // Create escalation task
        var task = new UserTask
        {
            OrganizationId = inquiry.OrganizationId,
            AssignedUserId = request.EscalatedToUserId,
            WorkflowInstanceId = Guid.Empty,
            WorkflowStepId = Guid.Empty,
            TitleAr = $"استفسار مُصعّد: {inquiry.SubjectAr}",
            TitleEn = $"Escalated Inquiry: {inquiry.SubjectEn}",
            DescriptionAr = $"تم تصعيد هذا الاستفسار. السبب: {request.Reason}",
            DescriptionEn = $"This inquiry has been escalated. Reason: {request.Reason}",
            Status = UserTaskStatus.Pending,
            Priority = TaskPriority.Critical,
            EntityId = inquiry.Id,
            EntityType = "Inquiry",
            DueDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };
        _context.UserTasks.Add(task);

        await CreateCommitteeCommandHandler.LogAuditAsync(_context, _currentUser, "InquiryEscalated",
            $"Inquiry '{inquiry.SubjectEn}' escalated to '{escalateTo.Email}'. Reason: {request.Reason}",
            "Inquiry", inquiry.Id, null, inquiry, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var dto = CreateInquiryCommandHandler.MapToDto(inquiry, inquiry.Tender, inquiry.SubmittedByUser, escalateTo);
        return ApiResponse<InquiryDto>.Success(dto, "Inquiry escalated successfully.");
    }
}

// ==================== Reopen Inquiry ====================
public record ReopenInquiryCommand(Guid InquiryId) : IRequest<ApiResponse<InquiryDto>>;

public class ReopenInquiryCommandHandler : IRequestHandler<ReopenInquiryCommand, ApiResponse<InquiryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ReopenInquiryCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<InquiryDto>> Handle(ReopenInquiryCommand request, CancellationToken cancellationToken)
    {
        var inquiry = await _context.Inquiries
            .Include(i => i.Tender)
            .Include(i => i.SubmittedByUser)
            .Include(i => i.AssignedToUser)
            .FirstOrDefaultAsync(i => i.Id == request.InquiryId, cancellationToken);

        if (inquiry == null)
            return ApiResponse<InquiryDto>.Failure("Inquiry not found.");

        if (inquiry.Status != InquiryStatus.Closed && inquiry.Status != InquiryStatus.Responded)
            return ApiResponse<InquiryDto>.Failure("Only closed or responded inquiries can be reopened.");

        inquiry.Status = InquiryStatus.Reopened;
        inquiry.ClosedAt = null;
        inquiry.UpdatedAt = DateTime.UtcNow;
        inquiry.UpdatedBy = _currentUser.UserId;

        await CreateCommitteeCommandHandler.LogAuditAsync(_context, _currentUser, "InquiryReopened",
            $"Inquiry '{inquiry.SubjectEn}' reopened",
            "Inquiry", inquiry.Id, null, inquiry, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var dto = CreateInquiryCommandHandler.MapToDto(inquiry, inquiry.Tender, inquiry.SubmittedByUser, inquiry.AssignedToUser);
        return ApiResponse<InquiryDto>.Success(dto, "Inquiry reopened successfully.");
    }
}

// ==================== Add Internal Note ====================
public record AddInquiryNoteCommand(Guid InquiryId, string NoteAr, string NoteEn) : IRequest<ApiResponse<InquiryDto>>;

public class AddInquiryNoteCommandHandler : IRequestHandler<AddInquiryNoteCommand, ApiResponse<InquiryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AddInquiryNoteCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<InquiryDto>> Handle(AddInquiryNoteCommand request, CancellationToken cancellationToken)
    {
        var inquiry = await _context.Inquiries
            .Include(i => i.Tender)
            .Include(i => i.SubmittedByUser)
            .Include(i => i.AssignedToUser)
            .FirstOrDefaultAsync(i => i.Id == request.InquiryId, cancellationToken);

        if (inquiry == null)
            return ApiResponse<InquiryDto>.Failure("Inquiry not found.");

        // Store internal notes in the inquiry (append to response fields as internal notes)
        var noteTimestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
        var notePrefix = $"[ملاحظة داخلية - {noteTimestamp}]: ";
        var notePrefixEn = $"[Internal Note - {noteTimestamp}]: ";

        inquiry.InternalNotesAr = string.IsNullOrEmpty(inquiry.InternalNotesAr)
            ? notePrefix + request.NoteAr
            : inquiry.InternalNotesAr + "\n" + notePrefix + request.NoteAr;

        inquiry.InternalNotesEn = string.IsNullOrEmpty(inquiry.InternalNotesEn)
            ? notePrefixEn + request.NoteEn
            : inquiry.InternalNotesEn + "\n" + notePrefixEn + request.NoteEn;

        inquiry.UpdatedAt = DateTime.UtcNow;
        inquiry.UpdatedBy = _currentUser.UserId;

        await CreateCommitteeCommandHandler.LogAuditAsync(_context, _currentUser, "InquiryNoteAdded",
            $"Internal note added to inquiry '{inquiry.SubjectEn}'",
            "Inquiry", inquiry.Id, null, inquiry, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var dto = CreateInquiryCommandHandler.MapToDto(inquiry, inquiry.Tender, inquiry.SubmittedByUser, inquiry.AssignedToUser);
        return ApiResponse<InquiryDto>.Success(dto, "Note added successfully.");
    }
}
