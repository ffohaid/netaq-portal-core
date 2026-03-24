using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;
using Netaq.Infrastructure.Services;

namespace Netaq.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditTrailService _auditTrailService;

    public AuditController(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IAuditTrailService auditTrailService)
    {
        _context = context;
        _currentUser = currentUser;
        _auditTrailService = auditTrailService;
    }

    /// <summary>
    /// Get audit logs for the current organization.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<AuditLogDto>>>> GetAuditLogs(
        [FromQuery] AuditActionCategory? category = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var query = _context.AuditLogs
            .IgnoreQueryFilters()
            .Where(a => a.OrganizationId == _currentUser.OrganizationId.Value);

        if (category.HasValue)
            query = query.Where(a => a.ActionCategory == category.Value);

        if (fromDate.HasValue)
            query = query.Where(a => a.Timestamp >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.Timestamp <= toDate.Value);

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id,
                a.ActionCategory,
                a.ActionType,
                a.ActionDescription,
                a.EntityType,
                a.EntityId,
                a.UserId,
                a.Timestamp,
                a.IpAddress,
                a.Hash,
                a.SequenceNumber))
            .ToListAsync();

        return Ok(ApiResponse<PaginatedResponse<AuditLogDto>>.Success(new PaginatedResponse<AuditLogDto>
        {
            Items = logs,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        }));
    }

    /// <summary>
    /// Verify the integrity of the audit trail chain.
    /// </summary>
    [HttpGet("verify-integrity")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ApiResponse<AuditIntegrityResult>>> VerifyIntegrity()
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var isValid = await _auditTrailService.VerifyChainIntegrityAsync(_currentUser.OrganizationId.Value);

        return Ok(ApiResponse<AuditIntegrityResult>.Success(new AuditIntegrityResult(
            isValid,
            isValid ? "Audit trail integrity verified successfully." : "Audit trail integrity check FAILED. Possible tampering detected.")));
    }
}

public record AuditLogDto(
    Guid Id,
    AuditActionCategory ActionCategory,
    string ActionType,
    string ActionDescription,
    string? EntityType,
    Guid? EntityId,
    Guid? UserId,
    DateTime Timestamp,
    string? IpAddress,
    string Hash,
    long SequenceNumber);

public record AuditIntegrityResult(bool IsValid, string Message);
