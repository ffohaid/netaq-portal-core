// ===== Sprint 3 - Evaluation Types =====

export type ProposalStatus =
  | 'Received'
  | 'CompliancePassed'
  | 'ComplianceFailed'
  | 'TechnicalEvaluationInProgress'
  | 'TechnicalEvaluationCompleted'
  | 'TechnicallyDisqualified'
  | 'FinancialEvaluationInProgress'
  | 'FinancialEvaluationCompleted'
  | 'Ranked'
  | 'Recommended'
  | 'Excluded';

export type ProposalFileCategory =
  | 'TechnicalOffer'
  | 'FinancialOffer'
  | 'ComplianceDocuments'
  | 'SupportingDocuments'
  | 'Other';

export type EvaluationReportType =
  | 'ComplianceInspection'
  | 'TechnicalEvaluation'
  | 'FinalEvaluation';

export type EvaluationReportStatus =
  | 'Draft'
  | 'PendingSignatures'
  | 'Signed'
  | 'Finalized'
  | 'Exported';

export interface Proposal {
  id: string;
  tenderId: string;
  vendorNameAr: string;
  vendorNameEn: string;
  vendorReferenceNumber: string;
  totalValue: number;
  status: ProposalStatus;
  receivedDate: string;
  passedComplianceCheck: boolean | null;
  complianceFailureReason: string | null;
  technicalScore: number | null;
  passedTechnicalEvaluation: boolean | null;
  financialScore: number | null;
  finalScore: number | null;
  finalRank: number | null;
  aiSummaryAr: string | null;
  aiSummaryEn: string | null;
  notes: string | null;
  files: ProposalFile[];
  createdAt: string;
}

export interface ProposalFile {
  id: string;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  category: ProposalFileCategory;
  isTextExtracted: boolean;
  createdAt: string;
}

export interface ComplianceChecklistItem {
  id: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  isMandatory: boolean;
  orderIndex: number;
  isDefault: boolean;
}

export interface ComplianceCheckResult {
  proposalId: string;
  vendorNameAr: string;
  vendorReferenceNumber: string;
  overallPassed: boolean;
  results: ComplianceItemResult[];
}

export interface ComplianceItemResult {
  checklistItemId: string;
  itemNameAr: string;
  itemNameEn: string;
  passed: boolean;
  failureReason: string | null;
  notes: string | null;
}

export interface EvaluationScore {
  id: string;
  proposalId: string;
  criteriaId: string;
  criteriaNameAr: string;
  criteriaNameEn: string;
  score: number;
  justification: string | null;
  aiSuggestedScore: number | null;
  aiJustification: string | null;
  isFinalized: boolean;
  finalizedScore: number | null;
}

export interface EvaluationSummary {
  proposalId: string;
  vendorNameAr: string;
  vendorReferenceNumber: string;
  criteriaSummaries: CriteriaSummary[];
  averageTotal: number;
  variance: number;
}

export interface CriteriaSummary {
  criteriaId: string;
  criteriaNameAr: string;
  averageScore: number;
  minScore: number;
  maxScore: number;
  variance: number;
  memberScores: MemberScore[];
}

export interface MemberScore {
  userId: string;
  userName: string;
  score: number;
  justification: string | null;
}

export interface FinalRanking {
  proposalId: string;
  vendorNameAr: string;
  vendorNameEn: string;
  vendorReferenceNumber: string;
  technicalScore: number;
  financialScore: number;
  finalScore: number;
  finalRank: number;
  status: string;
}

export interface EvaluationReport {
  id: string;
  tenderId: string;
  reportType: EvaluationReportType;
  titleAr: string;
  titleEn: string;
  referenceNumber: string;
  status: EvaluationReportStatus;
  contentHtml: string;
  pdfObjectKey: string | null;
  finalizedAt: string | null;
  aiAwardJustification: string | null;
  signatures: ReportSignature[];
  createdAt: string;
}

export interface ReportSignature {
  id: string;
  userId: string;
  userName: string;
  role: string;
  isSigned: boolean;
  signedAt: string | null;
  comments: string | null;
}

// ===== AI Result Types =====
export interface AiSummarizationResult {
  success: boolean;
  summaryAr: string;
  summaryEn: string;
  keyStrengths: string[];
  keyWeaknesses: string[];
  error: string | null;
}

export interface AiGapAnalysisResult {
  success: boolean;
  gaps: GapItem[];
  overallAssessmentAr: string;
  overallAssessmentEn: string;
  error: string | null;
}

export interface GapItem {
  requirementAr: string;
  gapDescriptionAr: string;
  severity: 'Critical' | 'Major' | 'Minor';
  recommendationAr: string | null;
}

export interface AiScoreSuggestion {
  criteriaId: string;
  criteriaNameAr: string;
  suggestedScore: number;
  justificationAr: string;
  justificationEn: string;
  confidence: number;
}

export interface FinancialResult {
  proposalId: string;
  vendorNameAr: string;
  technicalScore: number;
  financialValue: number;
  financialScore: number;
  finalScore: number;
  finalRank: number;
}

// ===== Request Types =====
export interface CreateProposalRequest {
  tenderId: string;
  vendorNameAr: string;
  vendorNameEn: string;
  vendorReferenceNumber: string;
  totalValue: number;
  receivedDate?: string;
  notes?: string;
}

export interface ComplianceCheckInput {
  checklistItemId: string;
  passed: boolean;
  failureReason?: string;
  notes?: string;
}

export interface ScoreInput {
  criteriaId: string;
  score: number;
  justification?: string;
}

export interface FinalizedScoreInput {
  criteriaId: string;
  finalScore: number;
  notes?: string;
}

export interface FinancialInput {
  proposalId: string;
  financialValue: number;
}
