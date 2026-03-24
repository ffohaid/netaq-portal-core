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
