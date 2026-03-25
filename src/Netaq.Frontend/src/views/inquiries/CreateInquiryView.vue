<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useInquiryStore } from '../../stores/inquiries'
import { useTenderStore } from '../../stores/tenders'
import { getCurrentLocale } from '../../i18n'

const { t } = useI18n()
const router = useRouter()
const inquiryStore = useInquiryStore()
const tenderStore = useTenderStore()
const locale = computed(() => getCurrentLocale())

const form = ref({
  tenderId: '',
  subjectAr: '',
  subjectEn: '',
  questionAr: '',
  questionEn: '',
  priority: 'Medium',
})

const isSubmitting = ref(false)
const errors = ref<Record<string, string>>({})

function getTenderName(tender: any) {
  return locale.value === 'ar' ? tender.titleAr : tender.titleEn
}

function validate(): boolean {
  errors.value = {}
  if (!form.value.tenderId) errors.value.tenderId = t('common.required')
  if (!form.value.subjectAr.trim()) errors.value.subjectAr = t('common.required')
  if (!form.value.subjectEn.trim()) errors.value.subjectEn = t('common.required')
  if (!form.value.questionAr.trim()) errors.value.questionAr = t('common.required')
  return Object.keys(errors.value).length === 0
}

async function handleSubmit() {
  if (!validate()) return
  isSubmitting.value = true
  const id = await inquiryStore.createInquiry({
    tenderId: form.value.tenderId,
    subjectAr: form.value.subjectAr,
    subjectEn: form.value.subjectEn,
    questionAr: form.value.questionAr,
    questionEn: form.value.questionEn,
    priority: form.value.priority,
  })
  isSubmitting.value = false
  if (id) {
    router.push(`/inquiries/${id}`)
  }
}

onMounted(() => {
  tenderStore.fetchTenders({ pageSize: 100 })
})
</script>

<template>
  <div class="p-6 max-w-4xl mx-auto">
    <!-- Header -->
    <div class="flex items-center gap-4 mb-6">
      <button @click="router.push('/inquiries')" class="p-2 hover:bg-gray-100 rounded-lg transition-colors">
        <svg class="w-5 h-5 text-gray-600 rtl:rotate-180" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
        </svg>
      </button>
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ t('inquiries.create') }}</h1>
        <p class="text-gray-500 mt-1">{{ t('inquiries.createDesc') }}</p>
      </div>
    </div>

    <!-- Error -->
    <div v-if="inquiryStore.error" class="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
      <p class="text-red-700 text-sm">{{ inquiryStore.error }}</p>
    </div>

    <form @submit.prevent="handleSubmit" class="space-y-6">
      <!-- Tender & Priority -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <h2 class="text-lg font-semibold text-gray-900 mb-4">{{ t('inquiries.basicInfo') }}</h2>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('inquiries.selectTender') }} *</label>
            <select v-model="form.tenderId"
              class="w-full px-4 py-2.5 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              :class="errors.tenderId ? 'border-red-300' : 'border-gray-300'">
              <option value="">{{ t('inquiries.chooseTender') }}</option>
              <option v-for="tender in tenderStore.tenders" :key="tender.id" :value="tender.id">
                {{ getTenderName(tender) }} ({{ tender.referenceNumber }})
              </option>
            </select>
            <p v-if="errors.tenderId" class="text-red-500 text-xs mt-1">{{ errors.tenderId }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('inquiries.priority') }}</label>
            <select v-model="form.priority"
              class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500">
              <option value="Low">{{ t('inquiries.priorityLow') }}</option>
              <option value="Medium">{{ t('inquiries.priorityMedium') }}</option>
              <option value="High">{{ t('inquiries.priorityHigh') }}</option>
              <option value="Critical">{{ t('inquiries.priorityCritical') }}</option>
            </select>
          </div>
        </div>
      </div>

      <!-- Subject & Question -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <h2 class="text-lg font-semibold text-gray-900 mb-4">{{ t('inquiries.details') }}</h2>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('inquiries.subjectAr') }} *</label>
            <input v-model="form.subjectAr" type="text" dir="rtl"
              class="w-full px-4 py-2.5 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              :class="errors.subjectAr ? 'border-red-300' : 'border-gray-300'" />
            <p v-if="errors.subjectAr" class="text-red-500 text-xs mt-1">{{ errors.subjectAr }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('inquiries.subjectEn') }} *</label>
            <input v-model="form.subjectEn" type="text" dir="ltr"
              class="w-full px-4 py-2.5 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              :class="errors.subjectEn ? 'border-red-300' : 'border-gray-300'" />
            <p v-if="errors.subjectEn" class="text-red-500 text-xs mt-1">{{ errors.subjectEn }}</p>
          </div>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('inquiries.questionAr') }} *</label>
            <textarea v-model="form.questionAr" rows="5" dir="rtl"
              class="w-full px-4 py-2.5 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              :class="errors.questionAr ? 'border-red-300' : 'border-gray-300'"></textarea>
            <p v-if="errors.questionAr" class="text-red-500 text-xs mt-1">{{ errors.questionAr }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('inquiries.questionEn') }}</label>
            <textarea v-model="form.questionEn" rows="5" dir="ltr"
              class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"></textarea>
          </div>
        </div>
      </div>

      <!-- Actions -->
      <div class="flex justify-end gap-3">
        <button type="button" @click="router.push('/inquiries')"
          class="px-6 py-2.5 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors text-sm font-medium">
          {{ t('common.cancel') }}
        </button>
        <button type="submit" :disabled="isSubmitting"
          class="px-6 py-2.5 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors text-sm font-medium disabled:opacity-50">
          <span v-if="isSubmitting" class="flex items-center gap-2">
            <div class="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
            {{ t('common.saving') }}
          </span>
          <span v-else>{{ t('inquiries.submit') }}</span>
        </button>
      </div>
    </form>
  </div>
</template>
