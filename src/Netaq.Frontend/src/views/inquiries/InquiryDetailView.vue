<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useInquiryStore } from '../../stores/inquiries'
import { getCurrentLocale } from '../../i18n'
import api from '../../services/api'
import type { ApiResponse, PaginatedResponse } from '../../types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useInquiryStore()
const locale = computed(() => getCurrentLocale())
const inquiryId = route.params.id as string

// Forms
const showAnswerForm = ref(false)
const showEscalateForm = ref(false)
const showCloseForm = ref(false)
const showAssignForm = ref(false)
const showNoteForm = ref(false)

const answerForm = ref({ answerAr: '', answerEn: '' })
const escalateReason = ref('')
const closeReason = ref('')
const assignUserId = ref('')
const noteText = ref('')

const isSubmitting = ref(false)
const availableUsers = ref<any[]>([])

function getText(ar?: string, en?: string) {
  return locale.value === 'ar' ? (ar || en || '-') : (en || ar || '-')
}

function formatDate(dateStr: string) {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString(locale.value === 'ar' ? 'ar-SA' : 'en-US', {
    year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit',
  })
}

function getStatusClass(status: string) {
  const map: Record<string, string> = {
    Submitted: 'bg-blue-100 text-blue-700 border-blue-200',
    UnderReview: 'bg-yellow-100 text-yellow-700 border-yellow-200',
    Responded: 'bg-green-100 text-green-700 border-green-200',
    Closed: 'bg-gray-100 text-gray-700 border-gray-200',
    Escalated: 'bg-red-100 text-red-700 border-red-200',
    Reopened: 'bg-purple-100 text-purple-700 border-purple-200',
  }
  return map[status] || 'bg-gray-100 text-gray-700 border-gray-200'
}
function getStatusLabel(status: string) {
  if (locale.value === 'ar') {
    const m: Record<string, string> = { Submitted: 'جديد', UnderReview: 'قيد المراجعة', Responded: 'تمت الإجابة', Closed: 'مغلق', Escalated: 'مُصعّد', Reopened: 'مُعاد فتحه' }
    return m[status] || status
  }
  const m: Record<string, string> = { Submitted: 'New', UnderReview: 'Under Review', Responded: 'Responded', Closed: 'Closed', Escalated: 'Escalated', Reopened: 'Reopened' }
  return m[status] || status
}
function getPriorityClass(priority: string) {
  const map: Record<string, string> = { Low: 'text-blue-600', Normal: 'text-gray-600', High: 'text-orange-600', Urgent: 'text-red-600' }
  return map[priority] || 'text-gray-600'
}
function getPriorityLabel(priority: string) {
  if (locale.value === 'ar') {
    const m: Record<string, string> = { Low: 'منخفضة', Normal: 'عادية', High: 'عالية', Urgent: 'عاجلة' }
    return m[priority] || priority
  }
  return priority
}
function getCategoryLabel(category: string) {
  if (locale.value === 'ar') {
    const m: Record<string, string> = { General: 'عام', Technical: 'فني', Financial: 'مالي', Legal: 'قانوني', Administrative: 'إداري', Clarification: 'توضيح' }
    return m[category] || category
  }
  return category
}

// Check if actions are allowed based on status
const canRespond = computed(() => {
  const s = store.currentInquiry?.status
  return s === 'Submitted' || s === 'UnderReview' || s === 'Reopened'
})
const canEscalate = computed(() => {
  const s = store.currentInquiry?.status
  return s === 'Submitted' || s === 'UnderReview' || s === 'Reopened'
})
const canClose = computed(() => {
  const s = store.currentInquiry?.status
  return s === 'Responded' || s === 'Escalated'
})
const canReopen = computed(() => {
  return store.currentInquiry?.status === 'Closed'
})
const canAssign = computed(() => {
  const s = store.currentInquiry?.status
  return s !== 'Closed'
})

async function loadUsers() {
  try {
    const response = await api.get<ApiResponse<PaginatedResponse<any>>>('/users?pageSize=100')
    if (response.data.isSuccess && response.data.data) {
      availableUsers.value = response.data.data.items
    }
  } catch { /* silent */ }
}

async function submitAnswer() {
  if (!answerForm.value.answerAr.trim() && !answerForm.value.answerEn.trim()) return
  isSubmitting.value = true
  const success = await store.respondToInquiry(inquiryId, {
    responseAr: answerForm.value.answerAr,
    responseEn: answerForm.value.answerEn,
  })
  isSubmitting.value = false
  if (success) {
    showAnswerForm.value = false
    answerForm.value = { answerAr: '', answerEn: '' }
    await store.fetchInquiry(inquiryId)
  }
}

async function handleEscalate() {
  if (!escalateReason.value.trim()) return
  isSubmitting.value = true
  const success = await store.escalateInquiry(inquiryId, '', escalateReason.value)
  isSubmitting.value = false
  if (success) {
    showEscalateForm.value = false
    escalateReason.value = ''
    await store.fetchInquiry(inquiryId)
  }
}

async function handleClose() {
  isSubmitting.value = true
  const success = await store.closeInquiry(inquiryId)
  isSubmitting.value = false
  if (success) {
    showCloseForm.value = false
    closeReason.value = ''
    await store.fetchInquiry(inquiryId)
  }
}

async function handleReopen() {
  isSubmitting.value = true
  const success = await store.reopenInquiry(inquiryId)
  isSubmitting.value = false
  if (success) {
    await store.fetchInquiry(inquiryId)
  }
}

async function handleAssign() {
  if (!assignUserId.value) return
  isSubmitting.value = true
  const success = await store.assignInquiry(inquiryId, assignUserId.value)
  isSubmitting.value = false
  if (success) {
    showAssignForm.value = false
    assignUserId.value = ''
    await store.fetchInquiry(inquiryId)
  }
}

async function handleAddNote() {
  if (!noteText.value.trim()) return
  isSubmitting.value = true
  const success = await store.addNote(inquiryId, noteText.value, noteText.value)
  isSubmitting.value = false
  if (success) {
    showNoteForm.value = false
    noteText.value = ''
    await store.fetchInquiry(inquiryId)
  }
}

onMounted(() => {
  store.fetchInquiry(inquiryId)
  loadUsers()
})
</script>

<template>
  <div class="p-6 max-w-5xl mx-auto">
    <!-- Loading -->
    <div v-if="store.isLoading && !store.currentInquiry" class="flex justify-center py-12">
      <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-primary-600"></div>
    </div>

    <template v-else-if="store.currentInquiry">
      <!-- Back Button & Header -->
      <div class="flex items-start justify-between mb-6">
        <div class="flex items-center gap-4">
          <button @click="router.push('/inquiries')" class="p-2 hover:bg-gray-100 rounded-lg transition-colors">
            <svg class="w-5 h-5 text-gray-600 rtl:rotate-180" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
          </button>
          <div>
            <h1 class="text-2xl font-bold text-gray-900">{{ getText(store.currentInquiry.subjectAr, store.currentInquiry.subjectEn) }}</h1>
            <div class="flex items-center gap-3 mt-2 flex-wrap">
              <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border" :class="getStatusClass(store.currentInquiry.status)">
                {{ getStatusLabel(store.currentInquiry.status) }}
              </span>
              <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-700">
                {{ getCategoryLabel(store.currentInquiry.category) }}
              </span>
              <span class="text-xs font-medium" :class="getPriorityClass(store.currentInquiry.priority)">
                {{ locale === 'ar' ? 'الأولوية:' : 'Priority:' }} {{ getPriorityLabel(store.currentInquiry.priority) }}
              </span>
              <span class="text-xs text-gray-500">{{ formatDate(store.currentInquiry.createdAt || store.currentInquiry.submittedAt) }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Action Buttons Bar -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4 mb-6">
        <div class="flex items-center gap-2 flex-wrap">
          <button v-if="canRespond" @click="showAnswerForm = !showAnswerForm" class="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 text-sm font-medium flex items-center gap-2">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h10a8 8 0 018 8v2M3 10l6 6m-6-6l6-6" /></svg>
            {{ locale === 'ar' ? 'الرد على الاستفسار' : 'Respond' }}
          </button>
          <button v-if="canEscalate" @click="showEscalateForm = !showEscalateForm" class="px-4 py-2 text-orange-600 border border-orange-300 rounded-lg hover:bg-orange-50 text-sm font-medium flex items-center gap-2">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 10l7-7m0 0l7 7m-7-7v18" /></svg>
            {{ locale === 'ar' ? 'تصعيد' : 'Escalate' }}
          </button>
          <button v-if="canAssign" @click="showAssignForm = !showAssignForm" class="px-4 py-2 text-blue-600 border border-blue-300 rounded-lg hover:bg-blue-50 text-sm font-medium flex items-center gap-2">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" /></svg>
            {{ locale === 'ar' ? 'إسناد' : 'Assign' }}
          </button>
          <button @click="showNoteForm = !showNoteForm" class="px-4 py-2 text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50 text-sm font-medium flex items-center gap-2">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
            {{ locale === 'ar' ? 'ملاحظة داخلية' : 'Internal Note' }}
          </button>
          <button v-if="canClose" @click="showCloseForm = true" class="px-4 py-2 text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50 text-sm font-medium flex items-center gap-2">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" /></svg>
            {{ locale === 'ar' ? 'إغلاق الاستفسار' : 'Close Inquiry' }}
          </button>
          <button v-if="canReopen" @click="handleReopen" :disabled="isSubmitting" class="px-4 py-2 text-purple-600 border border-purple-300 rounded-lg hover:bg-purple-50 text-sm font-medium flex items-center gap-2">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" /></svg>
            {{ locale === 'ar' ? 'إعادة فتح' : 'Reopen' }}
          </button>
        </div>
      </div>

      <!-- Tender Reference Card -->
      <div class="bg-blue-50 border border-blue-200 rounded-xl p-4 mb-6">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 bg-blue-100 rounded-lg flex items-center justify-center">
            <svg class="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
          </div>
          <div>
            <div class="text-sm text-blue-600 font-medium">{{ locale === 'ar' ? 'المنافسة المرتبطة' : 'Related Tender' }}</div>
            <div class="text-blue-900 font-semibold">{{ getText(store.currentInquiry.tenderTitleAr, store.currentInquiry.tenderTitleEn) }}</div>
            <div v-if="store.currentInquiry.tenderReferenceNumber" class="text-blue-600 text-xs font-mono mt-0.5">{{ store.currentInquiry.tenderReferenceNumber }}</div>
          </div>
        </div>
      </div>

      <!-- Info Cards Grid -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
          <div class="text-xs text-gray-500 mb-1">{{ locale === 'ar' ? 'مقدم الاستفسار' : 'Submitted By' }}</div>
          <div class="text-sm font-medium text-gray-900">{{ getText(store.currentInquiry.submittedByNameAr || store.currentInquiry.submittedByUserNameAr, store.currentInquiry.submittedByNameEn || store.currentInquiry.submittedByUserNameEn) }}</div>
        </div>
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
          <div class="text-xs text-gray-500 mb-1">{{ locale === 'ar' ? 'المسند إليه' : 'Assigned To' }}</div>
          <div class="text-sm font-medium text-gray-900">
            {{ store.currentInquiry.assignedToUserNameAr || store.currentInquiry.assignedToUserNameEn ? getText(store.currentInquiry.assignedToUserNameAr, store.currentInquiry.assignedToUserNameEn) : (locale === 'ar' ? 'غير مسند' : 'Unassigned') }}
          </div>
        </div>
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
          <div class="text-xs text-gray-500 mb-1">{{ locale === 'ar' ? 'الموعد النهائي' : 'Deadline' }}</div>
          <div class="text-sm font-medium text-gray-900">{{ store.currentInquiry.deadline ? formatDate(store.currentInquiry.deadline) : (locale === 'ar' ? 'غير محدد' : 'Not set') }}</div>
        </div>
      </div>

      <!-- Question Card -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 mb-6">
        <div class="flex items-center gap-3 mb-4">
          <div class="w-10 h-10 bg-primary-100 text-primary-700 rounded-full flex items-center justify-center text-sm font-bold">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
          </div>
          <h3 class="text-lg font-semibold text-gray-900">{{ locale === 'ar' ? 'نص الاستفسار' : 'Inquiry Question' }}</h3>
        </div>
        <div class="bg-gray-50 rounded-lg p-4">
          <p class="text-gray-700 leading-relaxed whitespace-pre-wrap">{{ getText(store.currentInquiry.questionAr, store.currentInquiry.questionEn) }}</p>
        </div>
        <!-- Show both languages if available -->
        <div v-if="store.currentInquiry.questionAr && store.currentInquiry.questionEn" class="mt-3">
          <button @click="(store.currentInquiry as any)._showOtherLang = !(store.currentInquiry as any)._showOtherLang" class="text-xs text-primary-600 hover:underline">
            {{ locale === 'ar' ? 'عرض النص الإنجليزي' : 'Show Arabic text' }}
          </button>
          <div v-if="(store.currentInquiry as any)._showOtherLang" class="bg-gray-50 rounded-lg p-4 mt-2">
            <p class="text-gray-600 leading-relaxed whitespace-pre-wrap" :dir="locale === 'ar' ? 'ltr' : 'rtl'">
              {{ locale === 'ar' ? store.currentInquiry.questionEn : store.currentInquiry.questionAr }}
            </p>
          </div>
        </div>
      </div>

      <!-- Answer Card -->
      <div v-if="store.currentInquiry.responseAr || store.currentInquiry.responseEn" class="bg-green-50 rounded-xl border border-green-200 p-6 mb-6">
        <div class="flex items-center gap-3 mb-4">
          <div class="w-10 h-10 bg-green-100 text-green-700 rounded-full flex items-center justify-center">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" /></svg>
          </div>
          <div>
            <h3 class="text-lg font-semibold text-green-900">{{ locale === 'ar' ? 'الرد الرسمي' : 'Official Response' }}</h3>
            <div class="text-xs text-green-600">
              {{ getText(store.currentInquiry.submittedByUserNameAr, store.currentInquiry.submittedByUserNameEn) }}
              <span v-if="store.currentInquiry.respondedAt"> - {{ formatDate(store.currentInquiry.respondedAt) }}</span>
            </div>
          </div>
        </div>
        <div class="bg-green-100/50 rounded-lg p-4">
          <p class="text-green-800 leading-relaxed whitespace-pre-wrap">{{ getText(store.currentInquiry.responseAr, store.currentInquiry.responseEn) }}</p>
        </div>
      </div>

      <!-- Internal Notes -->
      <div v-if="store.currentInquiry.internalNotesAr || store.currentInquiry.internalNotesEn" class="bg-amber-50 rounded-xl border border-amber-200 p-6 mb-6">
        <div class="flex items-center gap-3 mb-3">
          <div class="w-8 h-8 bg-amber-100 text-amber-700 rounded-full flex items-center justify-center">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z" /></svg>
          </div>
          <h3 class="text-sm font-semibold text-amber-900">{{ locale === 'ar' ? 'ملاحظات داخلية (سرية)' : 'Internal Notes (Confidential)' }}</h3>
        </div>
        <p class="text-amber-800 text-sm leading-relaxed whitespace-pre-wrap">{{ getText(store.currentInquiry.internalNotesAr, store.currentInquiry.internalNotesEn) }}</p>
      </div>

      <!-- Answer Form -->
      <div v-if="showAnswerForm" class="bg-white rounded-xl shadow-sm border border-primary-200 p-6 mb-6">
        <h3 class="text-lg font-semibold text-gray-900 mb-4">{{ locale === 'ar' ? 'كتابة الرد' : 'Write Response' }}</h3>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ locale === 'ar' ? 'الرد (عربي)' : 'Response (Arabic)' }} *</label>
            <textarea v-model="answerForm.answerAr" rows="6" dir="rtl" :placeholder="locale === 'ar' ? 'اكتب الرد باللغة العربية...' : 'Write response in Arabic...'"
              class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm"></textarea>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ locale === 'ar' ? 'الرد (إنجليزي)' : 'Response (English)' }}</label>
            <textarea v-model="answerForm.answerEn" rows="6" dir="ltr" :placeholder="locale === 'ar' ? 'اكتب الرد باللغة الإنجليزية...' : 'Write response in English...'"
              class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm"></textarea>
          </div>
        </div>
        <div class="flex justify-end gap-3">
          <button @click="showAnswerForm = false" class="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 text-sm font-medium">{{ t('common.cancel') }}</button>
          <button @click="submitAnswer" :disabled="isSubmitting || (!answerForm.answerAr.trim() && !answerForm.answerEn.trim())"
            class="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 text-sm font-medium disabled:opacity-50 flex items-center gap-2">
            <div v-if="isSubmitting" class="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
            {{ locale === 'ar' ? 'إرسال الرد' : 'Submit Response' }}
          </button>
        </div>
      </div>

      <!-- Escalate Form -->
      <div v-if="showEscalateForm" class="bg-white rounded-xl shadow-sm border border-orange-200 p-6 mb-6">
        <h3 class="text-lg font-semibold text-orange-900 mb-4">{{ locale === 'ar' ? 'تصعيد الاستفسار' : 'Escalate Inquiry' }}</h3>
        <p class="text-sm text-gray-600 mb-3">{{ locale === 'ar' ? 'سيتم تصعيد هذا الاستفسار للمسؤول الأعلى. يرجى ذكر سبب التصعيد.' : 'This inquiry will be escalated to a higher authority. Please provide the reason.' }}</p>
        <textarea v-model="escalateReason" rows="3" :placeholder="locale === 'ar' ? 'سبب التصعيد...' : 'Escalation reason...'"
          class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-orange-500 focus:border-orange-500 text-sm mb-4"></textarea>
        <div class="flex justify-end gap-3">
          <button @click="showEscalateForm = false" class="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 text-sm font-medium">{{ t('common.cancel') }}</button>
          <button @click="handleEscalate" :disabled="isSubmitting || !escalateReason.trim()"
            class="px-4 py-2 bg-orange-600 text-white rounded-lg hover:bg-orange-700 text-sm font-medium disabled:opacity-50 flex items-center gap-2">
            <div v-if="isSubmitting" class="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
            {{ locale === 'ar' ? 'تأكيد التصعيد' : 'Confirm Escalation' }}
          </button>
        </div>
      </div>

      <!-- Close Confirmation -->
      <div v-if="showCloseForm" class="bg-white rounded-xl shadow-sm border border-gray-300 p-6 mb-6">
        <h3 class="text-lg font-semibold text-gray-900 mb-4">{{ locale === 'ar' ? 'إغلاق الاستفسار' : 'Close Inquiry' }}</h3>
        <p class="text-sm text-gray-600 mb-4">{{ locale === 'ar' ? 'هل أنت متأكد من إغلاق هذا الاستفسار؟ يمكنك إعادة فتحه لاحقاً إذا لزم الأمر.' : 'Are you sure you want to close this inquiry? You can reopen it later if needed.' }}</p>
        <div class="flex justify-end gap-3">
          <button @click="showCloseForm = false" class="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 text-sm font-medium">{{ t('common.cancel') }}</button>
          <button @click="handleClose" :disabled="isSubmitting"
            class="px-4 py-2 bg-gray-600 text-white rounded-lg hover:bg-gray-700 text-sm font-medium disabled:opacity-50 flex items-center gap-2">
            <div v-if="isSubmitting" class="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
            {{ locale === 'ar' ? 'تأكيد الإغلاق' : 'Confirm Close' }}
          </button>
        </div>
      </div>

      <!-- Assign Form -->
      <div v-if="showAssignForm" class="bg-white rounded-xl shadow-sm border border-blue-200 p-6 mb-6">
        <h3 class="text-lg font-semibold text-blue-900 mb-4">{{ locale === 'ar' ? 'إسناد الاستفسار' : 'Assign Inquiry' }}</h3>
        <select v-model="assignUserId" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 mb-4">
          <option value="">{{ locale === 'ar' ? '-- اختر المختص --' : '-- Select User --' }}</option>
          <option v-for="user in availableUsers" :key="user.id" :value="user.id">{{ getText(user.fullNameAr, user.fullNameEn) }} ({{ user.email }})</option>
        </select>
        <div class="flex justify-end gap-3">
          <button @click="showAssignForm = false" class="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 text-sm font-medium">{{ t('common.cancel') }}</button>
          <button @click="handleAssign" :disabled="isSubmitting || !assignUserId"
            class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 text-sm font-medium disabled:opacity-50 flex items-center gap-2">
            <div v-if="isSubmitting" class="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
            {{ locale === 'ar' ? 'تأكيد الإسناد' : 'Confirm Assignment' }}
          </button>
        </div>
      </div>

      <!-- Internal Note Form -->
      <div v-if="showNoteForm" class="bg-white rounded-xl shadow-sm border border-amber-200 p-6 mb-6">
        <h3 class="text-lg font-semibold text-amber-900 mb-2">{{ locale === 'ar' ? 'إضافة ملاحظة داخلية' : 'Add Internal Note' }}</h3>
        <p class="text-xs text-gray-500 mb-3">{{ locale === 'ar' ? 'الملاحظات الداخلية سرية ولا تظهر للمستفسر' : 'Internal notes are confidential and not visible to the inquirer' }}</p>
        <textarea v-model="noteText" rows="3" :placeholder="locale === 'ar' ? 'اكتب ملاحظتك هنا...' : 'Write your note here...'"
          class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-amber-500 text-sm mb-4"></textarea>
        <div class="flex justify-end gap-3">
          <button @click="showNoteForm = false" class="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 text-sm font-medium">{{ t('common.cancel') }}</button>
          <button @click="handleAddNote" :disabled="isSubmitting || !noteText.trim()"
            class="px-4 py-2 bg-amber-600 text-white rounded-lg hover:bg-amber-700 text-sm font-medium disabled:opacity-50 flex items-center gap-2">
            <div v-if="isSubmitting" class="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
            {{ locale === 'ar' ? 'حفظ الملاحظة' : 'Save Note' }}
          </button>
        </div>
      </div>

      <!-- Error -->
      <div v-if="store.error" class="bg-red-50 border border-red-200 rounded-xl p-4 mb-6">
        <p class="text-red-700 text-sm">{{ store.error }}</p>
      </div>
    </template>

    <!-- Not Found -->
    <div v-else class="text-center py-12">
      <svg class="mx-auto h-16 w-16 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
      <h3 class="mt-4 text-lg font-medium text-gray-900">{{ locale === 'ar' ? 'الاستفسار غير موجود' : 'Inquiry Not Found' }}</h3>
      <button @click="router.push('/inquiries')" class="mt-4 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 text-sm">
        {{ locale === 'ar' ? 'العودة للقائمة' : 'Back to List' }}
      </button>
    </div>
  </div>
</template>
