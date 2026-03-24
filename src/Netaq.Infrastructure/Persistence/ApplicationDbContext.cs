using Microsoft.EntityFrameworkCore;
using Netaq.Domain.Entities;
using Netaq.Domain.Interfaces;

namespace Netaq.Infrastructure.Persistence;

/// <summary>
/// Main application database context with multi-tenancy support via RLS interceptors.
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Sprint 1 entities
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<PermissionMatrix> PermissionMatrices => Set<PermissionMatrix>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<WorkflowTemplate> WorkflowTemplates => Set<WorkflowTemplate>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowAction> WorkflowActions => Set<WorkflowAction>();
    public DbSet<UserTask> UserTasks => Set<UserTask>();
    public DbSet<SlaTracking> SlaTrackings => Set<SlaTracking>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AiConfiguration> AiConfigurations => Set<AiConfiguration>();
    
    // Sprint 2 entities - Tender & Booklet Drafting
    public DbSet<Tender> Tenders => Set<Tender>();
    public DbSet<TenderSection> TenderSections => Set<TenderSection>();
    public DbSet<TenderCriteria> TenderCriteria => Set<TenderCriteria>();
    public DbSet<BookletTemplate> BookletTemplates => Set<BookletTemplate>();
    public DbSet<BookletTemplateSection> BookletTemplateSections => Set<BookletTemplateSection>();
    public DbSet<Committee> Committees => Set<Committee>();
    public DbSet<CommitteeMember> CommitteeMembers => Set<CommitteeMember>();
    
    // Sprint 3 entities - Offer Evaluation & AI
    public DbSet<Proposal> Proposals => Set<Proposal>();
    public DbSet<ProposalFile> ProposalFiles => Set<ProposalFile>();
    public DbSet<ComplianceChecklist> ComplianceChecklists => Set<ComplianceChecklist>();
    public DbSet<ComplianceCheckResult> ComplianceCheckResults => Set<ComplianceCheckResult>();
    public DbSet<EvaluationScore> EvaluationScores => Set<EvaluationScore>();
    public DbSet<EvaluationReport> EvaluationReports => Set<EvaluationReport>();
    public DbSet<ReportSignature> ReportSignatures => Set<ReportSignature>();

    // Sprint 4 entities - Settings, Knowledge Base, Dashboards
    public DbSet<KnowledgeSource> KnowledgeSources => Set<KnowledgeSource>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
