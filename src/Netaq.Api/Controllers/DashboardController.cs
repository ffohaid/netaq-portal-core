using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Domain.Enums;
using Netaq.Infrastructure.Services;

namespace Netaq.Api.Controllers;

/// <summary>
/// Role-based dashboard endpoints providing specialized views for different user roles.
/// Executive (Admin), Operational (Manager/Coordinator), Committee, and Monitoring dashboards.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ICurrentUserService _currentUser;

    public DashboardController(
        IDashboardService dashboardService,
        ICurrentUserService currentUser)
    {
        _dashboardService = dashboardService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get the appropriate dashboard based on the current user's role.
    /// Automatically determines which dashboard type to return.
    /// </summary>
    [HttpGet("my-dashboard")]
    public async Task<ActionResult<ApiResponse<object>>> GetMyDashboard()
    {
        if (!_currentUser.OrganizationId.HasValue || !_currentUser.UserId.HasValue)
            return Unauthorized();

        var orgId = _currentUser.OrganizationId.Value;
        var userId = _currentUser.UserId.Value;
        var roleStr = _currentUser.Role;
        var role = Enum.TryParse<OrganizationRole>(roleStr, out var parsedRole) ? parsedRole : (OrganizationRole?)null;

        object dashboard;
        string dashboardType;

        switch (role)
        {
            case OrganizationRole.SystemAdmin:
            case OrganizationRole.OrganizationAdmin:
                dashboard = await _dashboardService.GetExecutiveDashboardAsync(orgId);
                dashboardType = "Executive";
                break;
            case OrganizationRole.DepartmentManager:
            case OrganizationRole.Coordinator:
                dashboard = await _dashboardService.GetOperationalDashboardAsync(orgId, userId);
                dashboardType = "Operational";
                break;
            case OrganizationRole.CommitteeChair:
            case OrganizationRole.CommitteeMember:
                dashboard = await _dashboardService.GetCommitteeDashboardAsync(orgId, userId);
                dashboardType = "Committee";
                break;
            default:
                dashboard = await _dashboardService.GetOperationalDashboardAsync(orgId, userId);
                dashboardType = "Operational";
                break;
        }

        return Ok(ApiResponse<object>.Success(new { type = dashboardType, data = dashboard }));
    }

    /// <summary>
    /// Get executive dashboard (SystemAdmin/OrganizationAdmin only).
    /// </summary>
    [HttpGet("executive")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult<ApiResponse<ExecutiveDashboardDto>>> GetExecutiveDashboard()
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var result = await _dashboardService.GetExecutiveDashboardAsync(_currentUser.OrganizationId.Value);
        return Ok(ApiResponse<ExecutiveDashboardDto>.Success(result));
    }

    /// <summary>
    /// Get operational dashboard for the current user.
    /// </summary>
    [HttpGet("operational")]
    public async Task<ActionResult<ApiResponse<OperationalDashboardDto>>> GetOperationalDashboard()
    {
        if (!_currentUser.OrganizationId.HasValue || !_currentUser.UserId.HasValue)
            return Unauthorized();

        var result = await _dashboardService.GetOperationalDashboardAsync(
            _currentUser.OrganizationId.Value, _currentUser.UserId.Value);
        return Ok(ApiResponse<OperationalDashboardDto>.Success(result));
    }

    /// <summary>
    /// Get committee dashboard for the current user.
    /// </summary>
    [HttpGet("committee")]
    public async Task<ActionResult<ApiResponse<CommitteeDashboardDto>>> GetCommitteeDashboard()
    {
        if (!_currentUser.OrganizationId.HasValue || !_currentUser.UserId.HasValue)
            return Unauthorized();

        var result = await _dashboardService.GetCommitteeDashboardAsync(
            _currentUser.OrganizationId.Value, _currentUser.UserId.Value);
        return Ok(ApiResponse<CommitteeDashboardDto>.Success(result));
    }

    /// <summary>
    /// Get monitoring dashboard (SystemAdmin only).
    /// SLA compliance, escalations, audit statistics, and knowledge base health.
    /// </summary>
    [HttpGet("monitoring")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ApiResponse<MonitoringDashboardDto>>> GetMonitoringDashboard()
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var result = await _dashboardService.GetMonitoringDashboardAsync(_currentUser.OrganizationId.Value);
        return Ok(ApiResponse<MonitoringDashboardDto>.Success(result));
    }
}
