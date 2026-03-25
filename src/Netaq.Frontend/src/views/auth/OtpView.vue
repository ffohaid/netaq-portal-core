<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '../../stores/auth'

const { t } = useI18n()
const router = useRouter()
const authStore = useAuthStore()

const otpCode = ref('')

const resendCooldown = ref(0)
let cooldownTimer: any = null

async function handleVerify() {
  const success = await authStore.verifyOtp(otpCode.value)
  if (success) {
    router.push({ name: 'Dashboard' })
  }
}

async function handleResendOtp() {
  if (resendCooldown.value > 0) return
  await authStore.resendOtp()
  resendCooldown.value = 60
  cooldownTimer = setInterval(() => {
    resendCooldown.value--
    if (resendCooldown.value <= 0) clearInterval(cooldownTimer)
  }, 1000)
}
</script>

<template>
  <div>
    <h2 class="text-xl font-bold text-gray-900 mb-2">{{ t('auth.otpTitle') }}</h2>
    <p class="text-sm text-gray-500 mb-6">{{ t('auth.otpSubtitle') }}</p>

    <form @submit.prevent="handleVerify" class="space-y-5">
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1.5">{{ t('auth.otpCode') }}</label>
        <input
          v-model="otpCode"
          type="text"
          required
          maxlength="6"
          class="input-field text-center text-2xl tracking-widest"
          placeholder="000000"
          dir="ltr"
        />
      </div>

      <div v-if="authStore.error" class="p-3 bg-danger-50 border border-danger-500/20 rounded-lg">
        <p class="text-sm text-danger-700">{{ authStore.error }}</p>
      </div>

      <button
        type="submit"
        :disabled="authStore.isLoading || otpCode.length < 6"
        class="btn-primary w-full"
      >
        {{ authStore.isLoading ? t('common.loading') : t('auth.verifyOtp') }}
      </button>

      <button type="button" @click="handleResendOtp" :disabled="resendCooldown > 0" class="w-full text-sm text-primary-500 hover:text-primary-600 disabled:text-gray-400">
        {{ resendCooldown > 0 ? `${t('auth.resendOtp')} (${resendCooldown}s)` : t('auth.resendOtp') }}
      </button>
    </form>
  </div>
</template>
