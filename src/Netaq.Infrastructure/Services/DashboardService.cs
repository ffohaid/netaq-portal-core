using Microsoft.EntityFrameworkCore;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Infrastructure.Services;

/// <summary>
/// Dashboard service interface for role-based analytics and KPIs.
/// </summary>
public interface IDashboardService
{
    Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(Guid organizationId);
    Task<OperationalDashboardDto> GetOperationalDashboardAsync(Guid organizationId, Guid userId);
    Task<CommitteeDashboardDto> GetCommitteeDashboardAsync(Guid organizationId, Guid userId);
    Task<MonitoringDashboardDto> GetMonitoringDashboardAsync(Guid organizationId);
}

/// <summary>
/// Dashboard service implementation with real-time analytics from database.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _context;

    public DashboardService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(Guid organizationId)
    {
        // Tender statistics
        var tenders = await _context.Tenders
            .Where(t => t.OrganizationId == organizationId)
            .ToListAsync();

        var tenderStats = new TenderStatisticsDto
        {
            TotalTenders = tenders.Count,
            DraftCount = tenders.Count(t => t.Status == TenderStatus.Draft),
            PendingApprovalCount = tenders.Count(t => t.Status == TenderStatus.PendingApproval),
            ApprovedCount = tenders.Count(t => t.Status == TenderStatus.Approved),
            EvaluationInProgressCount = tenders.Count(t => t.Status == TenderStatus.EvaluationInProgress),
            EvaluationCompletedCount = tenders.Count(t => t.Status == TenderStatus.EvaluationCompleted),
            ArchivedCount = tenders.Count(t => t.Status == TenderStatus.Archived),
            CancelledCount = tenders.Count(t => t.Status == TenderStatus.Cancelled),
            TotalEstimatedValue = tenders.Sum(t => t.EstimatedValue),
            TendersByType = tenders.GroupBy(t => t.TenderType)
                .Select(g => new TenderTypeCountDto { TenderType = g.Key.ToString(), Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList(),
            TendersByMonth = tenders
                .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                .Select(g => new MonthlyCountDto 
                { 
                    Year = g.Key.Year, 
                    Month = g.Key.Month, 
                    Count = g.Count() 
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList()
        };

        // Workflow statistics
        var workflows = await _context.WorkflowInstances
            .Where(w => w.OrganizationId == organizationId)
            .ToListAsync();

        var workflowStats = new WorkflowStatisticsDto
        {
            TotalInstances = workflows.Count,
            ActiveCount = workflows.Count(w => w.Status == WorkflowInstanceStatus.Active),
            CompletedCount = workflows.Count(w => w.Status == WorkflowInstanceStatus.Completed),
            RejectedCount = workflows.Count(w => w.Status == WorkflowInstanceStatus.Rejected),
            AverageCompletionDays = workflows
                .Where(w => w.Status == WorkflowInstanceStatus.Completed && w.CompletedAt.HasValue)
                .Select(w => (w.CompletedAt!.Value - w.CreatedAt).TotalDays)
                .DefaultIfEmpty(0)
                .Average()
        };

        // User statistics
        var users = await _context.Users
            .Where(u => u.OrganizationId == organizationId)
            .ToListAsync();

        var userStats = new UserStatisticsDto
        {
            TotalUsers = users.Count,
            ActiveUsers = users.Count(u => u.Status == UserStatus.Active),
            InvitedUsers = users.Count(u => u.Status == UserStatus.Invited),
            SuspendedUsers = users.Count(u => u.Status == UserStatus.Suspended),
            UsersByRole = users.GroupBy(u => u.Role)
                .Select(g => new RoleCountDto { Role = g.Key.ToString(), Count = g.Count() })
                .ToList()
        };

        // Recent activity (last 10 audit logs)
        var recentActivity = await _context.AuditLogs
            .IgnoreQueryFilters()
            .Where(a => a.OrganizationId == organizationId)
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .Select(a => new RecentActivityDto
            {
                Id = a.Id,
                ActionType = a.ActionType,
                ActionDescription = a.ActionDescription,
                Timestamp = a.Timestamp,
                UserName = a.User != null ? a.User.FullNameAr : "النظام"
            })
            .ToListAsync();

        // Evaluation statistics
        var proposals = await _context.Proposals
            .Where(p => p.Tender.OrganizationId == organizationId)
            .ToListAsync();

        var evaluationStats = new EvaluationStatisticsDto
        {
            TotalProposals = proposals.Count,
            CompliancePassedCount = proposals.Count(p => p.Status == ProposalStatus.CompliancePassed),
            ComplianceFailedCount = proposals.Count(p => p.Status == ProposalStatus.ComplianceFailed),
            TechnicalEvaluationCount = proposals.Count(p => 
                p.Status == ProposalStatus.TechnicalEvaluationInProgress || 
                p.Status == ProposalStatus.TechnicalEvaluationCompleted),
            FinancialEvaluationCount = proposals.Count(p => 
                p.Status == ProposalStatus.FinancialEvaluationInProgress || 
                p.Status == ProposalStatus.FinancialEvaluationCompleted),
            RecommendedCount = proposals.Count(p => p.Status == ProposalStatus.Recommended),
            ExcludedCount = proposals.Count(p => p.Status == ProposalStatus.Excluded)
        };

        return new ExecutiveDashboardDto
        {
            TenderStatistics = tenderStats,
            WorkflowStatistics = workflowStats,
            UserStatistics = userStats,
            EvaluationStatistics = evaluationStats,
            RecentActivity = recentActivity
        };
    }

    public async Task<OperationalDashboardDto> GetOperationalDashboardAsync(Guid organizationId, Guid userId)
    {
        // My tasks
        var myTasks = await _context.UserTasks
            .Where(t => t.AssignedUserId == userId)
            .ToListAsync();

        var taskStats = new TaskDashboardDto
        {
            TotalTasks = myTasks.Count,
            PendingCount = myTasks.Count(t => t.Status == UserTaskStatus.Pending),
            InProgressCount = myTasks.Count(t => t.Status == UserTaskStatus.InProgress),
            CompletedCount = myTasks.Count(t => t.Status == UserTaskStatus.Completed),
            OverdueCount = myTasks.Count(t => t.SlaStatus == SlaStatus.Overdue),
            AtRiskCount = myTasks.Count(t => t.SlaStatus == SlaStatus.AtRisk),
            EscalatedCount = myTasks.Count(t => t.Status == UserTaskStatus.Escalated),
            UpcomingDeadlines = myTasks
                .Where(t => t.Status == UserTaskStatus.Pending || t.Status == UserTaskStatus.InProgress)
                .Where(t => t.DueDate <= DateTime.UtcNow.AddDays(3))
                .OrderBy(t => t.DueDate)
                .Take(5)
                .Select(t => new UpcomingDeadlineDto
                {
                    TaskId = t.Id,
                    TitleAr = t.TitleAr,
                    TitleEn = t.TitleEn,
                    DueDate = t.DueDate,
                    Priority = t.Priority,
                    SlaStatus = t.SlaStatus
                })
                .ToList()
        };

        // My tenders (created by me or assigned to me)
        var myTenders = await _context.Tenders
            .Where(t => t.OrganizationId == organizationId && t.CreatedBy == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .Select(t => new TenderSummaryDto
            {
                Id = t.Id,
                TitleAr = t.TitleAr,
                TitleEn = t.TitleEn,
                ReferenceNumber = t.ReferenceNumber,
                Status = t.Status,
                CompletionPercentage = t.CompletionPercentage,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return new OperationalDashboardDto
        {
            TaskDashboard = taskStats,
            MyTenders = myTenders
        };
    }

    public async Task<CommitteeDashboardDto> GetCommitteeDashboardAsync(Guid organizationId, Guid userId)
    {
        // Committees where user is a member
        var myCommittees = await _context.CommitteeMembers
            .Where(cm => cm.UserId == userId)
            .Include(cm => cm.Committee)
            .Select(cm => new CommitteeSummaryDto
            {
                CommitteeId = cm.CommitteeId,
                CommitteeNameAr = cm.Committee.NameAr,
                CommitteeNameEn = cm.Committee.NameEn,
                MemberRole = cm.Role,
                CommitteeType = cm.Committee.Type
            })
            .ToListAsync();

        // Pending evaluation tasks for this user
        var pendingEvaluations = await _context.UserTasks
            .Where(t => t.AssignedUserId == userId && 
                   (t.Status == UserTaskStatus.Pending || t.Status == UserTaskStatus.InProgress))
            .OrderBy(t => t.DueDate)
            .Take(10)
            .Select(t => new PendingEvaluationDto
            {
                TaskId = t.Id,
                TitleAr = t.TitleAr,
                TitleEn = t.TitleEn,
                DueDate = t.DueDate,
                Priority = t.Priority,
                SlaStatus = t.SlaStatus
            })
            .ToListAsync();

        // Evaluation reports pending signatures
        var pendingSignatures = await _context.ReportSignatures
            .Where(rs => rs.SignedByUserId == userId && !rs.IsSigned)
            .Include(rs => rs.EvaluationReport)
            .Select(rs => new PendingSignatureDto
            {
                ReportId = rs.EvaluationReportId,
                ReportType = rs.EvaluationReport.ReportType,
                TenderId = rs.EvaluationReport.TenderId,
                CreatedAt = rs.EvaluationReport.CreatedAt
            })
            .ToListAsync();

        return new CommitteeDashboardDto
        {
            MyCommittees = myCommittees,
            PendingEvaluations = pendingEvaluations,
            PendingSignatures = pendingSignatures
        };
    }

    public async Task<MonitoringDashboardDto> GetMonitoringDashboardAsync(Guid organizationId)
    {
        // SLA tracking overview
        var slaRecords = await _context.SlaTrackings
            .Where(s => s.WorkflowInstance.OrganizationId == organizationId)
            .ToListAsync();

        var slaStats = new SlaStatisticsDto
        {
            TotalTracked = slaRecords.Count,
            OnTrackCount = slaRecords.Count(s => s.Status == SlaStatus.OnTrack),
            AtRiskCount = slaRecords.Count(s => s.Status == SlaStatus.AtRisk),
            OverdueCount = slaRecords.Count(s => s.Status == SlaStatus.Overdue),
            ComplianceRate = slaRecords.Count > 0 
                ? Math.Round((double)slaRecords.Count(s => s.Status == SlaStatus.OnTrack) / slaRecords.Count * 100, 1)
                : 100.0
        };

        // Escalation summary
        var escalatedTasks = await _context.UserTasks
            .Where(t => t.Status == UserTaskStatus.Escalated)
            .Where(t => _context.Users.Any(u => u.Id == t.AssignedUserId && u.OrganizationId == organizationId))
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .Take(10)
            .Select(t => new EscalatedTaskDto
            {
                TaskId = t.Id,
                TitleAr = t.TitleAr,
                TitleEn = t.TitleEn,
                AssignedUserId = t.AssignedUserId,
                DueDate = t.DueDate,
                EscalatedAt = t.UpdatedAt ?? t.CreatedAt
            })
            .ToListAsync();

        // Audit trail statistics (last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var auditStats = await _context.AuditLogs
            .IgnoreQueryFilters()
            .Where(a => a.OrganizationId == organizationId && a.Timestamp >= thirtyDaysAgo)
            .GroupBy(a => a.ActionCategory)
            .Select(g => new AuditCategoryCountDto
            {
                Category = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        // Knowledge base statistics
        var kbStats = await GetKnowledgeBaseStatsAsync(organizationId);

        return new MonitoringDashboardDto
        {
            SlaStatistics = slaStats,
            EscalatedTasks = escalatedTasks,
            AuditStatistics = auditStats,
            KnowledgeBaseStatistics = kbStats
        };
    }

    private async Task<KnowledgeBaseStatsDto> GetKnowledgeBaseStatsAsync(Guid organizationId)
    {
        var sources = await _context.KnowledgeSources
            .Where(ks => ks.OrganizationId == organizationId)
            .ToListAsync();

        return new KnowledgeBaseStatsDto
        {
            TotalSources = sources.Count,
            AutoIndexedCount = sources.Count(s => s.SourceType == KnowledgeSourceType.AutoIndexed),
            ManualUploadCount = sources.Count(s => s.SourceType == KnowledgeSourceType.ManualUpload),
            IndexedCount = sources.Count(s => s.IndexingStatus == KnowledgeIndexingStatus.Completed),
            PendingCount = sources.Count(s => s.IndexingStatus == KnowledgeIndexingStatus.Pending),
            FailedCount = sources.Count(s => s.IndexingStatus == KnowledgeIndexingStatus.Failed),
            TotalChunks = sources.Sum(s => s.TotalChunks),
            TotalVectors = sources.Sum(s => s.TotalVectors)
        };
    }
}

// ===== DTOs =====

public class ExecutiveDashboardDto
{
    public TenderStatisticsDto TenderStatistics { get; set; } = new();
    public WorkflowStatisticsDto WorkflowStatistics { get; set; } = new();
    public UserStatisticsDto UserStatistics { get; set; } = new();
    public EvaluationStatisticsDto EvaluationStatistics { get; set; } = new();
    public List<RecentActivityDto> RecentActivity { get; set; } = new();
}

public class OperationalDashboardDto
{
    public TaskDashboardDto TaskDashboard { get; set; } = new();
    public List<TenderSummaryDto> MyTenders { get; set; } = new();
}

public class CommitteeDashboardDto
{
    public List<CommitteeSummaryDto> MyCommittees { get; set; } = new();
    public List<PendingEvaluationDto> PendingEvaluations { get; set; } = new();
    public List<PendingSignatureDto> PendingSignatures { get; set; } = new();
}

public class MonitoringDashboardDto
{
    public SlaStatisticsDto SlaStatistics { get; set; } = new();
    public List<EscalatedTaskDto> EscalatedTasks { get; set; } = new();
    public List<AuditCategoryCountDto> AuditStatistics { get; set; } = new();
    public KnowledgeBaseStatsDto KnowledgeBaseStatistics { get; set; } = new();
}

public class TenderStatisticsDto
{
    public int TotalTenders { get; set; }
    public int DraftCount { get; set; }
    public int PendingApprovalCount { get; set; }
    public int ApprovedCount { get; set; }
    public int EvaluationInProgressCount { get; set; }
    public int EvaluationCompletedCount { get; set; }
    public int ArchivedCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal TotalEstimatedValue { get; set; }
    public List<TenderTypeCountDto> TendersByType { get; set; } = new();
    public List<MonthlyCountDto> TendersByMonth { get; set; } = new();
}

public class TenderTypeCountDto
{
    public string TenderType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class MonthlyCountDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Count { get; set; }
}

public class WorkflowStatisticsDto
{
    public int TotalInstances { get; set; }
    public int ActiveCount { get; set; }
    public int CompletedCount { get; set; }
    public int RejectedCount { get; set; }
    public double AverageCompletionDays { get; set; }
}

public class UserStatisticsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InvitedUsers { get; set; }
    public int SuspendedUsers { get; set; }
    public List<RoleCountDto> UsersByRole { get; set; } = new();
}

public class RoleCountDto
{
    public string Role { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class EvaluationStatisticsDto
{
    public int TotalProposals { get; set; }
    public int CompliancePassedCount { get; set; }
    public int ComplianceFailedCount { get; set; }
    public int TechnicalEvaluationCount { get; set; }
    public int FinancialEvaluationCount { get; set; }
    public int RecommendedCount { get; set; }
    public int ExcludedCount { get; set; }
}

public class RecentActivityDto
{
    public Guid Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ActionDescription { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string UserName { get; set; } = string.Empty;
}

public class TaskDashboardDto
{
    public int TotalTasks { get; set; }
    public int PendingCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int OverdueCount { get; set; }
    public int AtRiskCount { get; set; }
    public int EscalatedCount { get; set; }
    public List<UpcomingDeadlineDto> UpcomingDeadlines { get; set; } = new();
}

public class UpcomingDeadlineDto
{
    public Guid TaskId { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public TaskPriority Priority { get; set; }
    public SlaStatus SlaStatus { get; set; }
}

public class TenderSummaryDto
{
    public Guid Id { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public TenderStatus Status { get; set; }
    public int CompletionPercentage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CommitteeSummaryDto
{
    public Guid CommitteeId { get; set; }
    public string CommitteeNameAr { get; set; } = string.Empty;
    public string CommitteeNameEn { get; set; } = string.Empty;
    public CommitteeMemberRole MemberRole { get; set; }
    public CommitteeType CommitteeType { get; set; }
}

public class PendingEvaluationDto
{
    public Guid TaskId { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public TaskPriority Priority { get; set; }
    public SlaStatus SlaStatus { get; set; }
}

public class PendingSignatureDto
{
    public Guid ReportId { get; set; }
    public EvaluationReportType ReportType { get; set; }
    public Guid TenderId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SlaStatisticsDto
{
    public int TotalTracked { get; set; }
    public int OnTrackCount { get; set; }
    public int AtRiskCount { get; set; }
    public int OverdueCount { get; set; }
    public double ComplianceRate { get; set; }
}

public class EscalatedTaskDto
{
    public Guid TaskId { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public Guid AssignedUserId { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime EscalatedAt { get; set; }
}

public class AuditCategoryCountDto
{
    public AuditActionCategory Category { get; set; }
    public int Count { get; set; }
}

public class KnowledgeBaseStatsDto
{
    public int TotalSources { get; set; }
    public int AutoIndexedCount { get; set; }
    public int ManualUploadCount { get; set; }
    public int IndexedCount { get; set; }
    public int PendingCount { get; set; }
    public int FailedCount { get; set; }
    public int TotalChunks { get; set; }
    public int TotalVectors { get; set; }
}
