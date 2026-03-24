// ===== Sprint 4: Settings Types =====

export interface OrganizationSettings {
  id: string
  nameAr: string
  nameEn: string
  descriptionAr?: string
  descriptionEn?: string
  address?: string
  phone?: string
  email?: string
  website?: string
  logoUrl?: string
  showPlatformLogo: boolean
  activeAuthProvider: string
  isOtpEnabled: boolean
  ssoEndpoint?: string
  ssoClientId?: string
  adDomain?: string
  adLdapUrl?: string
}

export interface AiConfiguration {
  id: string
  providerType: string
  providerName: string
  isActive: boolean
  endpoint: string
  modelName: string
  temperature: number
  maxTokens: number
  vectorDbEndpoint?: string
  embeddingModel?: string
  chunkSize: number
  hasApiKey: boolean
}

export interface AiTestResult {
  success: boolean
  message: string
  responseTime?: number
}

export interface KnowledgeSource {
  id: string
  titleAr: string
  titleEn: string
  descriptionAr?: string
  descriptionEn?: string
  sourceType: string
  indexingStatus: string
  documentCount: number
  lastIndexedAt?: string
  fileUrl?: string
  createdAt: string
}

export interface KnowledgeSourceList {
  sources: KnowledgeSource[]
  totalDocuments: number
  totalIndexed: number
  totalPending: number
}

export interface SystemSetting {
  id: string
  category: string
  settingKey: string
  settingValue: string
  labelAr?: string
  labelEn?: string
  dataType: string
  isEditable: boolean
}

// ===== Sprint 4: Dashboard Types =====

export interface ExecutiveDashboard {
  tenderStats: TenderStatsDto
  workflowStats: WorkflowStatsDto
  userStats: UserStatsDto
  recentAuditLogs: AuditLogSummary[]
  tendersByType: TenderByTypeDto[]
  tendersByMonth: TenderByMonthDto[]
}

export interface OperationalDashboard {
  taskStats: TaskDashboardDto
  myTenders: TenderSummaryDto[]
  pendingEvaluations: PendingEvaluationDto[]
  pendingSignatures: PendingSignatureDto[]
}

export interface CommitteeDashboard {
  activeCommittees: CommitteeInfoDto[]
  pendingEvaluations: PendingEvaluationDto[]
  pendingSignatures: PendingSignatureDto[]
}

export interface MonitoringDashboard {
  slaStats: SlaStatisticsDto
  escalatedTasks: EscalatedTaskDto[]
  auditCategories: AuditCategoryCountDto[]
}

export interface TenderStatsDto {
  totalTenders: number
  draftCount: number
  pendingApprovalCount: number
  approvedCount: number
  evaluationInProgressCount: number
  completedCount: number
  cancelledCount: number
}

export interface WorkflowStatsDto {
  totalWorkflows: number
  activeCount: number
  completedCount: number
  rejectedCount: number
}

export interface UserStatsDto {
  totalUsers: number
  activeUsers: number
  invitedUsers: number
}

export interface AuditLogSummary {
  id: string
  actionType: string
  actionDescription: string
  timestamp: string
}

export interface TenderByTypeDto {
  tenderType: string
  count: number
}

export interface TenderByMonthDto {
  month: string
  count: number
}

export interface TaskDashboardDto {
  totalTasks: number
  pendingCount: number
  inProgressCount: number
  completedCount: number
  overdueCount: number
  atRiskCount: number
  escalatedCount: number
  upcomingDeadlines: UpcomingDeadlineDto[]
}

export interface UpcomingDeadlineDto {
  taskId: string
  titleAr: string
  titleEn: string
  dueDate: string
  priority: string
  slaStatus: string
}

export interface TenderSummaryDto {
  id: string
  titleAr: string
  titleEn: string
  status: string
  completionPercentage: number
}

export interface PendingEvaluationDto {
  taskId: string
  titleAr: string
  titleEn: string
  dueDate: string
  priority: string
  slaStatus: string
}

export interface PendingSignatureDto {
  reportId: string
  reportTitleAr: string
  reportTitleEn: string
  reportType: string
  createdAt: string
}

export interface CommitteeInfoDto {
  committeeId: string
  nameAr: string
  nameEn: string
  memberRole: string
  committeeType: string
}

export interface SlaStatisticsDto {
  totalTracked: number
  onTrackCount: number
  atRiskCount: number
  overdueCount: number
  complianceRate: number
}

export interface EscalatedTaskDto {
  taskId: string
  titleAr: string
  titleEn: string
  assignedUserId: string
  dueDate: string
  escalatedAt: string
}

export interface AuditCategoryCountDto {
  category: string
  count: number
}

// ===== Sprint 4: Report Types =====

export interface TenderStatusReport {
  generatedAt: string
  totalTenders: number
  byStatus: Record<string, number>
  byType: Record<string, number>
  averageCompletionPercentage: number
}

export interface SlaComplianceReport {
  generatedAt: string
  totalSlaRecords: number
  onTrackCount: number
  atRiskCount: number
  overdueCount: number
  complianceRate: number
  totalTasks: number
  escalatedTaskCount: number
  overdueTaskCount: number
  averageTaskCompletionHours: number
}

export interface UserActivityReport {
  generatedAt: string
  totalUsers: number
  activeInLast30Days: number
  usersByRole: Record<string, number>
  topActiveUsers: TopActiveUserDto[]
}

export interface TopActiveUserDto {
  userId: string
  fullNameAr: string
  fullNameEn: string
  role: string
  actionCount: number
  lastActiveAt: string
}
