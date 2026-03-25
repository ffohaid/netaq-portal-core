<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useInquiryStore } from '../../stores/inquiries'
import { getCurrentLocale } from '../../i18n'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useInquiryStore()
const locale = computed(() => getCurrentLocale())
const inquiryId = route.params.id as string

const showAnswerForm = ref(false)
const answerForm = ref({ answerAr: '', answerEn: '' })
const isSubmitting = ref(false)

function getText(ar?: string, en?: string) {
  return locale.value === 'ar' ? ar : en
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString(locale.value === 'ar' ? 'ar-SA' : 'en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function getStatusClass(status: string) {
  const map: Record<string, string> = {
    Pending: 'bg-yellow-100 text-yellow-700',
    Answered: 'bg-green-100 text-green-700',
    Rejected: 'bg-red-100 text-red-700',
    Escalated: 'bg-orange-100 text-orange-700',
  }
  return map[status] || 'bg-gray-100 text-gray-700'
}

async function submitAnswer() {
  if (!answerForm.value.answerAr.trim() && !answerForm.value.answerEn.trim()) return
  isSubmitting.value = true
  const success = await store.answerInquiry(inquiryId, answerForm.value)
  isSubmitting.value = false
  if (success) {
    showAnswerForm.value = false
  }
}

async function handleEscalate() {
  await store.escalateInquiry(inquiryId)
}

onMounted(() => store.fetchInquiry(inquiryId))
</script>

<template>
  <div class="p-6 max-w-4xl mx-auto">
    <!-- Loading -->
    <div v-if="store.isLoading && !store.currentInquiry" class="flex justify-center py-12">
      <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-primary-600"></div>
    </div>

    <template v-else-if="store.currentInquiry">
      <!-- Header -->
      <div class="flex items-start justify-between mb-6">
        <div class="flex items-center gap-4">
          <button @click="router.push('/inquiries')" class="p-2 hover:bg-gray-100 rounded-lg transition-colors">
            <svg class="w-5 h-5 text-gray-600 rtl:rotate-180" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
          </button>
          <div>
            <h1 class="text-2xl font-bold text-gray-900">{{ getText(store.currentInquiry.subjectAr, store.currentInquiry.subjectEn) }}</h1>
            <div class="flex items-center gap-3 mt-2">
              <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium" :class="getStatusClass(store.currentInquiry.status)">
                {{ t(`inquiries.status${store.currentInquiry.status}`) }}
              </span>
              <span class="text-sm text-gray-500">{{ formatDate(store.currentInquiry.submittedAt) }}</span>
            </div>
          </div>
        </div>
        <div v-if="store.currentInquiry.status === 'Pending'" class="flex gap-2">
          <button @click="handleEscalate" class="px-4 py-2 text-orange-600 border border-orange-300 rounded-lg hover:bg-orange-50 text-sm">
            {{ t('inquiries.escalate') }}
          </button>
          <button @click="showAnswerForm = true" class="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 text-sm">
            {{ t('inquiries.answer') }}
          </button>
        </div>
      </div>

      <!-- Tender Reference -->
      <div class="bg-blue-50 border border-blue-200 rounded-xl p-4 mb-6">
        <div class="text-sm text-blue-600 font-medium">{{ t('inquiries.relatedTender') }}</div>
        <div class="text-blue-900 font-semibold mt-1">
          {{ getText(store.currentInquiry.tenderTitleAr, store.currentInquiry.tenderTitleEn) }}
        </div>
        <div class="text-blue-600 text-xs font-mono mt-1">{{ store.currentInquiry.tenderReferenceNumber }}</div>
      </div>

      <!-- Question -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 mb-6">
        <div class="flex items-center gap-3 mb-4">
          <div class="w-10 h-10 bg-primary-100 text-primary-700 rounded-full flex items-center justify-center text-sm font-bold">
            {{ (getText(store.currentInquiry.submittedByNameAr, store.currentInquiry.submittedByNameEn) || '?').charAt(0) }}
          </div>
          <div>
            <div class="text-sm font-medium text-gray-900">{{ getText(store.currentInquiry.submittedByNameAr, store.currentInquiry.submittedByNameEn) }}</div>
            <div class="text-xs text-gray-500">{{ t('inquiries.submittedBy') }}</div>
          </div>
        </div>
        <h3 class="text-lg font-semibold text-gray-900 mb-2">{{ t('inquiries.question') }}</h3>
        <p class="text-gray-700 leading-relaxed whitespace-pre-wrap">{{ getText(store.currentInquiry.questionAr, store.currentInquiry.questionEn) }}</p>
      </div>

      <!-- Answer -->
      <div v-if="store.currentInquiry.answerAr || store.currentInquiry.answerEn" class="bg-green-50 rounded-xl border border-green-200 p-6 mb-6">
        <div class="flex items-center gap-3 mb-4">
          <div class="w-10 h-10 bg-green-100 text-green-700 rounded-full flex items-center justify-center">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
            </svg>
          </div>
          <div>
            <div class="text-sm font-medium text-green-900">{{ getText(store.currentInquiry.answeredByNameAr, store.currentInquiry.answeredByNameEn) }}</div>
            <div class="text-xs text-green-600">{{ store.currentInquiry.answeredAt ? formatDate(store.currentInquiry.answeredAt) : '' }}</div>
          </div>
        </div>
        <h3 class="text-lg font-semibold text-green-900 mb-2">{{ t('inquiries.answerLabel') }}</h3>
        <p class="text-green-800 leading-relaxed whitespace-pre-wrap">{{ getText(store.currentInquiry.answerAr, store.currentInquiry.answerEn) }}</p>
      </div>

      <!-- Answer Form -->
      <div v-if="showAnswerForm" class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <h3 class="text-lg font-semibold text-gray-900 mb-4">{{ t('inquiries.writeAnswer') }}</h3>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('inquiries.answerAr') }}</label>
            <textarea v-model="answerForm.answerAr" rows="5" dir="rtl"
              class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"></textarea>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('inquiries.answerEn') }}</label>
            <textarea v-model="answerForm.answerEn" rows="5" dir="ltr"
              class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"></textarea>
          </div>
        </div>
        <div class="flex justify-end gap-3">
          <button @click="showAnswerForm = false" class="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 text-sm">
            {{ t('common.cancel') }}
          </button>
          <button @click="submitAnswer" :disabled="isSubmitting" class="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 text-sm disabled:opacity-50">
            {{ isSubmitting ? t('common.saving') : t('inquiries.submitAnswer') }}
          </button>
        </div>
      </div>
    </template>
  </div>
</template>
