// ===== API Response Types =====
export interface ApiResponse<T> {
  isSuccess: boolean
  data?: T
  error?: string
  errors?: string[]
}

export interface PaginatedResponse<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

// ===== Auth Types =====
export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  requiresOtp: boolean
  accessToken?: string
  refreshToken?: string
  message?: string
}

export interface TokenResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
}

export interface UserProfile {
  id: string
  fullNameAr: string
  fullNameEn: string
  email: string
  phone?: string
  jobTitleAr?: string
  jobTitleEn?: string
  departmentAr?: string
  departmentEn?: string
  role: string
  locale: string
  avatarUrl?: string
  organizationNameAr: string
  organizationNameEn: string
  organizationLogoUrl?: string
  lastLoginAt?: string
}

// ===== Task Types =====
export type UserTaskStatus = 'Pending' | 'InProgress' | 'Completed' | 'Rejected' | 'Delegated' | 'Escalated' | 'ReturnedForClarification'
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Critical'
export type SlaStatus = 'OnTrack' | 'AtRisk' | 'Overdue'

export interface UserTask {
  id: string
  titleAr: string
  titleEn: string
  descriptionAr?: string
  descriptionEn?: string
  status: UserTaskStatus
  priority: TaskPriority
  slaStatus: SlaStatus
  dueDate: string
  createdAt: string
  completedAt?: string
  entityId?: string
  entityType?: string
  workflowInstanceId: string
  workflowStepId: string
  assignedUserNameAr?: string
  assignedUserNameEn?: string
}

export interface TaskStatistics {
  totalTasks: number
  pendingTasks: number
  completedTasks: number
  overdueTasks: number
  escalatedTasks: number
}

// ===== Workflow Types =====
export type WorkflowStepType = 'Sequential' | 'Parallel' | 'Conditional'
export type WorkflowInstanceStatus = 'Active' | 'Completed' | 'Rejected' | 'Cancelled'
export type WorkflowActionType = 'Approve' | 'Reject' | 'ReturnForClarification' | 'Delegate' | 'Escalate'

export interface WorkflowTemplate {
  id: string
  nameAr: string
  nameEn: string
  descriptionAr?: string
  descriptionEn?: string
  isActive: boolean
  version: number
  stepCount: number
  createdAt: string
}

export interface WorkflowStep {
  id: string
  nameAr: string
  nameEn: string
  order: number
  stepType: WorkflowStepType
  requiredRole: string
  slaDurationHours: number
  parallelGroupId?: string
  conditionExpression?: string
}

export interface WorkflowTemplateDetail {
  id: string
  nameAr: string
  nameEn: string
  descriptionAr?: string
  descriptionEn?: string
  isActive: boolean
  version: number
  steps: WorkflowStep[]
  createdAt: string
}

// ===== Audit Types =====
export interface AuditLog {
  id: string
  actionCategory: string
  actionType: string
  actionDescription: string
  entityType?: string
  entityId?: string
  userId?: string
  timestamp: string
  ipAddress?: string
  hash: string
  sequenceNumber: number
}

export interface AuditIntegrityResult {
  isValid: boolean
  message: string
}

// ===== Invitation Types =====
export interface SendInvitationRequest {
  email: string
  fullNameAr?: string
  fullNameEn?: string
  assignedRole: string
}

// ===== Sprint 2: Tender Types =====
export type TenderType =
  | 'GeneralSupply' | 'PharmaceuticalSupply' | 'MedicalSupply' | 'MilitarySupply'
  | 'GeneralServices' | 'CateringServices' | 'CityCleaning' | 'BuildingMaintenance'
  | 'GeneralConsulting' | 'EngineeringDesign' | 'EngineeringSupervision'
  | 'GeneralConstruction' | 'RoadConstruction' | 'RoadMaintenance'
  | 'InformationTechnology'
  | 'FrameworkAgreementSupply' | 'FrameworkAgreementServices' | 'FrameworkAgreementConsulting'
  | 'RevenueSharing' | 'PerformanceBasedContract' | 'CapacityStudy'

export type TenderStatus = 'Draft' | 'PendingApproval' | 'Approved' | 'EvaluationInProgress' | 'EvaluationCompleted' | 'Archived' | 'Cancelled'

export type BookletCreationMethod = 'FromTemplate' | 'AiExtraction' | 'ManualEntry'

export type BookletSectionType =
  | 'GeneralTermsAndConditions' | 'TechnicalScopeAndSpecifications'
  | 'QualificationRequirements' | 'EvaluationCriteria'
  | 'FinancialTerms' | 'ContractualTerms'
  | 'LocalContentRequirements' | 'AppendicesAndForms'

export type CriteriaType = 'Technical' | 'Financial'

export type TemplateCategory =
  | 'Supply' | 'Services' | 'Consulting' | 'Engineering'
  | 'InformationTechnology' | 'Construction' | 'SpecialModels'

export interface Tender {
  id: string
  titleAr: string
  titleEn: string
  referenceNumber: string
  descriptionAr?: string
  descriptionEn?: string
  tenderType: TenderType
  estimatedValue: number
  status: TenderStatus
  creationMethod: BookletCreationMethod
  bookletTemplateId?: string
  submissionOpenDate?: string
  submissionCloseDate?: string
  projectStartDate?: string
  projectEndDate?: string
  completionPercentage: number
  technicalWeight: number
  financialWeight: number
  createdAt: string
  createdBy?: string
}

export interface TenderDetail extends Tender {
  sections: TenderSection[]
  criteria: TenderCriteria[]
}

export interface TenderSection {
  id: string
  sectionType: BookletSectionType
  titleAr: string
  titleEn: string
  contentHtml?: string
  completionPercentage: number
  orderIndex: number
  isAiReviewed: boolean
  lastAutoSavedAt?: string
}

export interface TenderCriteria {
  id: string
  parentId?: string
  nameAr: string
  nameEn: string
  descriptionAr?: string
  descriptionEn?: string
  criteriaType: CriteriaType
  weight: number
  passingThreshold?: number
  orderIndex: number
  isAiSuggested: boolean
  children: TenderCriteria[]
}

// ===== Sprint 2: Template Types =====
export interface BookletTemplate {
  id: string
  nameAr: string
  nameEn: string
  category: TemplateCategory
  applicableTenderType: TenderType
  descriptionAr?: string
  descriptionEn?: string
  isActive: boolean
  version: string
  sectionCount: number
}

export interface BookletTemplateSection {
  id: string
  sectionType: BookletSectionType
  titleAr: string
  titleEn: string
  defaultContentHtml?: string
  orderIndex: number
  guidanceNotesAr?: string
  guidanceNotesEn?: string
}

export interface BookletTemplateDetail extends Omit<BookletTemplate, 'sectionCount'> {
  sections: BookletTemplateSection[]
}

// ===== Sprint 2: AI Types =====
export interface AiSuggestion {
  content: string
  provider: string
  model: string
  confidenceScore: number
}

export interface ComplianceIssue {
  sectionTitle: string
  issue: string
  suggestion: string
  severity: 'High' | 'Medium' | 'Low'
}

export interface AiComplianceCheck {
  isCompliant: boolean
  issues: ComplianceIssue[]
  summary: string
  provider: string
  model: string
}

export interface AiCriteriaSuggestion {
  suggestedCriteria: TenderCriteria[]
  rationale: string
  provider: string
  model: string
}

// ===== Sprint 2: Create Tender Request =====
export interface CreateTenderRequest {
  titleAr: string
  titleEn: string
  descriptionAr?: string
  descriptionEn?: string
  tenderType: TenderType
  estimatedValue: number
  creationMethod: BookletCreationMethod
  bookletTemplateId?: string
  submissionOpenDate?: string
  submissionCloseDate?: string
  projectStartDate?: string
  projectEndDate?: string
  technicalWeight: number
  financialWeight: number
}

export interface UpdateSectionRequest {
  sectionId: string
  contentHtml?: string
  completionPercentage: number
}

export interface SaveCriteriaRequest {
  tenderId: string
  criteria: CriteriaItem[]
}

export interface CriteriaItem {
  id?: string
  parentId?: string
  nameAr: string
  nameEn: string
  descriptionAr?: string
  descriptionEn?: string
  criteriaType: CriteriaType
  weight: number
  passingThreshold?: number
  orderIndex: number
  children?: CriteriaItem[]
}
