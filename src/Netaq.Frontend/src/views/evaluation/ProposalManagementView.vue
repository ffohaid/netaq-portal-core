<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute } from 'vue-router';
import { useEvaluationStore } from '@/stores/evaluation';
import { useI18n } from 'vue-i18n';
import type { CreateProposalRequest, ProposalFileCategory } from '@/types/evaluation';

const { t, locale } = useI18n();
const route = useRoute();
const store = useEvaluationStore();

const tenderId = computed(() => route.params.tenderId as string);
const isAr = computed(() => locale.value === 'ar');

// State
const showCreateModal = ref(false);
const showUploadModal = ref(false);
const selectedProposalId = ref<string | null>(null);
const statusFilter = ref('');
const currentPage = ref(1);

// Form data
const newProposal = ref<CreateProposalRequest>({
  tenderId: '',
  vendorNameAr: '',
  vendorNameEn: '',
  vendorReferenceNumber: '',
  totalValue: 0,
  notes: ''
});

const uploadFile = ref<File | null>(null);
const uploadCategory = ref<ProposalFileCategory>('TechnicalOffer');

// Computed
const proposals = computed(() => store.proposals);
const loading = computed(() => store.loading);
const error = computed(() => store.error);

// Status badge colors
const statusColors: Record<string, string> = {
  Received: 'bg-blue-100 text-blue-800',
  CompliancePassed: 'bg-green-100 text-green-800',
  ComplianceFailed: 'bg-red-100 text-red-800',
  TechnicalEvaluationInProgress: 'bg-yellow-100 text-yellow-800',
  TechnicalEvaluationCompleted: 'bg-green-100 text-green-800',
  TechnicallyDisqualified: 'bg-red-100 text-red-800',
  FinancialEvaluationCompleted: 'bg-green-100 text-green-800',
  Ranked: 'bg-purple-100 text-purple-800',
  Recommended: 'bg-emerald-100 text-emerald-800',
  Excluded: 'bg-gray-100 text-gray-800'
};

const statusLabelsAr: Record<string, string> = {
  Received: 'مستلم',
  CompliancePassed: 'اجتاز الفحص النظامي',
  ComplianceFailed: 'لم يجتز الفحص',
  TechnicalEvaluationInProgress: 'قيد التقييم الفني',
  TechnicalEvaluationCompleted: 'اكتمل التقييم الفني',
  TechnicallyDisqualified: 'مستبعد فنياً',
  FinancialEvaluationCompleted: 'اكتمل التقييم المالي',
  Ranked: 'مرتب',
  Recommended: 'موصى بالترسية',
  Excluded: 'مستبعد'
};

// Methods
async function loadProposals() {
  await store.fetchProposals(tenderId.value, statusFilter.value || undefined, currentPage.value);
}

async function handleCreateProposal() {
  newProposal.value.tenderId = tenderId.value;
  try {
    await store.createProposal(tenderId.value, newProposal.value);
    showCreateModal.value = false;
    resetForm();
    await loadProposals();
  } catch (e) {
    // Error handled in store
  }
}

async function handleFileUpload() {
  if (!selectedProposalId.value || !uploadFile.value) return;
  try {
    await store.uploadProposalFile(tenderId.value, selectedProposalId.value, uploadFile.value, uploadCategory.value);
    showUploadModal.value = false;
    uploadFile.value = null;
    await loadProposals();
  } catch (e) {
    // Error handled in store
  }
}

async function handleCloseReceipt() {
  if (confirm(isAr.value ? 'هل أنت متأكد من إغلاق باب استلام العروض؟ لن يمكن رفع عروض جديدة بعد ذلك.' : 'Are you sure you want to close proposal receipt? No new proposals can be uploaded after this.')) {
    try {
      await store.closeProposalReceipt(tenderId.value);
      await loadProposals();
    } catch (e) {
      // Error handled in store
    }
  }
}

function openUploadModal(proposalId: string) {
  selectedProposalId.value = proposalId;
  showUploadModal.value = true;
}

function resetForm() {
  newProposal.value = {
    tenderId: '',
    vendorNameAr: '',
    vendorNameEn: '',
    vendorReferenceNumber: '',
    totalValue: 0,
    notes: ''
  };
}

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return bytes + ' B';
  if (bytes < 1048576) return (bytes / 1024).toFixed(1) + ' KB';
  return (bytes / 1048576).toFixed(1) + ' MB';
}

function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value) + ' ر.س';
}

function onFileSelected(event: Event) {
  const input = event.target as HTMLInputElement;
  if (input.files && input.files.length > 0) {
    uploadFile.value = input.files[0];
  }
}

onMounted(loadProposals);
watch([statusFilter, currentPage], loadProposals);
</script>

<template>
  <div class="min-h-screen bg-gray-50 p-6" :dir="isAr ? 'rtl' : 'ltr'">
    <!-- Header -->
    <div class="mb-6 flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">
          {{ isAr ? 'إدارة العروض' : 'Proposal Management' }}
        </h1>
        <p class="mt-1 text-sm text-gray-500">
          {{ isAr ? 'رفع وإدارة عروض المتنافسين' : 'Upload and manage vendor proposals' }}
        </p>
      </div>
      <div class="flex gap-3">
        <button
          @click="showCreateModal = true"
          class="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 transition-colors"
        >
          {{ isAr ? 'رفع عرض جديد' : 'Upload New Proposal' }}
        </button>
        <button
          @click="handleCloseReceipt"
          class="rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 transition-colors"
        >
          {{ isAr ? 'إغلاق باب الاستلام' : 'Close Receipt' }}
        </button>
      </div>
    </div>

    <!-- Filters -->
    <div class="mb-4 flex gap-4">
      <select v-model="statusFilter" class="rounded-lg border border-gray-300 px-3 py-2 text-sm">
        <option value="">{{ isAr ? 'جميع الحالات' : 'All Statuses' }}</option>
        <option v-for="(label, key) in statusLabelsAr" :key="key" :value="key">
          {{ isAr ? label : key }}
        </option>
      </select>
    </div>

    <!-- Error Message -->
    <div v-if="error" class="mb-4 rounded-lg bg-red-50 p-4 text-red-700">
      {{ error }}
    </div>

    <!-- Proposals Table -->
    <div class="overflow-hidden rounded-xl bg-white shadow">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th class="px-6 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500">
              {{ isAr ? 'المتنافس' : 'Vendor' }}
            </th>
            <th class="px-6 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500">
              {{ isAr ? 'الرقم المرجعي' : 'Reference' }}
            </th>
            <th class="px-6 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500">
              {{ isAr ? 'القيمة' : 'Value' }}
            </th>
            <th class="px-6 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500">
              {{ isAr ? 'الحالة' : 'Status' }}
            </th>
            <th class="px-6 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500">
              {{ isAr ? 'الملفات' : 'Files' }}
            </th>
            <th class="px-6 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500">
              {{ isAr ? 'الدرجات' : 'Scores' }}
            </th>
            <th class="px-6 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500">
              {{ isAr ? 'الإجراءات' : 'Actions' }}
            </th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-200 bg-white">
          <tr v-for="proposal in proposals" :key="proposal.id" class="hover:bg-gray-50 transition-colors">
            <td class="whitespace-nowrap px-6 py-4">
              <div class="font-medium text-gray-900">{{ isAr ? proposal.vendorNameAr : proposal.vendorNameEn }}</div>
            </td>
            <td class="whitespace-nowrap px-6 py-4 text-sm text-gray-500">
              {{ proposal.vendorReferenceNumber }}
            </td>
            <td class="whitespace-nowrap px-6 py-4 text-sm text-gray-900">
              {{ formatCurrency(proposal.totalValue) }}
            </td>
            <td class="whitespace-nowrap px-6 py-4">
              <span :class="['inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium', statusColors[proposal.status] || 'bg-gray-100 text-gray-800']">
                {{ isAr ? statusLabelsAr[proposal.status] || proposal.status : proposal.status }}
              </span>
            </td>
            <td class="whitespace-nowrap px-6 py-4 text-sm text-gray-500">
              {{ proposal.files?.length || 0 }} {{ isAr ? 'ملف' : 'files' }}
            </td>
            <td class="whitespace-nowrap px-6 py-4 text-sm">
              <div v-if="proposal.technicalScore != null" class="text-green-600">
                {{ isAr ? 'فني' : 'Tech' }}: {{ proposal.technicalScore?.toFixed(2) }}
              </div>
              <div v-if="proposal.financialScore != null" class="text-blue-600">
                {{ isAr ? 'مالي' : 'Fin' }}: {{ proposal.financialScore?.toFixed(2) }}
              </div>
              <div v-if="proposal.finalScore != null" class="font-bold text-purple-600">
                {{ isAr ? 'نهائي' : 'Final' }}: {{ proposal.finalScore?.toFixed(2) }}
              </div>
              <div v-if="proposal.finalRank" class="text-xs text-gray-500">
                {{ isAr ? 'الترتيب' : 'Rank' }}: #{{ proposal.finalRank }}
              </div>
            </td>
            <td class="whitespace-nowrap px-6 py-4 text-sm">
              <div class="flex gap-2">
                <button
                  @click="openUploadModal(proposal.id)"
                  class="rounded bg-blue-50 px-2 py-1 text-xs text-blue-700 hover:bg-blue-100"
                >
                  {{ isAr ? 'رفع ملف' : 'Upload' }}
                </button>
                <router-link
                  :to="`/tenders/${tenderId}/evaluation/${proposal.id}`"
                  class="rounded bg-indigo-50 px-2 py-1 text-xs text-indigo-700 hover:bg-indigo-100"
                >
                  {{ isAr ? 'تقييم' : 'Evaluate' }}
                </router-link>
              </div>
            </td>
          </tr>
        </tbody>
      </table>

      <!-- Empty State -->
      <div v-if="!loading && proposals.length === 0" class="py-12 text-center">
        <p class="text-gray-500">{{ isAr ? 'لا توجد عروض مرفوعة بعد' : 'No proposals uploaded yet' }}</p>
      </div>

      <!-- Loading -->
      <div v-if="loading" class="flex justify-center py-12">
        <div class="h-8 w-8 animate-spin rounded-full border-4 border-indigo-600 border-t-transparent"></div>
      </div>
    </div>

    <!-- Create Proposal Modal -->
    <div v-if="showCreateModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div class="w-full max-w-lg rounded-xl bg-white p-6 shadow-xl">
        <h2 class="mb-4 text-lg font-bold text-gray-900">
          {{ isAr ? 'رفع عرض جديد' : 'Upload New Proposal' }}
        </h2>
        <form @submit.prevent="handleCreateProposal" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700">{{ isAr ? 'اسم المتنافس (عربي)' : 'Vendor Name (Arabic)' }}</label>
            <input v-model="newProposal.vendorNameAr" required class="mt-1 w-full rounded-lg border border-gray-300 px-3 py-2" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700">{{ isAr ? 'اسم المتنافس (إنجليزي)' : 'Vendor Name (English)' }}</label>
            <input v-model="newProposal.vendorNameEn" class="mt-1 w-full rounded-lg border border-gray-300 px-3 py-2" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700">{{ isAr ? 'الرقم المرجعي' : 'Reference Number' }}</label>
            <input v-model="newProposal.vendorReferenceNumber" required class="mt-1 w-full rounded-lg border border-gray-300 px-3 py-2" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700">{{ isAr ? 'القيمة الإجمالية (ر.س)' : 'Total Value (SAR)' }}</label>
            <input v-model.number="newProposal.totalValue" type="number" min="0" step="0.01" required class="mt-1 w-full rounded-lg border border-gray-300 px-3 py-2" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700">{{ isAr ? 'ملاحظات' : 'Notes' }}</label>
            <textarea v-model="newProposal.notes" rows="2" class="mt-1 w-full rounded-lg border border-gray-300 px-3 py-2"></textarea>
          </div>
          <div class="flex justify-end gap-3">
            <button type="button" @click="showCreateModal = false" class="rounded-lg border px-4 py-2 text-sm text-gray-700 hover:bg-gray-50">
              {{ isAr ? 'إلغاء' : 'Cancel' }}
            </button>
            <button type="submit" :disabled="loading" class="rounded-lg bg-indigo-600 px-4 py-2 text-sm text-white hover:bg-indigo-700 disabled:opacity-50">
              {{ isAr ? 'حفظ' : 'Save' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Upload File Modal -->
    <div v-if="showUploadModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div class="w-full max-w-md rounded-xl bg-white p-6 shadow-xl">
        <h2 class="mb-4 text-lg font-bold text-gray-900">
          {{ isAr ? 'رفع ملف' : 'Upload File' }}
        </h2>
        <form @submit.prevent="handleFileUpload" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700">{{ isAr ? 'تصنيف الملف' : 'File Category' }}</label>
            <select v-model="uploadCategory" class="mt-1 w-full rounded-lg border border-gray-300 px-3 py-2">
              <option value="TechnicalOffer">{{ isAr ? 'العرض الفني' : 'Technical Offer' }}</option>
              <option value="FinancialOffer">{{ isAr ? 'العرض المالي' : 'Financial Offer' }}</option>
              <option value="ComplianceDocuments">{{ isAr ? 'وثائق الامتثال' : 'Compliance Documents' }}</option>
              <option value="SupportingDocuments">{{ isAr ? 'وثائق داعمة' : 'Supporting Documents' }}</option>
              <option value="Other">{{ isAr ? 'أخرى' : 'Other' }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700">{{ isAr ? 'الملف (PDF حتى 100MB)' : 'File (PDF up to 100MB)' }}</label>
            <input type="file" accept=".pdf" @change="onFileSelected" class="mt-1 w-full" />
          </div>
          <div class="flex justify-end gap-3">
            <button type="button" @click="showUploadModal = false" class="rounded-lg border px-4 py-2 text-sm text-gray-700 hover:bg-gray-50">
              {{ isAr ? 'إلغاء' : 'Cancel' }}
            </button>
            <button type="submit" :disabled="loading || !uploadFile" class="rounded-lg bg-indigo-600 px-4 py-2 text-sm text-white hover:bg-indigo-700 disabled:opacity-50">
              {{ isAr ? 'رفع' : 'Upload' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
