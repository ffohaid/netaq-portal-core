<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useEvaluationStore } from '@/stores/evaluation';
import api from '@/services/api';

const { t, locale } = useI18n({ useScope: 'global' });
const router = useRouter();
const store = useEvaluationStore();

const isAr = computed(() => locale.value === 'ar');

// State
const loading = ref(false);
const tenders = ref<any[]>([]);
const selectedTenderId = ref<string | null>(null);
const selectedTender = ref<any>(null);
const proposals = ref<any[]>([]);
const committees = ref<any[]>([]);
const evaluationReports = ref<any[]>([]);
const activePhase = ref<'select' | 'proposals' | 'compliance' | 'technical' | 'financial' | 'reports'>('select');
const error = ref<string | null>(null);

// Fetch tenders that are in evaluation-ready status
async function fetchTenders() {
  loading.value = true;
  error.value = null;
  try {
    const res = await api.get('/api/tenders', { params: { pageSize: 100 } });
    if (res.data?.isSuccess) {
      tenders.value = (res.data.data?.items || res.data.data || []);
    }
  } catch (e: any) {
    error.value = e.message;
  } finally {
    loading.value = false;
  }
}

// Fetch proposals for selected tender
async function fetchProposals() {
  if (!selectedTenderId.value) return;
  loading.value = true;
  try {
    const res = await api.get(`/api/tenders/${selectedTenderId.value}/proposals`, { params: { pageSize: 100 } });
    if (res.data?.isSuccess) {
      proposals.value = res.data.data?.items || res.data.data || [];
    }
  } catch (e: any) {
    error.value = e.message;
  } finally {
    loading.value = false;
  }
}

// Fetch committees linked to this tender
async function fetchCommittees() {
  try {
    const res = await api.get('/api/committees', { params: { pageSize: 100 } });
    if (res.data?.isSuccess) {
      const all = res.data.data?.items || res.data.data || [];
      committees.value = all.filter((c: any) =>
        c.tenderId === selectedTenderId.value ||
        c.tenderIds?.includes(selectedTenderId.value)
      );
    }
  } catch (e: any) {
    // silently fail
  }
}

// Fetch evaluation reports
async function fetchReports() {
  if (!selectedTenderId.value) return;
  try {
    const res = await api.get(`/api/tenders/${selectedTenderId.value}/evaluation/reports`);
    if (res.data?.isSuccess) {
      evaluationReports.value = res.data.data || [];
    }
  } catch (e: any) {
    // silently fail
  }
}

// Select a tender and load its data
async function selectTender(tender: any) {
  selectedTenderId.value = tender.id;
  selectedTender.value = tender;
  activePhase.value = 'proposals';
  await Promise.all([fetchProposals(), fetchCommittees(), fetchReports()]);
}

// Navigate to proposal management for upload
function goToProposalUpload() {
  if (selectedTenderId.value) {
    router.push(`/tenders/${selectedTenderId.value}/proposals`);
  }
}

// Navigate to compliance check
function goToComplianceCheck(proposalId: string) {
  if (selectedTenderId.value) {
    router.push(`/tenders/${selectedTenderId.value}/evaluation/${proposalId}/compliance`);
  }
}

// Navigate to technical evaluation
function goToTechnicalEvaluation(proposalId: string) {
  if (selectedTenderId.value) {
    router.push(`/tenders/${selectedTenderId.value}/evaluation/${proposalId}`);
  }
}

// Navigate to financial evaluation
function goToFinancialEvaluation() {
  if (selectedTenderId.value) {
    router.push(`/tenders/${selectedTenderId.value}/evaluation/financial`);
  }
}

// Navigate to reports
function goToReports() {
  if (selectedTenderId.value) {
    router.push(`/tenders/${selectedTenderId.value}/evaluation/reports`);
  }
}

// Status helpers
function getStatusLabel(status: string): string {
  const map: Record<string, { ar: string; en: string }> = {
    Received: { ar: 'مستلم', en: 'Received' },
    ComplianceChecked: { ar: 'تم الفحص النظامي', en: 'Compliance Checked' },
    ComplianceFailed: { ar: 'لم يجتز الفحص', en: 'Compliance Failed' },
    TechnicalEvaluationInProgress: { ar: 'قيد التقييم الفني', en: 'Technical Evaluation In Progress' },
    TechnicalEvaluationCompleted: { ar: 'اكتمل التقييم الفني', en: 'Technical Evaluation Completed' },
    TechnicalFailed: { ar: 'لم يجتز التقييم الفني', en: 'Technical Failed' },
    FinancialEvaluationCompleted: { ar: 'اكتمل التقييم المالي', en: 'Financial Evaluation Completed' },
    Awarded: { ar: 'تمت الترسية', en: 'Awarded' },
    Rejected: { ar: 'مرفوض', en: 'Rejected' },
    Draft: { ar: 'مسودة', en: 'Draft' },
    Published: { ar: 'منشور', en: 'Published' },
    Approved: { ar: 'معتمد', en: 'Approved' },
    EvaluationInProgress: { ar: 'قيد التقييم', en: 'Evaluation In Progress' },
    EvaluationCompleted: { ar: 'اكتمل التقييم', en: 'Evaluation Completed' },
  };
  return isAr.value ? (map[status]?.ar || status) : (map[status]?.en || status);
}

function getStatusColor(status: string): string {
  const colors: Record<string, string> = {
    Received: 'bg-blue-100 text-blue-800',
    ComplianceChecked: 'bg-green-100 text-green-800',
    ComplianceFailed: 'bg-red-100 text-red-800',
    TechnicalEvaluationInProgress: 'bg-yellow-100 text-yellow-800',
    TechnicalEvaluationCompleted: 'bg-green-100 text-green-800',
    TechnicalFailed: 'bg-red-100 text-red-800',
    FinancialEvaluationCompleted: 'bg-emerald-100 text-emerald-800',
    Awarded: 'bg-purple-100 text-purple-800',
    Rejected: 'bg-red-100 text-red-800',
    Draft: 'bg-gray-100 text-gray-800',
    Approved: 'bg-green-100 text-green-800',
    EvaluationInProgress: 'bg-yellow-100 text-yellow-800',
  };
  return colors[status] || 'bg-gray-100 text-gray-800';
}

// Stats
const totalProposals = computed(() => proposals.value.length);
const compliancePassed = computed(() => proposals.value.filter((p: any) => p.passedComplianceCheck === true).length);
const technicalPassed = computed(() => proposals.value.filter((p: any) => p.passedTechnicalEvaluation === true).length);
const financialCompleted = computed(() => proposals.value.filter((p: any) => p.financialScore !== null).length);

// Phases for stepper
const phases = computed(() => [
  {
    key: 'proposals',
    label: isAr.value ? 'رفع العروض' : 'Upload Proposals',
    icon: 'M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12',
    count: totalProposals.value,
    active: activePhase.value === 'proposals',
  },
  {
    key: 'compliance',
    label: isAr.value ? 'الفحص النظامي' : 'Compliance Check',
    icon: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z',
    count: compliancePassed.value,
    active: activePhase.value === 'compliance',
  },
  {
    key: 'technical',
    label: isAr.value ? 'التقييم الفني' : 'Technical Evaluation',
    icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01',
    count: technicalPassed.value,
    active: activePhase.value === 'technical',
  },
  {
    key: 'financial',
    label: isAr.value ? 'التقييم المالي' : 'Financial Evaluation',
    icon: 'M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z',
    count: financialCompleted.value,
    active: activePhase.value === 'financial',
  },
  {
    key: 'reports',
    label: isAr.value ? 'المحاضر والتقارير' : 'Reports & Minutes',
    icon: 'M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
    count: evaluationReports.value.length,
    active: activePhase.value === 'reports',
  },
]);

onMounted(fetchTenders);
</script>

<template>
  <div class="min-h-screen bg-gray-50 p-6">
    <!-- Header -->
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-gray-900">
        {{ isAr ? 'فحص وتقييم العروض' : 'Proposal Evaluation' }}
      </h1>
      <p class="mt-1 text-sm text-gray-500">
        {{ isAr ? 'إدارة دورة حياة العروض من الاستلام حتى الترسية - وفقاً لنظام المنافسات والمشتريات الحكومية' : 'Manage proposal lifecycle from receipt to award - per Government Tenders & Procurement Law' }}
      </p>
    </div>

    <!-- Error Alert -->
    <div v-if="error" class="mb-4 rounded-lg bg-red-50 p-4 text-sm text-red-700">
      {{ error }}
      <button @click="error = null" class="ms-2 font-bold">&times;</button>
    </div>

    <!-- Step 1: Select Tender -->
    <div v-if="activePhase === 'select'" class="space-y-4">
      <div class="rounded-xl bg-white p-6 shadow-sm border border-gray-200">
        <h2 class="text-lg font-semibold text-gray-900 mb-4">
          {{ isAr ? 'اختر المنافسة' : 'Select Tender' }}
        </h2>
        <p class="text-sm text-gray-500 mb-6">
          {{ isAr ? 'اختر المنافسة التي تريد إدارة عروضها وتقييمها. يتم جلب العروض من منصة اعتماد كملفات PDF ورفعها هنا.' : 'Select the tender whose proposals you want to manage and evaluate. Proposals are fetched from Etimad platform as PDF files and uploaded here.' }}
        </p>

        <!-- Loading -->
        <div v-if="loading" class="flex justify-center py-12">
          <div class="h-10 w-10 animate-spin rounded-full border-4 border-indigo-600 border-t-transparent"></div>
        </div>

        <!-- Tenders Grid -->
        <div v-else-if="tenders.length > 0" class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          <div
            v-for="tender in tenders"
            :key="tender.id"
            @click="selectTender(tender)"
            class="cursor-pointer rounded-xl border-2 border-gray-200 bg-white p-5 transition-all hover:border-indigo-400 hover:shadow-lg group"
          >
            <div class="flex items-start justify-between mb-3">
              <span :class="getStatusColor(tender.status)" class="rounded-full px-2.5 py-1 text-xs font-medium">
                {{ getStatusLabel(tender.status) }}
              </span>
              <span class="text-xs text-gray-400">{{ tender.referenceNumber }}</span>
            </div>
            <h3 class="font-bold text-gray-900 group-hover:text-indigo-700 mb-2 line-clamp-2">
              {{ isAr ? tender.titleAr : (tender.titleEn || tender.titleAr) }}
            </h3>
            <p class="text-sm text-gray-500 mb-3 line-clamp-2">
              {{ isAr ? tender.descriptionAr : (tender.descriptionEn || tender.descriptionAr) }}
            </p>
            <div class="flex items-center justify-between text-xs text-gray-400">
              <span>{{ isAr ? 'القيمة التقديرية:' : 'Est. Value:' }} {{ (tender.estimatedValue || 0).toLocaleString() }} {{ isAr ? 'ر.س' : 'SAR' }}</span>
              <span class="flex items-center gap-1">
                <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                </svg>
              </span>
            </div>
          </div>
        </div>

        <!-- Empty State -->
        <div v-else class="py-12 text-center">
          <svg class="mx-auto h-12 w-12 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M19 20H5a2 2 0 01-2-2V6a2 2 0 012-2h10a2 2 0 012 2v1m2 13a2 2 0 01-2-2V7m2 13a2 2 0 002-2V9a2 2 0 00-2-2h-2m-4-3H9M7 16h6M7 8h6v4H7V8z" />
          </svg>
          <p class="mt-3 text-gray-500">{{ isAr ? 'لا توجد منافسات متاحة حالياً' : 'No tenders available' }}</p>
        </div>
      </div>
    </div>

    <!-- Selected Tender View -->
    <div v-else>
      <!-- Back Button + Tender Info -->
      <div class="mb-6 flex items-center gap-4">
        <button
          @click="activePhase = 'select'; selectedTenderId = null; selectedTender = null"
          class="flex items-center gap-2 rounded-lg border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-50"
        >
          <svg class="w-4 h-4" :class="isAr ? 'rotate-180' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
          </svg>
          {{ isAr ? 'عودة للمنافسات' : 'Back to Tenders' }}
        </button>
        <div class="flex-1">
          <h2 class="text-lg font-bold text-gray-900">
            {{ isAr ? selectedTender?.titleAr : (selectedTender?.titleEn || selectedTender?.titleAr) }}
          </h2>
          <p class="text-sm text-gray-500">{{ selectedTender?.referenceNumber }}</p>
        </div>
      </div>

      <!-- Phase Stepper -->
      <div class="mb-6 rounded-xl bg-white p-4 shadow-sm border border-gray-200">
        <div class="flex items-center justify-between gap-2 overflow-x-auto">
          <button
            v-for="(phase, idx) in phases"
            :key="phase.key"
            @click="activePhase = phase.key as any"
            class="flex flex-1 flex-col items-center gap-2 rounded-lg px-3 py-3 text-center transition-all min-w-[120px]"
            :class="phase.active ? 'bg-indigo-50 border-2 border-indigo-500 shadow-sm' : 'border-2 border-transparent hover:bg-gray-50'"
          >
            <div class="flex items-center gap-2">
              <div
                class="flex h-8 w-8 items-center justify-center rounded-full text-sm font-bold"
                :class="phase.active ? 'bg-indigo-600 text-white' : 'bg-gray-200 text-gray-600'"
              >
                {{ idx + 1 }}
              </div>
              <svg class="w-5 h-5" :class="phase.active ? 'text-indigo-600' : 'text-gray-400'" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="phase.icon" />
              </svg>
            </div>
            <span class="text-xs font-medium" :class="phase.active ? 'text-indigo-700' : 'text-gray-500'">
              {{ phase.label }}
            </span>
            <span class="rounded-full px-2 py-0.5 text-xs font-bold" :class="phase.active ? 'bg-indigo-100 text-indigo-700' : 'bg-gray-100 text-gray-500'">
              {{ phase.count }}
            </span>
          </button>
        </div>
      </div>

      <!-- Stats Cards -->
      <div class="mb-6 grid gap-4 md:grid-cols-4">
        <div class="rounded-xl bg-white p-4 shadow-sm border border-gray-200">
          <div class="flex items-center gap-3">
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-100">
              <svg class="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
              </svg>
            </div>
            <div>
              <p class="text-2xl font-bold text-gray-900">{{ totalProposals }}</p>
              <p class="text-xs text-gray-500">{{ isAr ? 'إجمالي العروض' : 'Total Proposals' }}</p>
            </div>
          </div>
        </div>
        <div class="rounded-xl bg-white p-4 shadow-sm border border-gray-200">
          <div class="flex items-center gap-3">
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-green-100">
              <svg class="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <div>
              <p class="text-2xl font-bold text-gray-900">{{ compliancePassed }}</p>
              <p class="text-xs text-gray-500">{{ isAr ? 'اجتاز الفحص النظامي' : 'Compliance Passed' }}</p>
            </div>
          </div>
        </div>
        <div class="rounded-xl bg-white p-4 shadow-sm border border-gray-200">
          <div class="flex items-center gap-3">
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-yellow-100">
              <svg class="w-5 h-5 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
              </svg>
            </div>
            <div>
              <p class="text-2xl font-bold text-gray-900">{{ technicalPassed }}</p>
              <p class="text-xs text-gray-500">{{ isAr ? 'اجتاز التقييم الفني' : 'Technical Passed' }}</p>
            </div>
          </div>
        </div>
        <div class="rounded-xl bg-white p-4 shadow-sm border border-gray-200">
          <div class="flex items-center gap-3">
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-purple-100">
              <svg class="w-5 h-5 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <div>
              <p class="text-2xl font-bold text-gray-900">{{ financialCompleted }}</p>
              <p class="text-xs text-gray-500">{{ isAr ? 'اكتمل التقييم المالي' : 'Financial Completed' }}</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Phase: Proposals Upload -->
      <div v-if="activePhase === 'proposals'" class="rounded-xl bg-white shadow-sm border border-gray-200">
        <div class="flex items-center justify-between border-b border-gray-200 p-5">
          <div>
            <h3 class="text-lg font-semibold text-gray-900">{{ isAr ? 'العروض المرفوعة' : 'Uploaded Proposals' }}</h3>
            <p class="text-sm text-gray-500">{{ isAr ? 'العروض المستلمة من منصة اعتماد (PDF) والمرفوعة على المنصة' : 'Proposals received from Etimad platform (PDF) and uploaded to the system' }}</p>
          </div>
          <button @click="goToProposalUpload" class="flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 transition-colors">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            {{ isAr ? 'رفع عرض جديد' : 'Upload New Proposal' }}
          </button>
        </div>

        <!-- Loading -->
        <div v-if="loading" class="flex justify-center py-12">
          <div class="h-8 w-8 animate-spin rounded-full border-4 border-indigo-600 border-t-transparent"></div>
        </div>

        <!-- Proposals Table -->
        <div v-else-if="proposals.length > 0" class="overflow-x-auto">
          <table class="w-full">
            <thead class="bg-gray-50">
              <tr>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">#</th>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">{{ isAr ? 'المتنافس' : 'Vendor' }}</th>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">{{ isAr ? 'الرقم المرجعي' : 'Reference' }}</th>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">{{ isAr ? 'القيمة (ر.س)' : 'Value (SAR)' }}</th>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">{{ isAr ? 'الملفات' : 'Files' }}</th>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">{{ isAr ? 'الحالة' : 'Status' }}</th>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">{{ isAr ? 'الإجراءات' : 'Actions' }}</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100">
              <tr v-for="(proposal, idx) in proposals" :key="proposal.id" class="hover:bg-gray-50 transition-colors">
                <td class="px-5 py-3 text-sm text-gray-500">{{ idx + 1 }}</td>
                <td class="px-5 py-3">
                  <div class="text-sm font-medium text-gray-900">{{ isAr ? proposal.vendorNameAr : (proposal.vendorNameEn || proposal.vendorNameAr) }}</div>
                </td>
                <td class="px-5 py-3 text-sm text-gray-500 font-mono">{{ proposal.vendorReferenceNumber }}</td>
                <td class="px-5 py-3 text-sm text-gray-900 font-medium">{{ (proposal.totalValue || 0).toLocaleString() }}</td>
                <td class="px-5 py-3">
                  <span class="rounded-full bg-blue-100 px-2 py-0.5 text-xs font-medium text-blue-700">
                    {{ (proposal.files || []).length }} {{ isAr ? 'ملف' : 'files' }}
                  </span>
                </td>
                <td class="px-5 py-3">
                  <span :class="getStatusColor(proposal.status)" class="rounded-full px-2.5 py-1 text-xs font-medium">
                    {{ getStatusLabel(proposal.status) }}
                  </span>
                </td>
                <td class="px-5 py-3">
                  <div class="flex items-center gap-2">
                    <button
                      @click="goToComplianceCheck(proposal.id)"
                      class="rounded-lg border border-gray-300 px-3 py-1.5 text-xs font-medium text-gray-700 hover:bg-gray-50 transition-colors"
                      :title="isAr ? 'الفحص النظامي' : 'Compliance Check'"
                    >
                      <svg class="w-4 h-4 inline-block" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                      </svg>
                      {{ isAr ? 'فحص' : 'Check' }}
                    </button>
                    <button
                      v-if="proposal.passedComplianceCheck"
                      @click="goToTechnicalEvaluation(proposal.id)"
                      class="rounded-lg border border-indigo-300 px-3 py-1.5 text-xs font-medium text-indigo-700 hover:bg-indigo-50 transition-colors"
                      :title="isAr ? 'التقييم الفني' : 'Technical Evaluation'"
                    >
                      <svg class="w-4 h-4 inline-block" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                      </svg>
                      {{ isAr ? 'تقييم فني' : 'Technical' }}
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Empty State -->
        <div v-else class="py-12 text-center">
          <svg class="mx-auto h-12 w-12 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
          </svg>
          <p class="mt-3 text-gray-500">{{ isAr ? 'لا توجد عروض مرفوعة بعد. قم برفع العروض المستلمة من منصة اعتماد.' : 'No proposals uploaded yet. Upload proposals received from Etimad platform.' }}</p>
          <button @click="goToProposalUpload" class="mt-4 rounded-lg bg-indigo-600 px-6 py-2 text-sm font-medium text-white hover:bg-indigo-700">
            {{ isAr ? 'رفع أول عرض' : 'Upload First Proposal' }}
          </button>
        </div>
      </div>

      <!-- Phase: Compliance Check -->
      <div v-if="activePhase === 'compliance'" class="rounded-xl bg-white shadow-sm border border-gray-200">
        <div class="border-b border-gray-200 p-5">
          <h3 class="text-lg font-semibold text-gray-900">{{ isAr ? 'الفحص النظامي للعروض' : 'Compliance Check' }}</h3>
          <p class="text-sm text-gray-500">{{ isAr ? 'التحقق من استيفاء العروض للمتطلبات النظامية والشكلية وفقاً لنظام المنافسات والمشتريات الحكومية' : 'Verify proposals meet regulatory and formal requirements per Government Tenders & Procurement Law' }}</p>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full">
            <thead class="bg-gray-50">
              <tr>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">#</th>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">{{ isAr ? 'المتنافس' : 'Vendor' }}</th>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">{{ isAr ? 'نتيجة الفحص' : 'Result' }}</th>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">{{ isAr ? 'الإجراء' : 'Action' }}</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100">
              <tr v-for="(p, idx) in proposals" :key="p.id" class="hover:bg-gray-50">
                <td class="px-5 py-3 text-sm text-gray-500">{{ idx + 1 }}</td>
                <td class="px-5 py-3 text-sm font-medium text-gray-900">{{ isAr ? p.vendorNameAr : (p.vendorNameEn || p.vendorNameAr) }}</td>
                <td class="px-5 py-3">
                  <span v-if="p.passedComplianceCheck === true" class="rounded-full bg-green-100 px-2.5 py-1 text-xs font-medium text-green-800">{{ isAr ? 'مستوفٍ' : 'Passed' }}</span>
                  <span v-else-if="p.passedComplianceCheck === false" class="rounded-full bg-red-100 px-2.5 py-1 text-xs font-medium text-red-800">{{ isAr ? 'غير مستوفٍ' : 'Failed' }}</span>
                  <span v-else class="rounded-full bg-gray-100 px-2.5 py-1 text-xs font-medium text-gray-600">{{ isAr ? 'لم يُفحص بعد' : 'Not Checked' }}</span>
                </td>
                <td class="px-5 py-3">
                  <button @click="goToComplianceCheck(p.id)" class="rounded-lg bg-indigo-600 px-4 py-1.5 text-xs font-medium text-white hover:bg-indigo-700">
                    {{ p.passedComplianceCheck !== null ? (isAr ? 'عرض النتائج' : 'View Results') : (isAr ? 'بدء الفحص' : 'Start Check') }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div v-if="proposals.length === 0" class="py-12 text-center text-gray-500">
          {{ isAr ? 'لا توجد عروض للفحص. يرجى رفع العروض أولاً.' : 'No proposals to check. Please upload proposals first.' }}
        </div>
      </div>

      <!-- Phase: Technical Evaluation -->
      <div v-if="activePhase === 'technical'" class="rounded-xl bg-white shadow-sm border border-gray-200">
        <div class="border-b border-gray-200 p-5">
          <h3 class="text-lg font-semibold text-gray-900">{{ isAr ? 'التقييم الفني' : 'Technical Evaluation' }}</h3>
          <p class="text-sm text-gray-500">{{ isAr ? 'تقييم العروض التي اجتازت الفحص النظامي وفقاً لمعايير التقييم الفني المحددة في كراسة الشروط والمواصفات' : 'Evaluate proposals that passed compliance check based on technical criteria defined in the terms and specifications booklet' }}</p>
        </div>

        <!-- Assigned Committee -->
        <div v-if="committees.length > 0" class="border-b border-gray-100 bg-blue-50 p-4">
          <p class="text-sm font-medium text-blue-800">
            <svg class="w-4 h-4 inline-block me-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z" />
            </svg>
            {{ isAr ? 'اللجنة المكلفة:' : 'Assigned Committee:' }}
            <span v-for="c in committees" :key="c.id" class="font-bold ms-1">{{ isAr ? c.nameAr : (c.nameEn || c.nameAr) }}</span>
          </p>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full">
            <thead class="bg-gray-50">
              <tr>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">#</th>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">{{ isAr ? 'المتنافس' : 'Vendor' }}</th>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">{{ isAr ? 'الدرجة الفنية' : 'Technical Score' }}</th>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">{{ isAr ? 'النتيجة' : 'Result' }}</th>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">{{ isAr ? 'الإجراء' : 'Action' }}</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100">
              <tr v-for="(p, idx) in proposals.filter(p => p.passedComplianceCheck)" :key="p.id" class="hover:bg-gray-50">
                <td class="px-5 py-3 text-sm text-gray-500">{{ idx + 1 }}</td>
                <td class="px-5 py-3 text-sm font-medium text-gray-900">{{ isAr ? p.vendorNameAr : (p.vendorNameEn || p.vendorNameAr) }}</td>
                <td class="px-5 py-3 text-sm font-bold" :class="p.technicalScore >= 60 ? 'text-green-600' : (p.technicalScore ? 'text-red-600' : 'text-gray-400')">
                  {{ p.technicalScore !== null ? p.technicalScore + '%' : '—' }}
                </td>
                <td class="px-5 py-3">
                  <span v-if="p.passedTechnicalEvaluation === true" class="rounded-full bg-green-100 px-2.5 py-1 text-xs font-medium text-green-800">{{ isAr ? 'ناجح' : 'Passed' }}</span>
                  <span v-else-if="p.passedTechnicalEvaluation === false" class="rounded-full bg-red-100 px-2.5 py-1 text-xs font-medium text-red-800">{{ isAr ? 'لم ينجح' : 'Failed' }}</span>
                  <span v-else class="rounded-full bg-gray-100 px-2.5 py-1 text-xs font-medium text-gray-600">{{ isAr ? 'لم يُقيّم بعد' : 'Not Evaluated' }}</span>
                </td>
                <td class="px-5 py-3">
                  <button @click="goToTechnicalEvaluation(p.id)" class="rounded-lg bg-indigo-600 px-4 py-1.5 text-xs font-medium text-white hover:bg-indigo-700">
                    {{ p.technicalScore !== null ? (isAr ? 'عرض التقييم' : 'View Evaluation') : (isAr ? 'بدء التقييم' : 'Start Evaluation') }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div v-if="proposals.filter(p => p.passedComplianceCheck).length === 0" class="py-12 text-center text-gray-500">
          {{ isAr ? 'لا توجد عروض اجتازت الفحص النظامي بعد.' : 'No proposals have passed compliance check yet.' }}
        </div>
      </div>

      <!-- Phase: Financial Evaluation -->
      <div v-if="activePhase === 'financial'" class="rounded-xl bg-white shadow-sm border border-gray-200">
        <div class="flex items-center justify-between border-b border-gray-200 p-5">
          <div>
            <h3 class="text-lg font-semibold text-gray-900">{{ isAr ? 'التقييم المالي' : 'Financial Evaluation' }}</h3>
            <p class="text-sm text-gray-500">{{ isAr ? 'تقييم العروض المالية للعروض التي اجتازت التقييم الفني. الدرجة المالية = (أقل عرض مالي / العرض الحالي) × الوزن المالي' : 'Financial evaluation for proposals that passed technical evaluation. Score = (Lowest / Current) × Financial Weight' }}</p>
          </div>
          <button @click="goToFinancialEvaluation" class="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700">
            {{ isAr ? 'فتح التقييم المالي' : 'Open Financial Evaluation' }}
          </button>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full">
            <thead class="bg-gray-50">
              <tr>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">#</th>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">{{ isAr ? 'المتنافس' : 'Vendor' }}</th>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">{{ isAr ? 'الدرجة الفنية' : 'Technical' }}</th>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">{{ isAr ? 'القيمة المالية' : 'Financial Value' }}</th>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">{{ isAr ? 'الدرجة المالية' : 'Financial Score' }}</th>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">{{ isAr ? 'الدرجة النهائية' : 'Final Score' }}</th>
                <th class="px-5 py-3 text-start text-xs font-semibold text-gray-600 uppercase">{{ isAr ? 'الترتيب' : 'Rank' }}</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100">
              <tr v-for="(p, idx) in proposals.filter(p => p.passedTechnicalEvaluation)" :key="p.id" class="hover:bg-gray-50">
                <td class="px-5 py-3 text-sm text-gray-500">{{ idx + 1 }}</td>
                <td class="px-5 py-3 text-sm font-medium text-gray-900">{{ isAr ? p.vendorNameAr : (p.vendorNameEn || p.vendorNameAr) }}</td>
                <td class="px-5 py-3 text-sm font-medium text-green-600">{{ p.technicalScore }}%</td>
                <td class="px-5 py-3 text-sm text-gray-900">{{ (p.totalValue || 0).toLocaleString() }} {{ isAr ? 'ر.س' : 'SAR' }}</td>
                <td class="px-5 py-3 text-sm font-medium" :class="p.financialScore ? 'text-blue-600' : 'text-gray-400'">{{ p.financialScore !== null ? p.financialScore + '%' : '—' }}</td>
                <td class="px-5 py-3 text-sm font-bold" :class="p.finalScore ? 'text-indigo-700' : 'text-gray-400'">{{ p.finalScore !== null ? p.finalScore + '%' : '—' }}</td>
                <td class="px-5 py-3">
                  <span v-if="p.finalRank" class="flex h-7 w-7 items-center justify-center rounded-full bg-indigo-100 text-xs font-bold text-indigo-700">{{ p.finalRank }}</span>
                  <span v-else class="text-sm text-gray-400">—</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div v-if="proposals.filter(p => p.passedTechnicalEvaluation).length === 0" class="py-12 text-center text-gray-500">
          {{ isAr ? 'لا توجد عروض اجتازت التقييم الفني بعد.' : 'No proposals have passed technical evaluation yet.' }}
        </div>
      </div>

      <!-- Phase: Reports -->
      <div v-if="activePhase === 'reports'" class="rounded-xl bg-white shadow-sm border border-gray-200">
        <div class="flex items-center justify-between border-b border-gray-200 p-5">
          <div>
            <h3 class="text-lg font-semibold text-gray-900">{{ isAr ? 'المحاضر والتقارير' : 'Reports & Minutes' }}</h3>
            <p class="text-sm text-gray-500">{{ isAr ? 'محاضر الفحص النظامي والتقييم الفني والتقرير النهائي' : 'Compliance inspection, technical evaluation, and final report minutes' }}</p>
          </div>
          <button @click="goToReports" class="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700">
            {{ isAr ? 'إدارة المحاضر' : 'Manage Reports' }}
          </button>
        </div>
        <div v-if="evaluationReports.length > 0" class="divide-y divide-gray-100">
          <div v-for="report in evaluationReports" :key="report.id" class="flex items-center justify-between p-5 hover:bg-gray-50">
            <div>
              <h4 class="text-sm font-medium text-gray-900">{{ isAr ? report.titleAr : (report.titleEn || report.titleAr) }}</h4>
              <p class="text-xs text-gray-500">{{ report.referenceNumber }} &middot; {{ getStatusLabel(report.status) }}</p>
            </div>
            <span :class="getStatusColor(report.status)" class="rounded-full px-2.5 py-1 text-xs font-medium">
              {{ getStatusLabel(report.status) }}
            </span>
          </div>
        </div>
        <div v-else class="py-12 text-center text-gray-500">
          {{ isAr ? 'لم يتم إنشاء أي محاضر بعد.' : 'No reports generated yet.' }}
        </div>
      </div>
    </div>
  </div>
</template>
