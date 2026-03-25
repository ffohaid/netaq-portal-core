using Microsoft.EntityFrameworkCore;
using Netaq.Domain.Entities;

namespace Netaq.Domain.Interfaces;

/// <summary>
/// Application database context interface for dependency inversion.
/// </summary>
public interface IApplicationDbContext
{
    // Sprint 1 entities
    DbSet<Organization> Organizations { get; }
    DbSet<User> Users { get; }
    DbSet<PermissionMatrix> PermissionMatrices { get; }
    DbSet<Invitation> Invitations { get; }
    DbSet<WorkflowTemplate> WorkflowTemplates { get; }
    DbSet<WorkflowStep> WorkflowSteps { get; }
    DbSet<WorkflowInstance> WorkflowInstances { get; }
    DbSet<WorkflowAction> WorkflowActions { get; }
    DbSet<UserTask> UserTasks { get; }
    DbSet<SlaTracking> SlaTrackings { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<AiConfiguration> AiConfigurations { get; }
    
    // Sprint 2 entities - Tender & Booklet Drafting
    DbSet<Tender> Tenders { get; }
    DbSet<TenderSection> TenderSections { get; }
    DbSet<TenderCriteria> TenderCriteria { get; }
    DbSet<BookletTemplate> BookletTemplates { get; }
    DbSet<BookletTemplateSection> BookletTemplateSections { get; }
    DbSet<Committee> Committees { get; }
    DbSet<CommitteeMember> CommitteeMembers { get; }
    
    // Sprint 3 entities - Offer Evaluation & AI
    DbSet<Proposal> Proposals { get; }
    DbSet<ProposalFile> ProposalFiles { get; }
    DbSet<ComplianceChecklist> ComplianceChecklists { get; }
    DbSet<ComplianceCheckResult> ComplianceCheckResults { get; }
    DbSet<EvaluationScore> EvaluationScores { get; }
    DbSet<EvaluationReport> EvaluationReports { get; }
    DbSet<ReportSignature> ReportSignatures { get; }
    
    // Sprint 4 entities - Settings, Knowledge Base, Dashboards
    DbSet<KnowledgeSource> KnowledgeSources { get; }
    DbSet<SystemSetting> SystemSettings { get; }
    
    // Sprint 6 entities - Inquiries
    DbSet<Inquiry> Inquiries { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
