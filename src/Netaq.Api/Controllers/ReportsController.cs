using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Interfaces;
using Netaq.Application.Common.Models;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Api.Controllers;

/// <summary>
/// System reports and analytics endpoints.
/// Provides exportable reports for tender status, workflow performance, SLA compliance, etc.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
public class ReportsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ReportsController(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get tender status summary report.
    /// </summary>
    [HttpGet("tender-status")]
    public async Task<ActionResult<ApiResponse<TenderStatusReportDto>>> GetTenderStatusReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var orgId = _currentUser.OrganizationId.Value;
        var query = _context.Tenders.Where(t => t.OrganizationId == orgId);

        if (fromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(t => t.CreatedAt <= toDate.Value);

        var tenders = await query.ToListAsync();

        var report = new TenderStatusReportDto
        {
            GeneratedAt = DateTime.UtcNow,
            TotalTenders = tenders.Count,
            TotalEstimatedValue = tenders.Sum(t => t.EstimatedValue),
            StatusBreakdown = Enum.GetValues<TenderStatus>()
                .Select(status => new StatusBreakdownItem
                {
                    Status = status.ToString(),
                    Count = tenders.Count(t => t.Status == status),
                    TotalValue = tenders.Where(t => t.Status == status).Sum(t => t.EstimatedValue)
                })
                .Where(s => s.Count > 0)
                .ToList(),
            TypeBreakdown = tenders.GroupBy(t => t.TenderType)
                .Select(g => new TypeBreakdownItem
                {
                    TenderType = g.Key.ToString(),
                    Count = g.Count(),
                    TotalValue = g.Sum(t => t.EstimatedValue)
                })
                .OrderByDescending(x => x.Count)
                .ToList(),
            MonthlyTrend = tenders
                .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                .Select(g => new MonthlyTrendItem
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count(),
                    TotalValue = g.Sum(t => t.EstimatedValue)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList(),
            AverageCompletionPercentage = tenders.Any() 
                ? Math.Round(tenders.Average(t => (double)t.CompletionPercentage), 1) 
                : 0
        };

        return Ok(ApiResponse<TenderStatusReportDto>.Success(report));
    }

    /// <summary>
    /// Get workflow performance report.
    /// </summary>
    [HttpGet("workflow-performance")]
    public async Task<ActionResult<ApiResponse<WorkflowPerformanceReportDto>>> GetWorkflowPerformanceReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var orgId = _currentUser.OrganizationId.Value;
        var query = _context.WorkflowInstances.Where(w => w.OrganizationId == orgId);

        if (fromDate.HasValue)
            query = query.Where(w => w.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(w => w.CreatedAt <= toDate.Value);

        var instances = await query.ToListAsync();

        var completed = instances.Where(w => w.Status == WorkflowInstanceStatus.Completed && w.CompletedAt.HasValue).ToList();

        var report = new WorkflowPerformanceReportDto
        {
            GeneratedAt = DateTime.UtcNow,
            TotalInstances = instances.Count,
            CompletedCount = completed.Count,
            ActiveCount = instances.Count(w => w.Status == WorkflowInstanceStatus.Active),
            RejectedCount = instances.Count(w => w.Status == WorkflowInstanceStatus.Rejected),
            CancelledCount = instances.Count(w => w.Status == WorkflowInstanceStatus.Cancelled),
            AverageCompletionDays = completed.Any()
                ? Math.Round(completed.Average(w => (w.CompletedAt!.Value - w.CreatedAt).TotalDays), 1)
                : 0,
            MinCompletionDays = completed.Any()
                ? Math.Round(completed.Min(w => (w.CompletedAt!.Value - w.CreatedAt).TotalDays), 1)
                : 0,
            MaxCompletionDays = completed.Any()
                ? Math.Round(completed.Max(w => (w.CompletedAt!.Value - w.CreatedAt).TotalDays), 1)
                : 0,
            CompletionRate = instances.Any()
                ? Math.Round((double)completed.Count / instances.Count * 100, 1)
                : 0
        };

        return Ok(ApiResponse<WorkflowPerformanceReportDto>.Success(report));
    }

    /// <summary>
    /// Get SLA compliance report.
    /// </summary>
    [HttpGet("sla-compliance")]
    public async Task<ActionResult<ApiResponse<SlaComplianceReportDto>>> GetSlaComplianceReport()
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var orgId = _currentUser.OrganizationId.Value;

        var slaRecords = await _context.SlaTrackings
            .Where(s => s.WorkflowInstance.OrganizationId == orgId)
            .ToListAsync();

        var tasks = await _context.UserTasks
            .Where(t => _context.Users.Any(u => u.Id == t.AssignedUserId && u.OrganizationId == orgId))
            .ToListAsync();

        var report = new SlaComplianceReportDto
        {
            GeneratedAt = DateTime.UtcNow,
            TotalSlaRecords = slaRecords.Count,
            OnTrackCount = slaRecords.Count(s => s.Status == SlaStatus.OnTrack),
            AtRiskCount = slaRecords.Count(s => s.Status == SlaStatus.AtRisk),
            OverdueCount = slaRecords.Count(s => s.Status == SlaStatus.Overdue),
            ComplianceRate = slaRecords.Any()
                ? Math.Round((double)slaRecords.Count(s => s.Status == SlaStatus.OnTrack) / slaRecords.Count * 100, 1)
                : 100,
            TotalTasks = tasks.Count,
            EscalatedTaskCount = tasks.Count(t => t.Status == UserTaskStatus.Escalated),
            OverdueTaskCount = tasks.Count(t => t.SlaStatus == SlaStatus.Overdue),
            AverageTaskCompletionHours = tasks
                .Where(t => t.Status == UserTaskStatus.Completed && t.CompletedAt.HasValue)
                .Select(t => (t.CompletedAt!.Value - t.CreatedAt).TotalHours)
                .DefaultIfEmpty(0)
                .Average()
        };

        return Ok(ApiResponse<SlaComplianceReportDto>.Success(report));
    }

    /// <summary>
    /// Get user activity report.
    /// </summary>
    [HttpGet("user-activity")]
    public async Task<ActionResult<ApiResponse<UserActivityReportDto>>> GetUserActivityReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var orgId = _currentUser.OrganizationId.Value;
        
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var auditLogs = await _context.AuditLogs
            .IgnoreQueryFilters()
            .Where(a => a.OrganizationId == orgId && a.Timestamp >= from && a.Timestamp <= to)
            .ToListAsync();

        var users = await _context.Users
            .Where(u => u.OrganizationId == orgId)
            .ToListAsync();

        var report = new UserActivityReportDto
        {
            GeneratedAt = DateTime.UtcNow,
            PeriodFrom = from,
            PeriodTo = to,
            TotalActions = auditLogs.Count,
            UniqueActiveUsers = auditLogs.Where(a => a.UserId.HasValue).Select(a => a.UserId!.Value).Distinct().Count(),
            TotalUsers = users.Count,
            ActionsByCategory = auditLogs.GroupBy(a => a.ActionCategory)
                .Select(g => new CategoryActionCount
                {
                    Category = g.Key.ToString(),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList(),
            TopActiveUsers = auditLogs
                .Where(a => a.UserId.HasValue)
                .GroupBy(a => a.UserId!.Value)
                .Select(g => new UserActionCount
                {
                    UserId = g.Key,
                    UserName = users.FirstOrDefault(u => u.Id == g.Key)?.FullNameAr ?? "Unknown",
                    ActionCount = g.Count()
                })
                .OrderByDescending(x => x.ActionCount)
                .Take(10)
                .ToList(),
            DailyActivity = auditLogs
                .GroupBy(a => a.Timestamp.Date)
                .Select(g => new DailyActivityItem
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList()
        };

        return Ok(ApiResponse<UserActivityReportDto>.Success(report));
    }

    /// <summary>
    /// Get audit trail summary report.
    /// </summary>
    [HttpGet("audit-summary")]
    public async Task<ActionResult<ApiResponse<AuditSummaryReportDto>>> GetAuditSummaryReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        if (!_currentUser.OrganizationId.HasValue)
            return Unauthorized();

        var orgId = _currentUser.OrganizationId.Value;
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var logs = await _context.AuditLogs
            .IgnoreQueryFilters()
            .Where(a => a.OrganizationId == orgId && a.Timestamp >= from && a.Timestamp <= to)
            .ToListAsync();

        var report = new AuditSummaryReportDto
        {
            GeneratedAt = DateTime.UtcNow,
            PeriodFrom = from,
            PeriodTo = to,
            TotalEntries = logs.Count,
            EntriesByCategory = logs.GroupBy(a => a.ActionCategory)
                .Select(g => new AuditCategorySummary
                {
                    Category = g.Key.ToString(),
                    Count = g.Count(),
                    LastOccurrence = g.Max(a => a.Timestamp)
                })
                .OrderByDescending(x => x.Count)
                .ToList(),
            EntriesByDay = logs
                .GroupBy(a => a.Timestamp.Date)
                .Select(g => new DailyActivityItem
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList(),
            LatestSequenceNumber = logs.Any() ? logs.Max(a => a.SequenceNumber) : 0
        };

        return Ok(ApiResponse<AuditSummaryReportDto>.Success(report));
    }
}

// ===== Report DTOs =====

public class TenderStatusReportDto
{
    public DateTime GeneratedAt { get; set; }
    public int TotalTenders { get; set; }
    public decimal TotalEstimatedValue { get; set; }
    public List<StatusBreakdownItem> StatusBreakdown { get; set; } = new();
    public List<TypeBreakdownItem> TypeBreakdown { get; set; } = new();
    public List<MonthlyTrendItem> MonthlyTrend { get; set; } = new();
    public double AverageCompletionPercentage { get; set; }
}

public class StatusBreakdownItem
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalValue { get; set; }
}

public class TypeBreakdownItem
{
    public string TenderType { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalValue { get; set; }
}

public class MonthlyTrendItem
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Count { get; set; }
    public decimal TotalValue { get; set; }
}

public class WorkflowPerformanceReportDto
{
    public DateTime GeneratedAt { get; set; }
    public int TotalInstances { get; set; }
    public int CompletedCount { get; set; }
    public int ActiveCount { get; set; }
    public int RejectedCount { get; set; }
    public int CancelledCount { get; set; }
    public double AverageCompletionDays { get; set; }
    public double MinCompletionDays { get; set; }
    public double MaxCompletionDays { get; set; }
    public double CompletionRate { get; set; }
}

public class SlaComplianceReportDto
{
    public DateTime GeneratedAt { get; set; }
    public int TotalSlaRecords { get; set; }
    public int OnTrackCount { get; set; }
    public int AtRiskCount { get; set; }
    public int OverdueCount { get; set; }
    public double ComplianceRate { get; set; }
    public int TotalTasks { get; set; }
    public int EscalatedTaskCount { get; set; }
    public int OverdueTaskCount { get; set; }
    public double AverageTaskCompletionHours { get; set; }
}

public class UserActivityReportDto
{
    public DateTime GeneratedAt { get; set; }
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public int TotalActions { get; set; }
    public int UniqueActiveUsers { get; set; }
    public int TotalUsers { get; set; }
    public List<CategoryActionCount> ActionsByCategory { get; set; } = new();
    public List<UserActionCount> TopActiveUsers { get; set; } = new();
    public List<DailyActivityItem> DailyActivity { get; set; } = new();
}

public class CategoryActionCount
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class UserActionCount
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int ActionCount { get; set; }
}

public class DailyActivityItem
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class AuditSummaryReportDto
{
    public DateTime GeneratedAt { get; set; }
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public int TotalEntries { get; set; }
    public List<AuditCategorySummary> EntriesByCategory { get; set; } = new();
    public List<DailyActivityItem> EntriesByDay { get; set; } = new();
    public long LatestSequenceNumber { get; set; }
}

public class AuditCategorySummary
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime LastOccurrence { get; set; }
}
