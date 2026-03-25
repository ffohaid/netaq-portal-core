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
  chairUserId: '',
  memberUserIds: [] as string[],
})

const availableUsers = ref<any[]>([])
const isLoadingUsers = ref(false)
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
    chairUserId: form.value.chairUserId,
    memberUserIds: form.value.memberUserIds,
  })
  isSubmitting.value = false
  if (id) {
    router.push(`/committees/${id}`)
  }
}

onMounted(() => loadUsers())
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
        <h2 class="text-lg font-semibold text-gray-900 mb-4">{{ t('committees.basicInfo') }}</h2>
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

      <!-- Members Selection -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <h2 class="text-lg font-semibold text-gray-900 mb-4">{{ t('committees.selectMembers') }}</h2>
        <p v-if="errors.members" class="text-red-500 text-sm mb-3">{{ errors.members }}</p>
        <p v-if="errors.chairUserId" class="text-red-500 text-sm mb-3">{{ errors.chairUserId }}</p>

        <div v-if="isLoadingUsers" class="flex justify-center py-8">
          <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
        </div>

        <div v-else class="space-y-2">
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
                <div class="text-xs text-gray-500">{{ user.email }}</div>
              </div>
            </div>
            <div v-if="form.memberUserIds.includes(user.id)" class="flex items-center gap-2">
              <label class="text-xs text-gray-500">{{ t('committees.chair') }}</label>
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

        <div class="mt-3 text-sm text-gray-500">
          {{ t('committees.selectedCount', { count: form.memberUserIds.length }) }}
        </div>
      </div>

      <!-- Actions -->
      <div class="flex justify-end gap-3">
        <button type="button" @click="router.push('/committees')"
          class="px-6 py-2.5 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors text-sm font-medium">
          {{ t('common.cancel') }}
        </button>
        <button type="submit" :disabled="isSubmitting"
          class="px-6 py-2.5 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors text-sm font-medium disabled:opacity-50">
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
