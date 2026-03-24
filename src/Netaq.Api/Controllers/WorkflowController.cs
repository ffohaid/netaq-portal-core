using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Application.Workflows.Commands;
using Netaq.Application.Workflows.Queries;
using Netaq.Domain.Enums;
using Netaq.Infrastructure.Services;

namespace Netaq.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkflowController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditTrailService _auditTrailService;

    public WorkflowController(IMediator mediator, ICurrentUserService currentUser, IAuditTrailService auditTrailService)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _auditTrailService = auditTrailService;
    }

    /// <summary>
    /// Get all workflow templates for the current organization.
    /// </summary>
    [HttpGet("templates")]
    public async Task<ActionResult<ApiResponse<List<WorkflowTemplateDto>>>> GetTemplates()
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var query = new GetWorkflowTemplatesQuery(_currentUser.OrganizationId.Value);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get workflow template details with steps.
    /// </summary>
    [HttpGet("templates/{id:guid}")]
    public async Task<ActionResult<ApiResponse<WorkflowTemplateDetailDto>>> GetTemplateDetail(Guid id)
    {
        var query = new GetWorkflowTemplateDetailQuery(id);
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
            return NotFound(result);
        
        return Ok(result);
    }

    /// <summary>
    /// Create a new workflow template with steps.
    /// </summary>
    [HttpPost("templates")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateTemplate([FromBody] CreateWorkflowTemplateRequest request)
    {
        if (!_currentUser.OrganizationId.HasValue || !_currentUser.UserId.HasValue)
            return Unauthorized();

        var command = new CreateWorkflowTemplateCommand(
            _currentUser.OrganizationId.Value,
            _currentUser.UserId.Value,
            request.NameAr,
            request.NameEn,
            request.DescriptionAr,
            request.DescriptionEn,
            request.Steps);

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            await _auditTrailService.LogAsync(
                _currentUser.OrganizationId.Value, _currentUser.UserId.Value,
                AuditActionCategory.WorkflowAction, "WORKFLOW_TEMPLATE_CREATED",
                $"Workflow template created: {request.NameEn}",
                "WorkflowTemplate", result.Data,
                ipAddress: _currentUser.IpAddress,
                userAgent: _currentUser.UserAgent);
        }

        return result.IsSuccess ? CreatedAtAction(nameof(GetTemplateDetail), new { id = result.Data }, result) : BadRequest(result);
    }

    /// <summary>
    /// Start a new workflow instance from a template.
    /// </summary>
    [HttpPost("instances")]
    public async Task<ActionResult<ApiResponse<Guid>>> StartWorkflow([FromBody] StartWorkflowRequest request)
    {
        if (!_currentUser.OrganizationId.HasValue || !_currentUser.UserId.HasValue)
            return Unauthorized();

        var command = new StartWorkflowCommand(
            _currentUser.OrganizationId.Value,
            _currentUser.UserId.Value,
            request.WorkflowTemplateId,
            request.EntityId,
            request.EntityType);

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            await _auditTrailService.LogAsync(
                _currentUser.OrganizationId.Value, _currentUser.UserId.Value,
                AuditActionCategory.WorkflowAction, "WORKFLOW_STARTED",
                $"Workflow instance started from template {request.WorkflowTemplateId}",
                "WorkflowInstance", result.Data,
                ipAddress: _currentUser.IpAddress,
                userAgent: _currentUser.UserAgent);
        }

        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Take an action on a workflow step (approve, reject, delegate, etc.).
    /// </summary>
    [HttpPost("actions")]
    public async Task<ActionResult<ApiResponse<bool>>> TakeAction([FromBody] WorkflowActionRequest request)
    {
        if (!_currentUser.OrganizationId.HasValue || !_currentUser.UserId.HasValue)
            return Unauthorized();

        var command = new TakeWorkflowActionCommand(
            _currentUser.OrganizationId.Value,
            _currentUser.UserId.Value,
            request.WorkflowInstanceId,
            request.ActionType,
            request.Justification,
            request.DelegatedToUserId,
            request.Notes);

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            await _auditTrailService.LogAsync(
                _currentUser.OrganizationId.Value, _currentUser.UserId.Value,
                AuditActionCategory.WorkflowAction, $"WORKFLOW_{request.ActionType.ToString().ToUpper()}",
                $"Workflow action {request.ActionType} on instance {request.WorkflowInstanceId}",
                "WorkflowInstance", request.WorkflowInstanceId,
                ipAddress: _currentUser.IpAddress,
                userAgent: _currentUser.UserAgent);
        }

        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
