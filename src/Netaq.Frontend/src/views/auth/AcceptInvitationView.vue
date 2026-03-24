<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import api from '../../services/api'
import type { ApiResponse } from '../../types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const token = ref((route.query.token as string) || '')
const fullNameAr = ref('')
const fullNameEn = ref('')
const password = ref('')
const confirmPassword = ref('')
const phone = ref('')
const jobTitleAr = ref('')
const jobTitleEn = ref('')
const isLoading = ref(false)
const error = ref<string | null>(null)
const success = ref(false)

async function handleAccept() {
  if (password.value !== confirmPassword.value) {
    error.value = 'Passwords do not match'
    return
  }

  isLoading.value = true
  error.value = null

  try {
    const response = await api.post<ApiResponse<string>>('/invitation/accept', {
      token: token.value,
      fullNameAr: fullNameAr.value,
      fullNameEn: fullNameEn.value,
      password: password.value,
      phone: phone.value,
      jobTitleAr: jobTitleAr.value,
      jobTitleEn: jobTitleEn.value,
    })

    if (response.data.isSuccess) {
      success.value = true
      setTimeout(() => router.push({ name: 'Login' }), 3000)
    } else {
      error.value = response.data.error || 'Failed to accept invitation'
    }
  } catch (err: any) {
    error.value = err.response?.data?.error || 'An error occurred'
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div>
    <h2 class="text-xl font-bold text-gray-900 mb-6">{{ t('auth.acceptInvitation') }}</h2>

    <div v-if="success" class="p-4 bg-green-50 border border-green-200 rounded-lg text-center">
      <svg class="w-12 h-12 text-green-500 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
      </svg>
      <p class="text-green-700 font-medium">Account created successfully! Redirecting to login...</p>
    </div>

    <form v-else @submit.prevent="handleAccept" class="space-y-4">
      <div class="grid grid-cols-2 gap-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('auth.fullNameAr') }}</label>
          <input v-model="fullNameAr" type="text" required class="input-field" dir="rtl" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('auth.fullNameEn') }}</label>
          <input v-model="fullNameEn" type="text" required class="input-field" dir="ltr" />
        </div>
      </div>

      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('auth.phone') }}</label>
        <input v-model="phone" type="tel" class="input-field" dir="ltr" />
      </div>

      <div class="grid grid-cols-2 gap-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('auth.jobTitle') }} (AR)</label>
          <input v-model="jobTitleAr" type="text" class="input-field" dir="rtl" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('auth.jobTitle') }} (EN)</label>
          <input v-model="jobTitleEn" type="text" class="input-field" dir="ltr" />
        </div>
      </div>

      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('auth.password') }}</label>
        <input v-model="password" type="password" required minlength="8" class="input-field" dir="ltr" />
      </div>

      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('auth.confirmPassword') }}</label>
        <input v-model="confirmPassword" type="password" required class="input-field" dir="ltr" />
      </div>

      <div v-if="error" class="p-3 bg-danger-50 border border-danger-500/20 rounded-lg">
        <p class="text-sm text-danger-700">{{ error }}</p>
      </div>

      <button type="submit" :disabled="isLoading" class="btn-primary w-full">
        {{ isLoading ? t('common.loading') : t('auth.acceptInvitation') }}
      </button>
    </form>
  </div>
</template>
