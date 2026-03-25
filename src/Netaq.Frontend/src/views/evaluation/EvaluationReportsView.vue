<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { useEvaluationStore } from '@/stores/evaluation';
import { useI18n } from 'vue-i18n';
import type { EvaluationReport, EvaluationReportType } from '@/types/evaluation';

const { locale } = useI18n();
const route = useRoute();
const store = useEvaluationStore();

const tenderId = computed(() => route.params.tenderId as string);
const isAr = computed(() => locale.value === 'ar');

// State
const selectedReport = ref<EvaluationReport | null>(null);
const signComments = ref('');
const generating = ref(false);
const signing = ref(false);
const aiJustificationLoading = ref(false);
const aiJustification = ref<any>(null);

// Report types
const reportTypes: { value: EvaluationReportType; labelAr: string; labelEn: string }[] = [
  { value: 'ComplianceInspection', labelAr: 'محضر الفحص النظامي', labelEn: 'Compliance Inspection Report' },
  { value: 'TechnicalEvaluation', labelAr: 'محضر التقييم الفني', labelEn: 'Technical Evaluation Report' },
  { value: 'FinalEvaluation', labelAr: 'محضر التقييم النهائي', labelEn: 'Final Evaluation Report' }
];

const statusColorsReport: Record<string, string> = {
  Draft: 'bg-gray-100 text-gray-800',
  PendingSignatures: 'bg-yellow-100 text-yellow-800',
  Signed: 'bg-green-100 text-green-800',
  Finalized: 'bg-blue-100 text-blue-800',
  Exported: 'bg-purple-100 text-purple-800'
};

const statusLabelsArReport: Record<string, string> = {
  Draft: 'مسودة',
  PendingSignatures: 'بانتظار التوقيعات',
  Signed: 'موقّع',
  Finalized: 'معتمد',
  Exported: 'مصدّر'
};

// Methods
async function handleGenerateReport(reportType: EvaluationReportType) {
  generating.value = true;
  try {
    const report = await store.generateReport(tenderId.value, reportType);
    selectedReport.value = report;
  } catch (e) {
    // Error handled in store
  } finally {
    generating.value = false;
  }
}

async function handleSignReport() {
  if (!selectedReport.value) return;
  signing.value = true;
  try {
    await store.signReport(tenderId.value, selectedReport.value.id, signComments.value || undefined);
    signComments.value = '';
    // Refresh report
    selectedReport.value = store.currentReport;
  } catch (e) {
    // Error handled in store
  } finally {
    signing.value = false;
  }
}

async function handleAiAwardJustification() {
  aiJustificationLoading.value = true;
  try {
    aiJustification.value = await store.aiAwardJustification(tenderId.value);
  } catch (e) {
    // Error handled in store
  } finally {
    aiJustificationLoading.value = false;
  }
}

async function handleAiComparisonMatrix() {
  aiJustificationLoading.value = true;
  try {
    const result = await store.aiComparisonMatrix(tenderId.value);
    aiJustification.value = result;
  } catch (e) {
    // Error handled in store
  } finally {
    aiJustificationLoading.value = false;
  }
}
</script>

<template>
  <div class="min-h-screen bg-gray-50 p-6" :dir="isAr ? 'rtl' : 'ltr'">
    <!-- Header -->
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-gray-900">
        {{ isAr ? 'محاضر التقييم والتوقيعات' : 'Evaluation Reports & Signatures' }}
      </h1>
      <p class="mt-1 text-sm text-gray-500">
        {{ isAr ? 'إنشاء المحاضر والتوقيع الإلكتروني' : 'Generate reports and electronic signatures' }}
      </p>
    </div>

    <!-- Error -->
    <div v-if="store.error" class="mb-4 rounded-lg bg-red-50 p-4 text-red-700">{{ store.error }}</div>

    <!-- Report Generation Buttons -->
    <div class="mb-6 grid grid-cols-1 gap-4 md:grid-cols-3">
      <button
        v-for="rt in reportTypes"
        :key="rt.value"
        @click="handleGenerateReport(rt.value)"
        :disabled="generating"
        class="rounded-xl bg-white p-6 shadow hover:shadow-md transition-all text-start border-2 border-transparent hover:border-indigo-200"
      >
        <h3 class="font-bold text-gray-900">{{ isAr ? rt.labelAr : rt.labelEn }}</h3>
        <p class="mt-1 text-sm text-gray-500">
          {{ isAr ? 'إنشاء المحضر تلقائياً' : 'Auto-generate report' }}
        </p>
      </button>
    </div>

    <!-- AI Tools -->
    <div class="mb-6 grid grid-cols-1 gap-4 md:grid-cols-2">
      <button
        @click="handleAiComparisonMatrix"
        :disabled="aiJustificationLoading"
        class="rounded-xl bg-purple-50 p-4 border border-purple-200 hover:bg-purple-100 transition-colors text-start"
      >
        <h3 class="font-medium text-purple-900">{{ isAr ? 'مصفوفة المقارنة (AI)' : 'Comparison Matrix (AI)' }}</h3>
        <p class="text-sm text-purple-600">{{ isAr ? 'مقارنة شاملة بين العروض' : 'Comprehensive proposal comparison' }}</p>
      </button>
      <button
        @click="handleAiAwardJustification"
        :disabled="aiJustificationLoading"
        class="rounded-xl bg-purple-50 p-4 border border-purple-200 hover:bg-purple-100 transition-colors text-start"
      >
        <h3 class="font-medium text-purple-900">{{ isAr ? 'مبررات الترسية (AI)' : 'Award Justification (AI)' }}</h3>
        <p class="text-sm text-purple-600">{{ isAr ? 'مسودة تقرير مبررات الترسية' : 'Draft award justification report' }}</p>
      </button>
    </div>

    <!-- AI Result -->
    <div v-if="aiJustification && aiJustification.justificationAr" class="mb-6 rounded-xl bg-white p-6 shadow">
      <h3 class="mb-3 font-bold text-gray-900">{{ isAr ? 'مبررات الترسية' : 'Award Justification' }}</h3>
      <p class="text-gray-700 leading-relaxed whitespace-pre-line">{{ isAr ? aiJustification.justificationAr : aiJustification.justificationEn }}</p>
      <div v-if="aiJustification.keyReasons" class="mt-4">
        <h4 class="font-medium text-gray-900">{{ isAr ? 'الأسباب الرئيسية:' : 'Key Reasons:' }}</h4>
        <ul class="mt-2 space-y-1">
          <li v-for="(reason, i) in aiJustification.keyReasons" :key="i" class="text-sm text-gray-600">{{ i + 1 }}. {{ reason }}</li>
        </ul>
      </div>
    </div>

    <!-- Selected Report Preview -->
    <div v-if="selectedReport" class="rounded-xl bg-white p-6 shadow">
      <div class="mb-4 flex items-center justify-between">
        <div>
          <h2 class="text-lg font-bold text-gray-900">{{ isAr ? selectedReport.titleAr : selectedReport.titleEn }}</h2>
          <p class="text-sm text-gray-500">{{ selectedReport.referenceNumber }}</p>
        </div>
        <span :class="['rounded-full px-3 py-1 text-xs font-medium', statusColorsReport[selectedReport.status] || 'bg-gray-100']">
          {{ isAr ? statusLabelsArReport[selectedReport.status] || selectedReport.status : selectedReport.status }}
        </span>
      </div>

      <!-- Report Content -->
      <div class="mb-6 rounded-lg border p-4" v-html="selectedReport.contentHtml"></div>

      <!-- Signatures Section -->
      <div class="mb-6">
        <h3 class="mb-3 font-bold text-gray-900">{{ isAr ? 'التوقيعات' : 'Signatures' }}</h3>
        <div class="space-y-2">
          <div v-for="sig in selectedReport.signatures" :key="sig.id" class="flex items-center gap-3 rounded-lg border p-3">
            <div :class="['h-3 w-3 rounded-full', sig.isSigned ? 'bg-green-500' : 'bg-gray-300']"></div>
            <div class="flex-1">
              <div class="font-medium">{{ sig.userName }}</div>
              <div class="text-xs text-gray-500">{{ sig.role }}</div>
            </div>
            <div v-if="sig.isSigned" class="text-xs text-green-600">
              {{ isAr ? 'تم التوقيع' : 'Signed' }} - {{ new Date(sig.signedAt!).toLocaleDateString('en-US') }}
            </div>
            <div v-else class="text-xs text-gray-400">{{ isAr ? 'بانتظار التوقيع' : 'Pending' }}</div>
          </div>
        </div>
      </div>

      <!-- Sign Form -->
      <div class="rounded-lg bg-indigo-50 p-4">
        <h4 class="mb-2 font-medium text-indigo-900">{{ isAr ? 'توقيع المحضر' : 'Sign Report' }}</h4>
        <textarea
          v-model="signComments"
          :placeholder="isAr ? 'ملاحظات (اختياري)' : 'Comments (optional)'"
          rows="2"
          class="mb-3 w-full rounded-lg border border-indigo-200 px-3 py-2"
        ></textarea>
        <button
          @click="handleSignReport"
          :disabled="signing"
          class="rounded-lg bg-indigo-600 px-6 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
        >
          {{ signing ? (isAr ? 'جاري التوقيع...' : 'Signing...') : (isAr ? 'توقيع' : 'Sign') }}
        </button>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="generating || aiJustificationLoading" class="flex justify-center py-8">
      <div class="h-8 w-8 animate-spin rounded-full border-4 border-indigo-600 border-t-transparent"></div>
      <span class="ms-3 text-indigo-600">{{ isAr ? 'جاري المعالجة...' : 'Processing...' }}</span>
    </div>
  </div>
</template>
