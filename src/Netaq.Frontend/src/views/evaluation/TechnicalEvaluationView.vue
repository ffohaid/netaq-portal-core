<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { useEvaluationStore } from '@/stores/evaluation';
import { useI18n } from 'vue-i18n';
import type { ScoreInput, EvaluationScore } from '@/types/evaluation';

const { t, locale } = useI18n();
const route = useRoute();
const store = useEvaluationStore();

const tenderId = computed(() => route.params.tenderId as string);
const proposalId = computed(() => route.params.proposalId as string);
const isAr = computed(() => locale.value === 'ar');

// State
const activeTab = ref<'scoring' | 'summary' | 'ai'>('scoring');
const scores = ref<Map<string, { score: number; justification: string }>>(new Map());
const saving = ref(false);
const aiLoading = ref(false);

// Computed
const myScores = computed(() => store.myScores);
const summary = computed(() => store.evaluationSummary);
const proposal = computed(() => store.currentProposal);
const loading = computed(() => store.loading);
const error = computed(() => store.error);

// Methods
async function loadData() {
  await Promise.all([
    store.fetchProposalDetail(tenderId.value, proposalId.value),
    store.fetchMyScores(tenderId.value, proposalId.value)
  ]);

  // Initialize scores from existing data
  for (const s of myScores.value) {
    scores.value.set(s.criteriaId, {
      score: s.score,
      justification: s.justification || ''
    });
  }
}

async function handleSubmitScores() {
  saving.value = true;
  try {
    const scoreInputs: ScoreInput[] = Array.from(scores.value.entries()).map(([criteriaId, data]) => ({
      criteriaId,
      score: data.score,
      justification: data.justification || undefined
    }));
    await store.submitTechnicalScores(tenderId.value, proposalId.value, scoreInputs);
  } catch (e) {
    // Error handled in store
  } finally {
    saving.value = false;
  }
}

async function loadSummary() {
  await store.fetchEvaluationSummary(tenderId.value, proposalId.value);
}

async function handleAiSuggestScores() {
  aiLoading.value = true;
  try {
    const result = await store.aiSuggestScores(proposalId.value);
    if (result?.suggestions) {
      for (const suggestion of result.suggestions) {
        const existing = scores.value.get(suggestion.criteriaId);
        if (existing) {
          // Don't overwrite, just show as suggestion
        } else {
          scores.value.set(suggestion.criteriaId, {
            score: suggestion.suggestedScore,
            justification: suggestion.justificationAr
          });
        }
      }
    }
  } catch (e) {
    // Error handled in store
  } finally {
    aiLoading.value = false;
  }
}

async function handleAiSummarize() {
  aiLoading.value = true;
  try {
    await store.aiSummarizeProposal(proposalId.value);
  } catch (e) {
    // Error handled in store
  } finally {
    aiLoading.value = false;
  }
}

async function handleAiGapAnalysis() {
  aiLoading.value = true;
  try {
    await store.aiAnalyzeGaps(proposalId.value);
  } catch (e) {
    // Error handled in store
  } finally {
    aiLoading.value = false;
  }
}

function updateScore(criteriaId: string, value: number) {
  const existing = scores.value.get(criteriaId) || { score: 0, justification: '' };
  scores.value.set(criteriaId, { ...existing, score: value });
}

function updateJustification(criteriaId: string, value: string) {
  const existing = scores.value.get(criteriaId) || { score: 0, justification: '' };
  scores.value.set(criteriaId, { ...existing, justification: value });
}

onMounted(loadData);
</script>

<template>
  <div class="min-h-screen bg-gray-50 p-6" :dir="isAr ? 'rtl' : 'ltr'">
    <!-- Header -->
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-gray-900">
        {{ isAr ? 'التقييم الفني' : 'Technical Evaluation' }}
      </h1>
      <p v-if="proposal" class="mt-1 text-sm text-gray-500">
        {{ isAr ? proposal.vendorNameAr : proposal.vendorNameEn }} - {{ proposal.vendorReferenceNumber }}
      </p>
    </div>

    <!-- Tabs -->
    <div class="mb-6 border-b border-gray-200">
      <nav class="flex gap-4">
        <button
          @click="activeTab = 'scoring'"
          :class="['pb-3 text-sm font-medium border-b-2 transition-colors', activeTab === 'scoring' ? 'border-indigo-600 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700']"
        >
          {{ isAr ? 'إدخال الدرجات' : 'Score Entry' }}
        </button>
        <button
          @click="activeTab = 'summary'; loadSummary()"
          :class="['pb-3 text-sm font-medium border-b-2 transition-colors', activeTab === 'summary' ? 'border-indigo-600 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700']"
        >
          {{ isAr ? 'ملخص التقييم (رئيس اللجنة)' : 'Evaluation Summary (Chair)' }}
        </button>
        <button
          @click="activeTab = 'ai'"
          :class="['pb-3 text-sm font-medium border-b-2 transition-colors', activeTab === 'ai' ? 'border-indigo-600 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700']"
        >
          {{ isAr ? 'مساعد الذكاء الاصطناعي' : 'AI Assistant' }}
        </button>
      </nav>
    </div>

    <!-- Error -->
    <div v-if="error" class="mb-4 rounded-lg bg-red-50 p-4 text-red-700">{{ error }}</div>

    <!-- Scoring Tab -->
    <div v-if="activeTab === 'scoring'" class="space-y-4">
      <div class="rounded-xl bg-yellow-50 border border-yellow-200 p-4 text-sm text-yellow-800">
        {{ isAr ? 'التقييم مستقل: لا يمكنك رؤية تقييمات الأعضاء الآخرين. أدخل درجاتك بشكل مستقل.' : 'Blind Evaluation: You cannot see other members\' scores. Enter your scores independently.' }}
      </div>

      <div v-for="score in myScores" :key="score.criteriaId" class="rounded-xl bg-white p-6 shadow">
        <div class="mb-3 flex items-center justify-between">
          <h3 class="font-medium text-gray-900">{{ isAr ? score.criteriaNameAr : score.criteriaNameEn }}</h3>
          <div v-if="score.aiSuggestedScore != null" class="rounded-full bg-purple-100 px-3 py-1 text-xs text-purple-700">
            {{ isAr ? 'اقتراح AI' : 'AI Suggestion' }}: {{ score.aiSuggestedScore }}
          </div>
        </div>

        <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
          <div>
            <label class="block text-sm font-medium text-gray-700">{{ isAr ? 'الدرجة (0-100)' : 'Score (0-100)' }}</label>
            <input
              type="number"
              min="0"
              max="100"
              step="0.01"
              :value="scores.get(score.criteriaId)?.score ?? score.score"
              @input="updateScore(score.criteriaId, Number(($event.target as HTMLInputElement).value))"
              class="mt-1 w-full rounded-lg border border-gray-300 px-3 py-2"
            />
            <!-- Score bar -->
            <div class="mt-2 h-2 w-full rounded-full bg-gray-200">
              <div
                class="h-2 rounded-full transition-all"
                :class="(scores.get(score.criteriaId)?.score ?? score.score) >= 60 ? 'bg-green-500' : 'bg-red-500'"
                :style="{ width: `${Math.min(scores.get(score.criteriaId)?.score ?? score.score, 100)}%` }"
              ></div>
            </div>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700">
              {{ isAr ? 'المبررات' : 'Justification' }}
              <span v-if="(scores.get(score.criteriaId)?.score ?? score.score) < 60" class="text-red-500">*</span>
            </label>
            <textarea
              :value="scores.get(score.criteriaId)?.justification ?? score.justification ?? ''"
              @input="updateJustification(score.criteriaId, ($event.target as HTMLTextAreaElement).value)"
              rows="2"
              class="mt-1 w-full rounded-lg border border-gray-300 px-3 py-2"
              :placeholder="isAr ? 'إلزامي عند الدرجة أقل من الحد الأدنى' : 'Required when score is below threshold'"
            ></textarea>
          </div>
        </div>

        <!-- AI Justification -->
        <div v-if="score.aiJustification" class="mt-3 rounded-lg bg-purple-50 p-3 text-sm text-purple-800">
          <strong>{{ isAr ? 'تحليل AI:' : 'AI Analysis:' }}</strong> {{ score.aiJustification }}
        </div>
      </div>

      <div class="flex justify-end gap-3">
        <button
          @click="handleSubmitScores"
          :disabled="saving"
          class="rounded-lg bg-indigo-600 px-6 py-2.5 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
        >
          {{ saving ? (isAr ? 'جاري الحفظ...' : 'Saving...') : (isAr ? 'حفظ الدرجات' : 'Save Scores') }}
        </button>
      </div>
    </div>

    <!-- Summary Tab (Chair Only) -->
    <div v-if="activeTab === 'summary'">
      <div v-if="summary" class="space-y-4">
        <div class="rounded-xl bg-white p-6 shadow">
          <h3 class="mb-4 text-lg font-bold text-gray-900">
            {{ isAr ? 'ملخص التقييم' : 'Evaluation Summary' }}
          </h3>
          <div class="grid grid-cols-2 gap-4 mb-6">
            <div class="rounded-lg bg-indigo-50 p-4">
              <div class="text-sm text-indigo-600">{{ isAr ? 'متوسط الدرجات' : 'Average Score' }}</div>
              <div class="text-2xl font-bold text-indigo-900">{{ summary.averageTotal.toFixed(2) }}</div>
            </div>
            <div class="rounded-lg bg-orange-50 p-4">
              <div class="text-sm text-orange-600">{{ isAr ? 'التباين' : 'Variance' }}</div>
              <div class="text-2xl font-bold text-orange-900">{{ summary.variance.toFixed(2) }}</div>
            </div>
          </div>

          <div v-for="cs in summary.criteriaSummaries" :key="cs.criteriaId" class="mb-4 rounded-lg border p-4">
            <div class="mb-2 flex items-center justify-between">
              <h4 class="font-medium">{{ cs.criteriaNameAr }}</h4>
              <span class="text-sm text-gray-500">
                {{ isAr ? 'متوسط' : 'Avg' }}: {{ cs.averageScore.toFixed(2) }} |
                {{ isAr ? 'أدنى' : 'Min' }}: {{ cs.minScore }} |
                {{ isAr ? 'أعلى' : 'Max' }}: {{ cs.maxScore }}
              </span>
            </div>
            <table class="w-full text-sm">
              <thead>
                <tr class="text-gray-500">
                  <th class="text-start py-1">{{ isAr ? 'العضو' : 'Member' }}</th>
                  <th class="text-start py-1">{{ isAr ? 'الدرجة' : 'Score' }}</th>
                  <th class="text-start py-1">{{ isAr ? 'المبرر' : 'Justification' }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="ms in cs.memberScores" :key="ms.userId" class="border-t">
                  <td class="py-2">{{ ms.userName }}</td>
                  <td class="py-2 font-medium">{{ ms.score }}</td>
                  <td class="py-2 text-gray-500">{{ ms.justification || '-' }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
      <div v-else-if="loading" class="flex justify-center py-12">
        <div class="h-8 w-8 animate-spin rounded-full border-4 border-indigo-600 border-t-transparent"></div>
      </div>
    </div>

    <!-- AI Tab -->
    <div v-if="activeTab === 'ai'" class="space-y-4">
      <div class="grid grid-cols-1 gap-4 md:grid-cols-3">
        <button
          @click="handleAiSummarize"
          :disabled="aiLoading"
          class="rounded-xl bg-white p-6 shadow hover:shadow-md transition-shadow text-start"
        >
          <div class="mb-2 text-2xl">📝</div>
          <h3 class="font-medium text-gray-900">{{ isAr ? 'تلخيص العرض' : 'Summarize Proposal' }}</h3>
          <p class="mt-1 text-sm text-gray-500">{{ isAr ? 'تلخيص ذكي للعرض الفني' : 'AI summary of technical proposal' }}</p>
        </button>
        <button
          @click="handleAiGapAnalysis"
          :disabled="aiLoading"
          class="rounded-xl bg-white p-6 shadow hover:shadow-md transition-shadow text-start"
        >
          <div class="mb-2 text-2xl">🔍</div>
          <h3 class="font-medium text-gray-900">{{ isAr ? 'تحليل الفجوات' : 'Gap Analysis' }}</h3>
          <p class="mt-1 text-sm text-gray-500">{{ isAr ? 'مقارنة مع متطلبات الكراسة' : 'Compare with booklet requirements' }}</p>
        </button>
        <button
          @click="handleAiSuggestScores"
          :disabled="aiLoading"
          class="rounded-xl bg-white p-6 shadow hover:shadow-md transition-shadow text-start"
        >
          <div class="mb-2 text-2xl">💡</div>
          <h3 class="font-medium text-gray-900">{{ isAr ? 'اقتراح الدرجات' : 'Suggest Scores' }}</h3>
          <p class="mt-1 text-sm text-gray-500">{{ isAr ? 'درجات مقترحة مع المبررات' : 'Suggested scores with justifications' }}</p>
        </button>
      </div>

      <!-- AI Summary Result -->
      <div v-if="store.aiSummary" class="rounded-xl bg-white p-6 shadow">
        <h3 class="mb-3 font-bold text-gray-900">{{ isAr ? 'ملخص العرض' : 'Proposal Summary' }}</h3>
        <p class="text-gray-700 leading-relaxed">{{ isAr ? store.aiSummary.summaryAr : store.aiSummary.summaryEn }}</p>
        <div class="mt-4 grid grid-cols-2 gap-4">
          <div>
            <h4 class="font-medium text-green-700">{{ isAr ? 'نقاط القوة' : 'Strengths' }}</h4>
            <ul class="mt-2 space-y-1">
              <li v-for="s in store.aiSummary.keyStrengths" :key="s" class="text-sm text-gray-600">• {{ s }}</li>
            </ul>
          </div>
          <div>
            <h4 class="font-medium text-red-700">{{ isAr ? 'نقاط الضعف' : 'Weaknesses' }}</h4>
            <ul class="mt-2 space-y-1">
              <li v-for="w in store.aiSummary.keyWeaknesses" :key="w" class="text-sm text-gray-600">• {{ w }}</li>
            </ul>
          </div>
        </div>
      </div>

      <!-- AI Gap Analysis Result -->
      <div v-if="store.aiGapAnalysis" class="rounded-xl bg-white p-6 shadow">
        <h3 class="mb-3 font-bold text-gray-900">{{ isAr ? 'تحليل الفجوات' : 'Gap Analysis' }}</h3>
        <p class="mb-4 text-gray-700">{{ isAr ? store.aiGapAnalysis.overallAssessmentAr : store.aiGapAnalysis.overallAssessmentEn }}</p>
        <div v-for="gap in store.aiGapAnalysis.gaps" :key="gap.requirementAr" class="mb-3 rounded-lg border p-3">
          <div class="flex items-center gap-2">
            <span :class="['rounded-full px-2 py-0.5 text-xs font-medium',
              gap.severity === 'Critical' ? 'bg-red-100 text-red-800' :
              gap.severity === 'Major' ? 'bg-orange-100 text-orange-800' :
              'bg-yellow-100 text-yellow-800']">
              {{ gap.severity }}
            </span>
            <span class="font-medium">{{ gap.requirementAr }}</span>
          </div>
          <p class="mt-1 text-sm text-gray-600">{{ gap.gapDescriptionAr }}</p>
          <p v-if="gap.recommendationAr" class="mt-1 text-sm text-green-700">{{ isAr ? 'التوصية:' : 'Recommendation:' }} {{ gap.recommendationAr }}</p>
        </div>
      </div>

      <!-- Loading -->
      <div v-if="aiLoading" class="flex justify-center py-8">
        <div class="h-8 w-8 animate-spin rounded-full border-4 border-purple-600 border-t-transparent"></div>
        <span class="ms-3 text-purple-600">{{ isAr ? 'جاري التحليل بالذكاء الاصطناعي...' : 'AI analysis in progress...' }}</span>
      </div>
    </div>
  </div>
</template>
