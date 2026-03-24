namespace Netaq.Domain.Enums;

/// <summary>
/// Authentication provider types supported by the platform.
/// </summary>
public enum AuthProviderType
{
    Nafath = 1,       // SSO via OIDC/SAML
    ActiveDirectory = 2, // LDAP/SAML
    CustomAuth = 3    // Encrypted email invitations with OTP
}

/// <summary>
/// User status within the organization.
/// </summary>
public enum UserStatus
{
    Invited = 1,
    Active = 2,
    Suspended = 3,
    Deactivated = 4
}

/// <summary>
/// Workflow step execution types.
/// </summary>
public enum WorkflowStepType
{
    Sequential = 1,
    Parallel = 2,
    Conditional = 3
}

/// <summary>
/// Status of a workflow instance.
/// </summary>
public enum WorkflowInstanceStatus
{
    Active = 1,
    Completed = 2,
    Rejected = 3,
    Cancelled = 4
}

/// <summary>
/// Actions that can be taken on a workflow step.
/// </summary>
public enum WorkflowActionType
{
    Approve = 1,
    Reject = 2,
    ReturnForClarification = 3,
    Delegate = 4,
    Escalate = 5
}

/// <summary>
/// Status of a user task in the Unified Task Center.
/// </summary>
public enum UserTaskStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Rejected = 4,
    Delegated = 5,
    Escalated = 6,
    ReturnedForClarification = 7
}

/// <summary>
/// Task priority levels.
/// </summary>
public enum TaskPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// SLA status indicators (visual traffic light).
/// </summary>
public enum SlaStatus
{
    OnTrack = 1,    // Green
    AtRisk = 2,     // Yellow
    Overdue = 3     // Red
}

/// <summary>
/// Invitation status.
/// </summary>
public enum InvitationStatus
{
    Pending = 1,
    Accepted = 2,
    Expired = 3,
    Revoked = 4
}

/// <summary>
/// Audit log action categories.
/// </summary>
public enum AuditActionCategory
{
    Authentication = 1,
    UserManagement = 2,
    WorkflowAction = 3,
    TaskAction = 4,
    PermissionChange = 5,
    OrganizationSettings = 6,
    AiConfiguration = 7,
    SystemEvent = 8
}

/// <summary>
/// Committee types.
/// </summary>
public enum CommitteeType
{
    Permanent = 1,
    Temporary = 2
}

/// <summary>
/// Committee member roles within a committee.
/// </summary>
public enum CommitteeMemberRole
{
    Chair = 1,
    ViceChair = 2,
    Member = 3,
    Secretary = 4,
    Observer = 5
}

/// <summary>
/// AI provider types.
/// </summary>
public enum AiProviderType
{
    Gemini = 1,       // Cloud SaaS
    Ollama = 2,       // On-Premise
    Aya = 3,          // On-Premise
    OpenAI = 4        // Cloud
}

/// <summary>
/// Tender phases for permission matrix.
/// </summary>
public enum TenderPhase
{
    Drafting = 1,
    Review = 2,
    Approval = 3,
    Published = 4,
    EvaluationTechnical = 5,
    EvaluationFinancial = 6,
    Awarding = 7,
    Closed = 8
}

/// <summary>
/// Organization-level roles.
/// </summary>
public enum OrganizationRole
{
    SystemAdmin = 1,
    OrganizationAdmin = 2,
    DepartmentManager = 3,
    Coordinator = 4,
    CommitteeChair = 5,
    CommitteeMember = 6,
    LegalAdvisor = 7,
    Viewer = 8
}

/// <summary>
/// Tender lifecycle status (State Machine).
/// Draft → PendingApproval → Approved → EvaluationInProgress → EvaluationCompleted → Archived
/// Cancelled can be reached from any state.
/// </summary>
public enum TenderStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    EvaluationInProgress = 4,
    EvaluationCompleted = 5,
    Archived = 6,
    Cancelled = 7
}

/// <summary>
/// Tender type categories as per Saudi Government Procurement Law.
/// </summary>
public enum TenderType
{
    GeneralSupply = 1,
    PharmaceuticalSupply = 2,
    MedicalSupply = 3,
    MilitarySupply = 4,
    GeneralServices = 5,
    CateringServices = 6,
    CityCleaning = 7,
    BuildingMaintenance = 8,
    GeneralConsulting = 9,
    EngineeringDesign = 10,
    EngineeringSupervision = 11,
    GeneralConstruction = 12,
    RoadConstruction = 13,
    RoadMaintenance = 14,
    InformationTechnology = 15,
    FrameworkAgreementSupply = 16,
    FrameworkAgreementServices = 17,
    FrameworkAgreementConsulting = 18,
    RevenueSharing = 19,
    PerformanceBasedContract = 20,
    CapacityStudy = 21
}

/// <summary>
/// Booklet template categories (7 main categories for 21 templates).
/// </summary>
public enum TemplateCategory
{
    Supply = 1,
    Services = 2,
    Consulting = 3,
    Engineering = 4,
    InformationTechnology = 5,
    Construction = 6,
    SpecialModels = 7
}

/// <summary>
/// Standard booklet section types (8 doors/chapters).
/// </summary>
public enum BookletSectionType
{
    GeneralTermsAndConditions = 1,
    TechnicalScopeAndSpecifications = 2,
    QualificationRequirements = 3,
    EvaluationCriteria = 4,
    FinancialTerms = 5,
    ContractualTerms = 6,
    LocalContentRequirements = 7,
    AppendicesAndForms = 8
}

/// <summary>
/// Evaluation criteria type (Technical or Financial).
/// </summary>
public enum CriteriaType
{
    Technical = 1,
    Financial = 2
}

/// <summary>
/// Method used to create a tender booklet.
/// </summary>
public enum BookletCreationMethod
{
    FromTemplate = 1,
    AiExtraction = 2,
    ManualEntry = 3
}

/// <summary>
/// AI feature types for the drafting phase.
/// </summary>
public enum AiFeatureType
{
    SuggestCriteria = 1,
    LegalComplianceCheck = 2,
    BoilerplateGeneration = 3
}

/// <summary>
/// Document export format types.
/// </summary>
public enum ExportFormat
{
    Pdf = 1,
    Docx = 2
}

/// <summary>
/// Audit log action categories for tender operations.
/// </summary>
public enum TenderAuditAction
{
    TenderCreated = 1,
    TenderUpdated = 2,
    SectionUpdated = 3,
    CriteriaUpdated = 4,
    SubmittedForApproval = 5,
    Approved = 6,
    Rejected = 7,
    Cancelled = 8,
    Exported = 9,
    AiSuggestionRequested = 10,
    AiComplianceCheckRequested = 11,
    // Sprint 3 - Offer Evaluation
    ProposalUploaded = 12,
    ProposalReceiptClosed = 13,
    ComplianceCheckPerformed = 14,
    TechnicalScoreEntered = 15,
    TechnicalScoreFinalized = 16,
    FinancialScoreEntered = 17,
    FinalScoreCalculated = 18,
    EvaluationReportGenerated = 19,
    EvaluationReportSigned = 20,
    AiSummarizationRequested = 21,
    AiGapAnalysisRequested = 22,
    AiScoreSuggestionRequested = 23,
    AiComparisonMatrixRequested = 24,
    AiAwardJustificationRequested = 25
}

// ===== Sprint 3 Enums =====

/// <summary>
/// Proposal lifecycle status.
/// </summary>
public enum ProposalStatus
{
    Received = 1,
    CompliancePassed = 2,
    ComplianceFailed = 3,
    TechnicalEvaluationInProgress = 4,
    TechnicalEvaluationCompleted = 5,
    TechnicallyDisqualified = 6,
    FinancialEvaluationInProgress = 7,
    FinancialEvaluationCompleted = 8,
    Ranked = 9,
    Recommended = 10,
    Excluded = 11
}

/// <summary>
/// Category of proposal file.
/// </summary>
public enum ProposalFileCategory
{
    TechnicalOffer = 1,
    FinancialOffer = 2,
    ComplianceDocuments = 3,
    SupportingDocuments = 4,
    Other = 5
}

/// <summary>
/// Evaluation report types (3 types as per PRD).
/// </summary>
public enum EvaluationReportType
{
    ComplianceInspection = 1,
    TechnicalEvaluation = 2,
    FinalEvaluation = 3
}

/// <summary>
/// Evaluation report status.
/// </summary>
public enum EvaluationReportStatus
{
    Draft = 1,
    PendingSignatures = 2,
    Signed = 3,
    Finalized = 4,
    Exported = 5
}

/// <summary>
/// AI evaluation feature types (Sprint 3).
/// </summary>
public enum AiEvaluationFeatureType
{
    ProposalSummarization = 1,
    AutoMapping = 2,
    GapAnalysis = 3,
    ScoreSuggestion = 4,
    ComparisonMatrix = 5,
    AwardJustificationDraft = 6
}
