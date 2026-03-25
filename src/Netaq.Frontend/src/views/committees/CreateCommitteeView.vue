<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useCommitteeStore } from '../../stores/committees'
import { getCurrentLocale } from '../../i18n'
import api from '../../services/api'
import type { ApiResponse, PaginatedResponse } from '../../types'

const { t } = useI18n()
const router = useRouter()
const store = useCommitteeStore()
const locale = computed(() => getCurrentLocale())

const form = ref({
  nameAr: '',
  nameEn: '',
  type: 'Permanent' as 'Permanent' | 'AdHoc',
  purposeAr: '',
  purposeEn: '',
  tenderId: '',
  startDate: '',
  endDate: '',
  formationDecisionNumber: '',
  chairUserId: '',
  memberUserIds: [] as string[],
})

const availableUsers = ref<any[]>([])
const availableTenders = ref<any[]>([])
const isLoadingUsers = ref(false)
const isLoadingTenders = ref(false)
const isSubmitting = ref(false)
const errors = ref<Record<string, string>>({})

async function loadUsers() {
  isLoadingUsers.value = true
  try {
    const response = await api.get<ApiResponse<PaginatedResponse<any>>>('/users?pageSize=100')
    if (response.data.isSuccess && response.data.data) {
      availableUsers.value = response.data.data.items
    }
  } catch {
    // silent
  } finally {
    isLoadingUsers.value = false
  }
}

async function loadTenders() {
  isLoadingTenders.value = true
  try {
    const response = await api.get<ApiResponse<PaginatedResponse<any>>>('/tenders?pageSize=100')
    if (response.data.isSuccess && response.data.data) {
      availableTenders.value = response.data.data.items
    }
  } catch {
    // silent
  } finally {
    isLoadingTenders.value = false
  }
}

function getUserName(user: any) {
  return locale.value === 'ar' ? user.fullNameAr : user.fullNameEn
}

function toggleMember(userId: string) {
  const idx = form.value.memberUserIds.indexOf(userId)
  if (idx === -1) {
    form.value.memberUserIds.push(userId)
  } else {
    form.value.memberUserIds.splice(idx, 1)
    if (form.value.chairUserId === userId) {
      form.value.chairUserId = ''
    }
  }
}

function validate(): boolean {
  errors.value = {}
  if (!form.value.nameAr.trim()) errors.value.nameAr = t('common.required')
  if (!form.value.nameEn.trim()) errors.value.nameEn = t('common.required')
  if (!form.value.chairUserId) errors.value.chairUserId = t('committees.selectChair')
  if (form.value.memberUserIds.length < 2) errors.value.members = t('committees.minMembers')
  if (form.value.type === 'AdHoc' && !form.value.tenderId) {
    errors.value.tenderId = locale.value === 'ar' ? 'يجب ربط اللجنة المؤقتة بمنافسة' : 'Temporary committee must be linked to a tender'
  }
  if (form.value.startDate && form.value.endDate && new Date(form.value.startDate) > new Date(form.value.endDate)) {
    errors.value.endDate = locale.value === 'ar' ? 'تاريخ الانتهاء يجب أن يكون بعد تاريخ البداية' : 'End date must be after start date'
  }
  return Object.keys(errors.value).length === 0
}

async function handleSubmit() {
  if (!validate()) return
  isSubmitting.value = true
  const id = await store.createCommittee({
    nameAr: form.value.nameAr,
    nameEn: form.value.nameEn,
    type: form.value.type,
    purposeAr: form.value.purposeAr,
    purposeEn: form.value.purposeEn,
    tenderId: form.value.tenderId || undefined,
    startDate: form.value.startDate || undefined,
    endDate: form.value.endDate || undefined,
    formationDecisionNumber: form.value.formationDecisionNumber || undefined,
    chairUserId: form.value.chairUserId,
    memberUserIds: form.value.memberUserIds,
  })
  isSubmitting.value = false
  if (id) {
    router.push(`/committees/${id}`)
  }
}

onMounted(() => {
  loadUsers()
  loadTenders()
})
</script>

<template>
  <div class="p-6 max-w-4xl mx-auto">
    <!-- Header -->
    <div class="flex items-center gap-4 mb-6">
      <button @click="router.push('/committees')" class="p-2 hover:bg-gray-100 rounded-lg transition-colors">
        <svg class="w-5 h-5 text-gray-600 rtl:rotate-180" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
        </svg>
      </button>
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ t('committees.create') }}</h1>
        <p class="text-gray-500 mt-1">{{ t('committees.createDesc') }}</p>
      </div>
    </div>

    <!-- Error -->
    <div v-if="store.error" class="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
      <p class="text-red-700 text-sm">{{ store.error }}</p>
    </div>

    <form @submit.prevent="handleSubmit" class="space-y-6">
      <!-- Basic Info -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <h2 class="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
          <svg class="w-5 h-5 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          {{ t('committees.basicInfo') }}
        </h2>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('committees.nameAr') }} *</label>
            <input v-model="form.nameAr" type="text" dir="rtl"
              class="w-full px-4 py-2.5 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              :class="errors.nameAr ? 'border-red-300' : 'border-gray-300'" />
            <p v-if="errors.nameAr" class="text-red-500 text-xs mt-1">{{ errors.nameAr }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('committees.nameEn') }} *</label>
            <input v-model="form.nameEn" type="text" dir="ltr"
              class="w-full px-4 py-2.5 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              :class="errors.nameEn ? 'border-red-300' : 'border-gray-300'" />
            <p v-if="errors.nameEn" class="text-red-500 text-xs mt-1">{{ errors.nameEn }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('committees.type') }}</label>
            <select v-model="form.type"
              class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500">
              <option value="Permanent">{{ t('committees.permanent') }}</option>
              <option value="AdHoc">{{ t('committees.adHoc') }}</option>
            </select>
          </div>
          <div v-if="form.type === 'AdHoc'">
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ locale === 'ar' ? 'المنافسة المرتبطة' : 'Linked Tender' }} *</label>
            <select v-model="form.tenderId"
              class="w-full px-4 py-2.5 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              :class="errors.tenderId ? 'border-red-300' : 'border-gray-300'">
              <option value="">{{ locale === 'ar' ? '-- اختر المنافسة --' : '-- Select Tender --' }}</option>
              <option v-for="tender in availableTenders" :key="tender.id" :value="tender.id">
                {{ locale === 'ar' ? tender.titleAr : tender.titleEn }} ({{ tender.referenceNumber }})
              </option>
            </select>
            <p v-if="errors.tenderId" class="text-red-500 text-xs mt-1">{{ errors.tenderId }}</p>
          </div>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mt-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('committees.purposeAr') }}</label>
            <textarea v-model="form.purposeAr" rows="3" dir="rtl"
              class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"></textarea>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('committees.purposeEn') }}</label>
            <textarea v-model="form.purposeEn" rows="3" dir="ltr"
              class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"></textarea>
          </div>
        </div>
      </div>

      <!-- Committee Period & Formation Decision -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <h2 class="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
          <svg class="w-5 h-5 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
          {{ locale === 'ar' ? 'فترة اللجنة وقرار التشكيل' : 'Committee Period & Formation Decision' }}
        </h2>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ locale === 'ar' ? 'تاريخ البداية' : 'Start Date' }}</label>
            <input v-model="form.startDate" type="date"
              class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ locale === 'ar' ? 'تاريخ الانتهاء' : 'End Date' }}</label>
            <input v-model="form.endDate" type="date"
              class="w-full px-4 py-2.5 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              :class="errors.endDate ? 'border-red-300' : 'border-gray-300'" />
            <p v-if="errors.endDate" class="text-red-500 text-xs mt-1">{{ errors.endDate }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ locale === 'ar' ? 'رقم قرار التشكيل' : 'Formation Decision No.' }}</label>
            <input v-model="form.formationDecisionNumber" type="text"
              class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              :placeholder="locale === 'ar' ? 'مثال: ق-1446/25' : 'e.g., D-1446/25'" />
          </div>
        </div>
        <div class="mt-4 p-3 bg-blue-50 border border-blue-200 rounded-lg">
          <p class="text-sm text-blue-700 flex items-start gap-2">
            <svg class="w-4 h-4 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            {{ locale === 'ar'
              ? 'يمكن إرفاق قرار التشكيل كمستند بعد إنشاء اللجنة من صفحة تفاصيل اللجنة. يجب أن يتضمن القرار أسماء الأعضاء وصلاحياتهم وفترة العمل.'
              : 'The formation decision document can be attached after creating the committee from the committee details page. The decision should include member names, authorities, and work period.' }}
          </p>
        </div>
      </div>

      <!-- Members Selection -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <h2 class="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
          <svg class="w-5 h-5 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z" />
          </svg>
          {{ t('committees.selectMembers') }}
        </h2>
        <p class="text-sm text-gray-500 mb-4">
          {{ locale === 'ar'
            ? 'اختر أعضاء اللجنة (3 أعضاء على الأقل) وحدد رئيس اللجنة. يجب أن يكون عدد الأعضاء فردياً وفقاً لنظام المنافسات والمشتريات الحكومية.'
            : 'Select committee members (minimum 3) and designate the chair. The number of members should be odd per Government Tenders and Procurement Law.' }}
        </p>
        <p v-if="errors.members" class="text-red-500 text-sm mb-3">{{ errors.members }}</p>
        <p v-if="errors.chairUserId" class="text-red-500 text-sm mb-3">{{ errors.chairUserId }}</p>

        <div v-if="isLoadingUsers" class="flex justify-center py-8">
          <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
        </div>

        <div v-else class="space-y-2 max-h-96 overflow-y-auto">
          <div
            v-for="user in availableUsers"
            :key="user.id"
            class="flex items-center justify-between p-3 rounded-lg border transition-colors cursor-pointer"
            :class="form.memberUserIds.includes(user.id) ? 'border-primary-300 bg-primary-50' : 'border-gray-200 hover:bg-gray-50'"
            @click="toggleMember(user.id)"
          >
            <div class="flex items-center gap-3">
              <input type="checkbox" :checked="form.memberUserIds.includes(user.id)" class="rounded border-gray-300 text-primary-600 focus:ring-primary-500" @click.stop />
              <div class="w-8 h-8 bg-primary-100 text-primary-700 rounded-full flex items-center justify-center text-sm font-bold">
                {{ getUserName(user).charAt(0) }}
              </div>
              <div>
                <div class="text-sm font-medium text-gray-900">{{ getUserName(user) }}</div>
                <div class="text-xs text-gray-500">{{ user.email }} &middot; {{ user.role }}</div>
              </div>
            </div>
            <div v-if="form.memberUserIds.includes(user.id)" class="flex items-center gap-2">
              <label class="text-xs text-gray-500 whitespace-nowrap">{{ t('committees.chair') }}</label>
              <input
                type="radio"
                :value="user.id"
                v-model="form.chairUserId"
                class="text-primary-600 focus:ring-primary-500"
                @click.stop
              />
            </div>
          </div>
        </div>

        <div class="mt-3 flex items-center justify-between">
          <span class="text-sm text-gray-500">
            {{ t('committees.selectedCount', { count: form.memberUserIds.length }) }}
          </span>
          <span v-if="form.memberUserIds.length > 0 && form.memberUserIds.length % 2 === 0" class="text-xs text-amber-600 flex items-center gap-1">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z" />
            </svg>
            {{ locale === 'ar' ? 'يفضل أن يكون عدد الأعضاء فردياً' : 'Odd number of members is recommended' }}
          </span>
        </div>
      </div>

      <!-- Actions -->
      <div class="flex justify-end gap-3">
        <button type="button" @click="router.push('/committees')"
          class="px-6 py-2.5 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors text-sm font-medium">
          {{ t('common.cancel') }}
        </button>
        <button type="submit" :disabled="isSubmitting"
          class="px-6 py-2.5 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors text-sm font-medium disabled:opacity-50 flex items-center gap-2">
          <span v-if="isSubmitting" class="flex items-center gap-2">
            <div class="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
            {{ t('common.saving') }}
          </span>
          <span v-else>{{ t('committees.create') }}</span>
        </button>
      </div>
    </form>
  </div>
</template>
