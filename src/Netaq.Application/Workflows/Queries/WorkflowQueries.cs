using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Models;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Workflows.Queries;

// --- DTOs ---
public record WorkflowTemplateDto(
    Guid Id,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    bool IsActive,
    int Version,
    int StepCount,
    DateTime CreatedAt);

public record WorkflowStepDto(
    Guid Id,
    string NameAr,
    string NameEn,
    int Order,
    WorkflowStepType StepType,
    OrganizationRole RequiredRole,
    int SlaDurationHours,
    string? ParallelGroupId,
    string? ConditionExpression);

public record WorkflowTemplateDetailDto(
    Guid Id,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    bool IsActive,
    int Version,
    List<WorkflowStepDto> Steps,
    DateTime CreatedAt);

public record WorkflowInstanceDto(
    Guid Id,
    Guid WorkflowTemplateId,
    string TemplateNameAr,
    string TemplateNameEn,
    WorkflowInstanceStatus Status,
    string? CurrentStepNameAr,
    string? CurrentStepNameEn,
    Guid? EntityId,
    string? EntityType,
    DateTime CreatedAt,
    DateTime? CompletedAt);

// --- Get All Templates Query ---
public record GetWorkflowTemplatesQuery(Guid OrganizationId) : IRequest<ApiResponse<List<WorkflowTemplateDto>>>;

public class GetWorkflowTemplatesQueryHandler : IRequestHandler<GetWorkflowTemplatesQuery, ApiResponse<List<WorkflowTemplateDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetWorkflowTemplatesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<WorkflowTemplateDto>>> Handle(GetWorkflowTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await _context.WorkflowTemplates
            .Include(t => t.Steps)
            .Where(t => t.OrganizationId == request.OrganizationId)
            .Select(t => new WorkflowTemplateDto(
                t.Id,
                t.NameAr,
                t.NameEn,
                t.DescriptionAr,
                t.DescriptionEn,
                t.IsActive,
                t.Version,
                t.Steps.Count,
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<WorkflowTemplateDto>>.Success(templates);
    }
}

// --- Get Template Detail Query ---
public record GetWorkflowTemplateDetailQuery(Guid TemplateId) : IRequest<ApiResponse<WorkflowTemplateDetailDto>>;

public class GetWorkflowTemplateDetailQueryHandler : IRequestHandler<GetWorkflowTemplateDetailQuery, ApiResponse<WorkflowTemplateDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetWorkflowTemplateDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<WorkflowTemplateDetailDto>> Handle(GetWorkflowTemplateDetailQuery request, CancellationToken cancellationToken)
    {
        var template = await _context.WorkflowTemplates
            .Include(t => t.Steps.OrderBy(s => s.Order))
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken);

        if (template == null)
            return ApiResponse<WorkflowTemplateDetailDto>.Failure("Template not found.");

        var dto = new WorkflowTemplateDetailDto(
            template.Id,
            template.NameAr,
            template.NameEn,
            template.DescriptionAr,
            template.DescriptionEn,
            template.IsActive,
            template.Version,
            template.Steps.Select(s => new WorkflowStepDto(
                s.Id, s.NameAr, s.NameEn, s.Order, s.StepType,
                s.RequiredRole, s.SlaDurationHours,
                s.ParallelGroupId, s.ConditionExpression)).ToList(),
            template.CreatedAt);

        return ApiResponse<WorkflowTemplateDetailDto>.Success(dto);
    }
}
