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
    Viewer = 7
}
