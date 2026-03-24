<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { useEvaluationStore } from '@/stores/evaluation';
import { useI18n } from 'vue-i18n';
import type { ComplianceCheckInput } from '@/types/evaluation';

const { locale } = useI18n();
const route = useRoute();
const store = useEvaluationStore();

const tenderId = computed(() => route.params.tenderId as string);
const proposalId = computed(() => route.params.proposalId as string);
const isAr = computed(() => locale.value === 'ar');

// State
const checkResults = ref<Map<string, { passed: boolean; failureReason: string; notes: string }>>(new Map());
const saving = ref(false);

// Computed
const checklist = computed(() => store.complianceChecklist);
const proposal = computed(() => store.currentProposal);
const loading = computed(() => store.loading);
const error = computed(() => store.error);

const allChecked = computed(() => {
  return checklist.value.every(item => checkResults.value.has(item.id));
});

const passedCount = computed(() => {
  return Array.from(checkResults.value.values()).filter(r => r.passed).length;
});

const failedCount = computed(() => {
  return Array.from(checkResults.value.values()).filter(r => !r.passed).length;
});

// Methods
async function loadData() {
  await Promise.all([
    store.fetchComplianceChecklist(tenderId.value),
    store.fetchProposalDetail(tenderId.value, proposalId.value)
  ]);

  // Initialize with all items as unchecked
  for (const item of checklist.value) {
    checkResults.value.set(item.id, { passed: true, failureReason: '', notes: '' });
  }
}

function toggleItem(itemId: string, passed: boolean) {
  const existing = checkResults.value.get(itemId) || { passed: true, failureReason: '', notes: '' };
  checkResults.value.set(itemId, { ...existing, passed });
}

function updateFailureReason(itemId: string, reason: string) {
  const existing = checkResults.value.get(itemId) || { passed: true, failureReason: '', notes: '' };
  checkResults.value.set(itemId, { ...existing, failureReason: reason });
}

function updateNotes(itemId: string, notes: string) {
  const existing = checkResults.value.get(itemId) || { passed: true, failureReason: '', notes: '' };
  checkResults.value.set(itemId, { ...existing, notes });
}

async function handleSubmit() {
  saving.value = true;
  try {
    const items: ComplianceCheckInput[] = Array.from(checkResults.value.entries()).map(([checklistItemId, data]) => ({
      checklistItemId,
      passed: data.passed,
      failureReason: data.passed ? undefined : data.failureReason || undefined,
      notes: data.notes || undefined
    }));
    await store.submitComplianceCheck(tenderId.value, proposalId.value, items);
  } catch (e) {
    // Error handled in store
  } finally {
    saving.value = false;
  }
}

onMounted(loadData);
</script>

<template>
  <div class="min-h-screen bg-gray-50 p-6" :dir="isAr ? 'rtl' : 'ltr'">
    <!-- Header -->
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-gray-900">
        {{ isAr ? 'الفحص النظامي' : 'Compliance Check' }}
      </h1>
      <p v-if="proposal" class="mt-1 text-sm text-gray-500">
        {{ isAr ? proposal.vendorNameAr : proposal.vendorNameEn }} - {{ proposal.vendorReferenceNumber }}
      </p>
    </div>

    <!-- Summary Bar -->
    <div class="mb-6 grid grid-cols-3 gap-4">
      <div class="rounded-xl bg-white p-4 shadow text-center">
        <div class="text-2xl font-bold text-gray-900">{{ checklist.length }}</div>
        <div class="text-sm text-gray-500">{{ isAr ? 'إجمالي البنود' : 'Total Items' }}</div>
      </div>
      <div class="rounded-xl bg-green-50 p-4 shadow text-center">
        <div class="text-2xl font-bold text-green-700">{{ passedCount }}</div>
        <div class="text-sm text-green-600">{{ isAr ? 'مجتاز' : 'Passed' }}</div>
      </div>
      <div class="rounded-xl bg-red-50 p-4 shadow text-center">
        <div class="text-2xl font-bold text-red-700">{{ failedCount }}</div>
        <div class="text-sm text-red-600">{{ isAr ? 'غير مجتاز' : 'Failed' }}</div>
      </div>
    </div>

    <!-- Error -->
    <div v-if="error" class="mb-4 rounded-lg bg-red-50 p-4 text-red-700">{{ error }}</div>

    <!-- Checklist Items -->
    <div class="space-y-3">
      <div
        v-for="item in checklist"
        :key="item.id"
        :class="['rounded-xl bg-white p-5 shadow transition-all border-2',
          checkResults.get(item.id)?.passed === false ? 'border-red-200' :
          checkResults.get(item.id)?.passed === true ? 'border-green-200' : 'border-transparent']"
      >
        <div class="flex items-start gap-4">
          <div class="flex-1">
            <div class="flex items-center gap-2">
              <span v-if="item.isMandatory" class="rounded bg-red-100 px-1.5 py-0.5 text-xs text-red-700">
                {{ isAr ? 'إلزامي' : 'Mandatory' }}
              </span>
              <h3 class="font-medium text-gray-900">{{ isAr ? item.nameAr : item.nameEn }}</h3>
            </div>
            <p v-if="item.descriptionAr" class="mt-1 text-sm text-gray-500">
              {{ isAr ? item.descriptionAr : item.descriptionEn }}
            </p>
          </div>

          <!-- Pass/Fail Toggle -->
          <div class="flex gap-2">
            <button
              @click="toggleItem(item.id, true)"
              :class="['rounded-lg px-4 py-2 text-sm font-medium transition-colors',
                checkResults.get(item.id)?.passed === true
                  ? 'bg-green-600 text-white'
                  : 'bg-green-50 text-green-700 hover:bg-green-100']"
            >
              {{ isAr ? 'مجتاز' : 'Pass' }}
            </button>
            <button
              @click="toggleItem(item.id, false)"
              :class="['rounded-lg px-4 py-2 text-sm font-medium transition-colors',
                checkResults.get(item.id)?.passed === false
                  ? 'bg-red-600 text-white'
                  : 'bg-red-50 text-red-700 hover:bg-red-100']"
            >
              {{ isAr ? 'غير مجتاز' : 'Fail' }}
            </button>
          </div>
        </div>

        <!-- Failure Reason (shown when failed) -->
        <div v-if="checkResults.get(item.id)?.passed === false" class="mt-3 rounded-lg bg-red-50 p-3">
          <label class="block text-sm font-medium text-red-700">
            {{ isAr ? 'سبب عدم الاجتياز' : 'Failure Reason' }} *
          </label>
          <textarea
            :value="checkResults.get(item.id)?.failureReason"
            @input="updateFailureReason(item.id, ($event.target as HTMLTextAreaElement).value)"
            rows="2"
            class="mt-1 w-full rounded-lg border border-red-200 px-3 py-2"
            :placeholder="isAr ? 'يرجى توضيح سبب عدم الاجتياز' : 'Please explain the failure reason'"
          ></textarea>
        </div>

        <!-- Notes -->
        <div class="mt-2">
          <input
            :value="checkResults.get(item.id)?.notes"
            @input="updateNotes(item.id, ($event.target as HTMLInputElement).value)"
            class="w-full rounded-lg border border-gray-200 px-3 py-1.5 text-sm"
            :placeholder="isAr ? 'ملاحظات (اختياري)' : 'Notes (optional)'"
          />
        </div>
      </div>
    </div>

    <!-- Submit Button -->
    <div class="mt-6 flex justify-end">
      <button
        @click="handleSubmit"
        :disabled="saving || !allChecked"
        class="rounded-lg bg-indigo-600 px-8 py-3 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
      >
        {{ saving ? (isAr ? 'جاري الحفظ...' : 'Saving...') : (isAr ? 'حفظ نتائج الفحص' : 'Save Compliance Results') }}
      </button>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex justify-center py-12">
      <div class="h-8 w-8 animate-spin rounded-full border-4 border-indigo-600 border-t-transparent"></div>
    </div>
  </div>
</template>
