<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import api from '../../services/api'

const { t, locale } = useI18n({ useScope: 'global' })
const route = useRoute()
const router = useRouter()
const isRtl = computed(() => locale.value === 'ar')

const token = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const showPassword = ref(false)
const showConfirmPassword = ref(false)
const isLoading = ref(false)
const isValidating = ref(true)
const isTokenValid = ref(false)
const isSuccess = ref(false)
const error = ref<string | null>(null)

onMounted(async () => {
  token.value = (route.query.token as string) || ''
  if (!token.value) {
    isValidating.value = false
    return
  }
  try {
    const res = await api.get(`/api/auth/validate-reset-token?token=${encodeURIComponent(token.value)}`)
    isTokenValid.value = res.data?.isSuccess === true
  } catch {
    isTokenValid.value = false
  } finally {
    isValidating.value = false
  }
})

async function handleReset() {
  error.value = null
  if (newPassword.value.length < 8) {
    error.value = isRtl.value ? 'كلمة المرور يجب أن تكون 8 أحرف على الأقل' : 'Password must be at least 8 characters'
    return
  }
  if (newPassword.value !== confirmPassword.value) {
    error.value = isRtl.value ? 'كلمتا المرور غير متطابقتين' : 'Passwords do not match'
    return
  }
  isLoading.value = true
  try {
    await api.post('/api/auth/reset-password', {
      token: token.value,
      newPassword: newPassword.value,
      confirmNewPassword: confirmPassword.value,
    })
    isSuccess.value = true
  } catch (err: any) {
    error.value = err?.response?.data?.message || (isRtl.value ? 'حدث خطأ، يرجى المحاولة لاحقاً' : 'An error occurred')
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div>
    <!-- Loading State -->
    <div v-if="isValidating" class="text-center py-8">
      <svg class="animate-spin h-10 w-10 text-blue-600 mx-auto mb-4" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" fill="none" />
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
      </svg>
      <p class="text-gray-500">{{ isRtl ? 'جارٍ التحقق من الرابط...' : 'Validating link...' }}</p>
    </div>

    <!-- Invalid Token -->
    <div v-else-if="!isTokenValid && !isSuccess" class="text-center">
      <div class="inline-flex items-center justify-center w-16 h-16 bg-red-100 rounded-full mb-6">
        <svg class="w-8 h-8 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
        </svg>
      </div>
      <h2 class="text-xl font-bold text-gray-900 mb-3">
        {{ isRtl ? 'رابط غير صالح' : 'Invalid Link' }}
      </h2>
      <p class="text-gray-500 text-sm mb-6">
        {{ isRtl ? 'هذا الرابط منتهي الصلاحية أو غير صالح. يرجى طلب رابط جديد.' : 'This link has expired or is invalid. Please request a new one.' }}
      </p>
      <router-link
        :to="{ name: 'ForgotPassword' }"
        class="inline-flex items-center gap-2 px-6 py-3 bg-blue-600 text-white rounded-xl hover:bg-blue-700 transition-colors font-medium text-sm"
      >
        {{ isRtl ? 'طلب رابط جديد' : 'Request New Link' }}
      </router-link>
    </div>

    <!-- Success State -->
    <div v-else-if="isSuccess" class="text-center">
      <div class="inline-flex items-center justify-center w-16 h-16 bg-green-100 rounded-full mb-6">
        <svg class="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
        </svg>
      </div>
      <h2 class="text-xl font-bold text-gray-900 mb-3">
        {{ isRtl ? 'تم إعادة تعيين كلمة المرور' : 'Password Reset Successful' }}
      </h2>
      <p class="text-gray-500 text-sm mb-6">
        {{ isRtl ? 'يمكنك الآن تسجيل الدخول بكلمة المرور الجديدة' : 'You can now log in with your new password' }}
      </p>
      <router-link
        :to="{ name: 'Login' }"
        class="inline-flex items-center gap-2 px-6 py-3 bg-gradient-to-r from-blue-600 to-indigo-600 text-white rounded-xl hover:from-blue-700 hover:to-indigo-700 transition-all font-medium text-sm shadow-lg shadow-blue-500/25"
      >
        {{ t('auth.login') }}
      </router-link>
    </div>

    <!-- Reset Form -->
    <div v-else>
      <div class="text-center mb-8">
        <div class="inline-flex items-center justify-center w-14 h-14 bg-blue-100 rounded-full mb-4">
          <svg class="w-7 h-7 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"/>
          </svg>
        </div>
        <h2 class="text-2xl font-bold text-gray-900">
          {{ isRtl ? 'إعادة تعيين كلمة المرور' : 'Reset Password' }}
        </h2>
        <p class="text-gray-500 mt-2 text-sm">
          {{ isRtl ? 'أدخل كلمة المرور الجديدة' : 'Enter your new password' }}
        </p>
      </div>

      <div v-if="error" class="mb-6 p-4 bg-red-50 border border-red-200 rounded-xl flex items-start gap-3">
        <svg class="w-5 h-5 text-red-500 mt-0.5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
        </svg>
        <p class="text-red-700 text-sm">{{ error }}</p>
      </div>

      <form @submit.prevent="handleReset" class="space-y-5">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">
            {{ isRtl ? 'كلمة المرور الجديدة' : 'New Password' }}
          </label>
          <div class="relative">
            <div class="absolute inset-y-0 start-0 flex items-center ps-4 pointer-events-none">
              <svg class="w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"/>
              </svg>
            </div>
            <input
              v-model="newPassword"
              :type="showPassword ? 'text' : 'password'"
              dir="ltr"
              required
              minlength="8"
              :placeholder="isRtl ? '8 أحرف على الأقل' : 'At least 8 characters'"
              class="w-full ps-12 pe-12 py-3.5 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all duration-200 bg-gray-50 focus:bg-white text-sm placeholder:text-gray-400"
            />
            <button type="button" @click="showPassword = !showPassword" class="absolute inset-y-0 end-0 flex items-center pe-4 text-gray-400 hover:text-gray-600">
              <svg v-if="!showPassword" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/></svg>
              <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21"/></svg>
            </button>
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">
            {{ isRtl ? 'تأكيد كلمة المرور' : 'Confirm Password' }}
          </label>
          <div class="relative">
            <div class="absolute inset-y-0 start-0 flex items-center ps-4 pointer-events-none">
              <svg class="w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
              </svg>
            </div>
            <input
              v-model="confirmPassword"
              :type="showConfirmPassword ? 'text' : 'password'"
              dir="ltr"
              required
              minlength="8"
              :placeholder="isRtl ? 'أعد إدخال كلمة المرور' : 'Re-enter your password'"
              class="w-full ps-12 pe-12 py-3.5 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all duration-200 bg-gray-50 focus:bg-white text-sm placeholder:text-gray-400"
            />
            <button type="button" @click="showConfirmPassword = !showConfirmPassword" class="absolute inset-y-0 end-0 flex items-center pe-4 text-gray-400 hover:text-gray-600">
              <svg v-if="!showConfirmPassword" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/></svg>
              <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21"/></svg>
            </button>
          </div>
        </div>

        <button
          type="submit"
          :disabled="isLoading"
          class="w-full py-3.5 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-700 hover:to-indigo-700 text-white font-semibold rounded-xl transition-all duration-200 shadow-lg shadow-blue-500/25 hover:shadow-blue-500/40 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
        >
          <svg v-if="isLoading" class="animate-spin h-5 w-5" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" fill="none" />
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
          </svg>
          {{ isLoading ? t('common.loading') : (isRtl ? 'إعادة تعيين كلمة المرور' : 'Reset Password') }}
        </button>
      </form>
    </div>
  </div>
</template>
