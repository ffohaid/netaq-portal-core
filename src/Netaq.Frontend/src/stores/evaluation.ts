import { defineStore } from 'pinia';
import { ref } from 'vue';
import axios from 'axios';
import type {
  Proposal,
  ComplianceChecklistItem,
  ComplianceCheckResult,
  EvaluationScore,
  EvaluationSummary,
  FinalRanking,
  EvaluationReport,
  AiSummarizationResult,
  AiGapAnalysisResult,
  FinancialResult,
  CreateProposalRequest,
  ComplianceCheckInput,
  ScoreInput,
  FinalizedScoreInput,
  FinancialInput
} from '@/types/evaluation';

const api = axios.create({ baseURL: '/api' });

export const useEvaluationStore = defineStore('evaluation', () => {
  // ===== State =====
  const proposals = ref<Proposal[]>([]);
  const currentProposal = ref<Proposal | null>(null);
  const complianceChecklist = ref<ComplianceChecklistItem[]>([]);
  const myScores = ref<EvaluationScore[]>([]);
  const evaluationSummary = ref<EvaluationSummary | null>(null);
  const finalRankings = ref<FinalRanking[]>([]);
  const reports = ref<EvaluationReport[]>([]);
  const currentReport = ref<EvaluationReport | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const totalCount = ref(0);

  // AI results
  const aiSummary = ref<AiSummarizationResult | null>(null);
  const aiGapAnalysis = ref<AiGapAnalysisResult | null>(null);

  // ===== Proposal Actions =====
  async function fetchProposals(tenderId: string, status?: string, page = 1, pageSize = 20) {
    loading.value = true;
    error.value = null;
    try {
      const params = new URLSearchParams({ pageNumber: String(page), pageSize: String(pageSize) });
      if (status) params.set('status', status);
      const { data } = await api.get(`/tenders/${tenderId}/proposals?${params}`);
      proposals.value = data.data.items;
      totalCount.value = data.data.totalCount;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Failed to fetch proposals';
    } finally {
      loading.value = false;
    }
  }

  async function fetchProposalDetail(tenderId: string, proposalId: string) {
    loading.value = true;
    error.value = null;
    try {
      const { data } = await api.get(`/tenders/${tenderId}/proposals/${proposalId}`);
      currentProposal.value = data.data;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Failed to fetch proposal';
    } finally {
      loading.value = false;
    }
  }

  async function createProposal(tenderId: string, request: CreateProposalRequest) {
    loading.value = true;
    error.value = null;
    try {
      const { data } = await api.post(`/tenders/${tenderId}/proposals`, request);
      proposals.value.unshift(data.data);
      return data.data;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Failed to create proposal';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function uploadProposalFile(tenderId: string, proposalId: string, file: File, category: string) {
    loading.value = true;
    error.value = null;
    try {
      const formData = new FormData();
      formData.append('file', file);
      const { data } = await api.post(
        `/tenders/${tenderId}/proposals/${proposalId}/files?category=${category}`,
        formData,
        { headers: { 'Content-Type': 'multipart/form-data' } }
      );
      return data.data;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Failed to upload file';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function closeProposalReceipt(tenderId: string) {
    loading.value = true;
    error.value = null;
    try {
      const { data } = await api.post(`/tenders/${tenderId}/proposals/close-receipt`);
      return data.data;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Failed to close receipt';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  // ===== Compliance Actions =====
  async function fetchComplianceChecklist(tenderId: string) {
    loading.value = true;
    try {
      const { data } = await api.get(`/tenders/${tenderId}/proposals/compliance-checklist`);
      complianceChecklist.value = data.data;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Failed to fetch checklist';
    } finally {
      loading.value = false;
    }
  }

  async function submitComplianceCheck(tenderId: string, proposalId: string, items: ComplianceCheckInput[]) {
    loading.value = true;
    error.value = null;
    try {
      const { data } = await api.post(
        `/tenders/${tenderId}/evaluation/proposals/${proposalId}/compliance`,
        { items }
      );
      return data.data as ComplianceCheckResult;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Failed to submit compliance check';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  // ===== Technical Evaluation Actions =====
  async function submitTechnicalScores(tenderId: string, proposalId: string, scores: ScoreInput[]) {
    loading.value = true;
    error.value = null;
    try {
      const { data } = await api.post(
        `/tenders/${tenderId}/evaluation/proposals/${proposalId}/technical-scores`,
        { scores }
      );
      myScores.value = data.data;
      return data.data;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Failed to submit scores';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function fetchMyScores(tenderId: string, proposalId: string) {
    loading.value = true;
    try {
      const { data } = await api.get(
        `/tenders/${tenderId}/evaluation/proposals/${proposalId}/my-scores`
      );
      myScores.value = data.data;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Failed to fetch scores';
    } finally {
      loading.value = false;
    }
  }

  async function fetchEvaluationSummary(tenderId: string, proposalId: string) {
    loading.value = true;
    try {
      const { data } = await api.get(
        `/tenders/${tenderId}/evaluation/proposals/${proposalId}/summary`
      );
      evaluationSummary.value = data.data;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Failed to fetch summary';
    } finally {
      loading.value = false;
    }
  }

  async function finalizeTechnicalScores(tenderId: string, proposalId: string, finalizedScores: FinalizedScoreInput[]) {
    loading.value = true;
    error.value = null;
    try {
      const { data } = await api.post(
        `/tenders/${tenderId}/evaluation/proposals/${proposalId}/finalize-technical`,
        { finalizedScores }
      );
      return data.data;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Failed to finalize scores';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  // ===== Financial Evaluation Actions =====
  async function submitFinancialEvaluation(tenderId: string, financialValues: FinancialInput[]) {
    loading.value = true;
    error.value = null;
    try {
      const { data } = await api.post(
        `/tenders/${tenderId}/evaluation/financial`,
        { financialValues }
      );
      finalRankings.value = data.data;
      return data.data as FinancialResult[];
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Failed to submit financial evaluation';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function fetchFinalRankings(tenderId: string) {
    loading.value = true;
    try {
      const { data } = await api.get(`/tenders/${tenderId}/evaluation/rankings`);
      finalRankings.value = data.data;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Failed to fetch rankings';
    } finally {
      loading.value = false;
    }
  }

  // ===== Report Actions =====
  async function generateReport(tenderId: string, reportType: string) {
    loading.value = true;
    error.value = null;
    try {
      const { data } = await api.post(
        `/tenders/${tenderId}/evaluation/reports/generate`,
        { reportType }
      );
      currentReport.value = data.data;
      return data.data as EvaluationReport;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Failed to generate report';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function signReport(tenderId: string, reportId: string, comments?: string) {
    loading.value = true;
    error.value = null;
    try {
      const { data } = await api.post(
        `/tenders/${tenderId}/evaluation/reports/${reportId}/sign`,
        { comments }
      );
      return data.data;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'Failed to sign report';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  // ===== AI Actions =====
  async function aiExtractText(proposalId: string) {
    loading.value = true;
    try {
      const { data } = await api.post(`/ai/evaluation/proposals/${proposalId}/extract-text`);
      return data.data;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'AI text extraction failed';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function aiSummarizeProposal(proposalId: string) {
    loading.value = true;
    try {
      const { data } = await api.post(`/ai/evaluation/proposals/${proposalId}/summarize`);
      aiSummary.value = data.data;
      return data.data;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'AI summarization failed';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function aiAnalyzeGaps(proposalId: string) {
    loading.value = true;
    try {
      const { data } = await api.post(`/ai/evaluation/proposals/${proposalId}/gap-analysis`);
      aiGapAnalysis.value = data.data;
      return data.data;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'AI gap analysis failed';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function aiSuggestScores(proposalId: string) {
    loading.value = true;
    try {
      const { data } = await api.post(`/ai/evaluation/proposals/${proposalId}/suggest-scores`);
      return data.data;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'AI score suggestion failed';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function aiComparisonMatrix(tenderId: string) {
    loading.value = true;
    try {
      const { data } = await api.post(`/ai/evaluation/tenders/${tenderId}/comparison-matrix`);
      return data.data;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'AI comparison matrix failed';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function aiAwardJustification(tenderId: string) {
    loading.value = true;
    try {
      const { data } = await api.post(`/ai/evaluation/tenders/${tenderId}/award-justification`);
      return data.data;
    } catch (e: any) {
      error.value = e.response?.data?.message || 'AI award justification failed';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  return {
    // State
    proposals, currentProposal, complianceChecklist, myScores,
    evaluationSummary, finalRankings, reports, currentReport,
    loading, error, totalCount, aiSummary, aiGapAnalysis,
    // Proposal actions
    fetchProposals, fetchProposalDetail, createProposal,
    uploadProposalFile, closeProposalReceipt,
    // Compliance actions
    fetchComplianceChecklist, submitComplianceCheck,
    // Technical evaluation actions
    submitTechnicalScores, fetchMyScores, fetchEvaluationSummary, finalizeTechnicalScores,
    // Financial evaluation actions
    submitFinancialEvaluation, fetchFinalRankings,
    // Report actions
    generateReport, signReport,
    // AI actions
    aiExtractText, aiSummarizeProposal, aiAnalyzeGaps,
    aiSuggestScores, aiComparisonMatrix, aiAwardJustification
  };
});
