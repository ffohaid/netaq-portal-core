<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { useEvaluationStore } from '@/stores/evaluation';
import { useI18n } from 'vue-i18n';
import type { FinancialInput } from '@/types/evaluation';

const { locale } = useI18n();
const route = useRoute();
const store = useEvaluationStore();

const tenderId = computed(() => route.params.tenderId as string);
const isAr = computed(() => locale.value === 'ar');

// State
const financialValues = ref<Map<string, number>>(new Map());
const saving = ref(false);

// Computed
const proposals = computed(() => store.proposals.filter(p => p.passedTechnicalEvaluation));
const rankings = computed(() => store.finalRankings);
const loading = computed(() => store.loading);
const error = computed(() => store.error);

// Methods
async function loadData() {
  await store.fetchProposals(tenderId.value, 'TechnicalEvaluationCompleted');
  await store.fetchFinalRankings(tenderId.value);

  for (const p of proposals.value) {
    financialValues.value.set(p.id, p.totalValue);
  }
}

async function handleSubmitFinancial() {
  saving.value = true;
  try {
    const inputs: FinancialInput[] = Array.from(financialValues.value.entries()).map(([proposalId, financialValue]) => ({
      proposalId,
      financialValue
    }));
    await store.submitFinancialEvaluation(tenderId.value, inputs);
    await store.fetchFinalRankings(tenderId.value);
  } catch (e) {
    // Error handled in store
  } finally {
    saving.value = false;
  }
}

function formatCurrency(value: number): string {
  return new Intl.NumberFormat('ar-SA', { style: 'currency', currency: 'SAR' }).format(value);
}

function getRankBadge(rank: number): string {
  if (rank === 1) return 'bg-yellow-100 text-yellow-800 border-yellow-300';
  if (rank === 2) return 'bg-gray-100 text-gray-800 border-gray-300';
  if (rank === 3) return 'bg-orange-100 text-orange-800 border-orange-300';
  return 'bg-white text-gray-600 border-gray-200';
}

onMounted(loadData);
</script>

<template>
  <div class="min-h-screen bg-gray-50 p-6" :dir="isAr ? 'rtl' : 'ltr'">
    <!-- Header -->
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-gray-900">
        {{ isAr ? 'التقييم المالي والترتيب النهائي' : 'Financial Evaluation & Final Ranking' }}
      </h1>
      <p class="mt-1 text-sm text-gray-500">
        {{ isAr ? 'إدخال القيم المالية وحساب الترتيب النهائي' : 'Enter financial values and calculate final ranking' }}
      </p>
    </div>

    <!-- Error -->
    <div v-if="error" class="mb-4 rounded-lg bg-red-50 p-4 text-red-700">{{ error }}</div>

    <!-- Financial Input Section -->
    <div class="mb-6 rounded-xl bg-white p-6 shadow">
      <h2 class="mb-4 text-lg font-bold text-gray-900">
        {{ isAr ? 'القيم المالية' : 'Financial Values' }}
      </h2>
      <div class="mb-4 rounded-lg bg-blue-50 border border-blue-200 p-3 text-sm text-blue-800">
        {{ isAr ? 'المعادلة: الدرجة المالية = (أقل قيمة مالية ÷ القيمة المالية الحالية) × الوزن المالي' : 'Formula: Financial Score = (Lowest Financial / Current Financial) × Financial Weight' }}
      </div>

      <table class="w-full">
        <thead>
          <tr class="border-b text-sm text-gray-500">
            <th class="py-2 text-start">{{ isAr ? 'المتنافس' : 'Vendor' }}</th>
            <th class="py-2 text-start">{{ isAr ? 'الدرجة الفنية' : 'Technical Score' }}</th>
            <th class="py-2 text-start">{{ isAr ? 'القيمة المالية (ريال)' : 'Financial Value (SAR)' }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in proposals" :key="p.id" class="border-b">
            <td class="py-3 font-medium">{{ isAr ? p.vendorNameAr : p.vendorNameEn }}</td>
            <td class="py-3">{{ p.technicalScore?.toFixed(2) }}</td>
            <td class="py-3">
              <input
                type="number"
                min="0"
                step="0.01"
                :value="financialValues.get(p.id)"
                @input="financialValues.set(p.id, Number(($event.target as HTMLInputElement).value))"
                class="w-48 rounded-lg border border-gray-300 px-3 py-1.5"
              />
            </td>
          </tr>
        </tbody>
      </table>

      <div class="mt-4 flex justify-end">
        <button
          @click="handleSubmitFinancial"
          :disabled="saving || proposals.length === 0"
          class="rounded-lg bg-indigo-600 px-6 py-2.5 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
        >
          {{ saving ? (isAr ? 'جاري الحساب...' : 'Calculating...') : (isAr ? 'حساب الترتيب النهائي' : 'Calculate Final Ranking') }}
        </button>
      </div>
    </div>

    <!-- Final Rankings -->
    <div v-if="rankings.length > 0" class="rounded-xl bg-white p-6 shadow">
      <h2 class="mb-4 text-lg font-bold text-gray-900">
        {{ isAr ? 'الترتيب النهائي' : 'Final Rankings' }}
      </h2>

      <div class="space-y-3">
        <div
          v-for="r in rankings"
          :key="r.proposalId"
          :class="['flex items-center gap-4 rounded-xl border-2 p-4 transition-all', getRankBadge(r.finalRank)]"
        >
          <div class="flex h-12 w-12 items-center justify-center rounded-full bg-white text-xl font-bold shadow">
            {{ r.finalRank }}
          </div>
          <div class="flex-1">
            <div class="font-bold text-gray-900">{{ isAr ? r.vendorNameAr : r.vendorNameEn }}</div>
            <div class="text-sm text-gray-500">{{ r.vendorReferenceNumber }}</div>
          </div>
          <div class="grid grid-cols-3 gap-6 text-center">
            <div>
              <div class="text-xs text-gray-500">{{ isAr ? 'فني' : 'Technical' }}</div>
              <div class="text-lg font-bold text-green-600">{{ r.technicalScore.toFixed(2) }}</div>
            </div>
            <div>
              <div class="text-xs text-gray-500">{{ isAr ? 'مالي' : 'Financial' }}</div>
              <div class="text-lg font-bold text-blue-600">{{ r.financialScore.toFixed(2) }}</div>
            </div>
            <div>
              <div class="text-xs text-gray-500">{{ isAr ? 'نهائي' : 'Final' }}</div>
              <div class="text-lg font-bold text-purple-600">{{ r.finalScore.toFixed(2) }}</div>
            </div>
          </div>
          <div v-if="r.status === 'Recommended'" class="rounded-full bg-emerald-500 px-3 py-1 text-xs font-medium text-white">
            {{ isAr ? 'موصى بالترسية' : 'Recommended' }}
          </div>
        </div>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex justify-center py-12">
      <div class="h-8 w-8 animate-spin rounded-full border-4 border-indigo-600 border-t-transparent"></div>
    </div>
  </div>
</template>
