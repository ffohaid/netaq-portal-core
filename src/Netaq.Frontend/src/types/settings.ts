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
  providerType: string | number
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
// Updated to match actual API response field names (camelCase from C# PascalCase)

export interface ExecutiveDashboard {
  tenderStatistics: TenderStatisticsDto
  workflowStatistics: WorkflowStatisticsDto
  userStatistics: UserStatisticsDto
  evaluationStatistics: EvaluationStatisticsDto
  recentActivity: RecentActivityDto[]
}

export interface OperationalDashboard {
  taskDashboard: TaskDashboardDto
  myTenders: TenderSummaryDto[]
}

export interface CommitteeDashboard {
  myCommittees: CommitteeSummaryDto[]
  pendingEvaluations: PendingEvaluationDto[]
  pendingSignatures: PendingSignatureDto[]
}

export interface MonitoringDashboard {
  slaStatistics: SlaStatisticsDto
  escalatedTasks: EscalatedTaskDto[]
  auditCategories: AuditCategoryCountDto[]
  knowledgeBaseStats: KnowledgeBaseStatsDto
}

export interface TenderStatisticsDto {
  totalTenders: number
  draftCount: number
  pendingApprovalCount: number
  approvedCount: number
  evaluationInProgressCount: number
  evaluationCompletedCount: number
  archivedCount: number
  cancelledCount: number
  totalEstimatedValue: number
  tendersByType: TenderTypeCountDto[]
  tendersByMonth: MonthlyCountDto[]
}

export interface TenderTypeCountDto {
  tenderType: string
  count: number
}

export interface MonthlyCountDto {
  year: number
  month: number
  count: number
}

export interface WorkflowStatisticsDto {
  totalInstances: number
  activeCount: number
  completedCount: number
  rejectedCount: number
  averageCompletionDays: number
}

export interface UserStatisticsDto {
  totalUsers: number
  activeUsers: number
  invitedUsers: number
  suspendedUsers: number
  usersByRole: RoleCountDto[]
}

export interface RoleCountDto {
  role: string
  count: number
}

export interface EvaluationStatisticsDto {
  totalProposals: number
  compliancePassedCount: number
  complianceFailedCount: number
  technicalEvaluationCount: number
  financialEvaluationCount: number
  recommendedCount: number
  excludedCount: number
}

export interface RecentActivityDto {
  id: string
  actionType: string
  actionDescription: string
  timestamp: string
  userName: string
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
  referenceNumber: string
  status: string
  completionPercentage: number
  createdAt: string
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
  reportType: string
  tenderId: string
  createdAt: string
}

export interface CommitteeSummaryDto {
  committeeId: string
  committeeNameAr: string
  committeeNameEn: string
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

export interface KnowledgeBaseStatsDto {
  totalSources: number
  autoIndexedCount: number
  manualUploadCount: number
  indexedCount: number
  pendingCount: number
  failedCount: number
  totalChunks: number
  totalVectors: number
}

// ===== Sprint 4: Report Types =====

export interface TenderStatusReport {
  generatedAt: string
  totalTenders: number
  totalEstimatedValue: number
  statusBreakdown: { status: string; count: number; totalValue: number }[]
  typeBreakdown: { tenderType: string; count: number; totalValue: number }[]
  monthlyTrend: { year: number; month: number; count: number; totalValue: number }[]
  averageCompletionPercentage: number
  // Legacy aliases for backward compatibility
  byStatus?: Record<string, number>
  byType?: Record<string, number>
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
  periodFrom: string
  periodTo: string
  totalActions: number
  uniqueActiveUsers: number
  totalUsers: number
  actionsByCategory: { category: string; count: number }[]
  topActiveUsers: TopActiveUserDto[]
  dailyActivity: { date: string; count: number }[]
  // Legacy aliases
  activeInLast30Days?: number
  usersByRole?: Record<string, number>
}

export interface TopActiveUserDto {
  userId: string
  userName: string
  fullNameAr?: string
  fullNameEn?: string
  role?: string
  actionCount: number
  lastActiveAt?: string
}
