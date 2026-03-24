import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '../services/api'
import type { UserProfile, LoginResponse, ApiResponse, TokenResponse } from '../types'
import { setLocale } from '../i18n'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<UserProfile | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const requiresOtp = ref(false)
  const otpEmail = ref('')

  const isAuthenticated = computed(() => !!localStorage.getItem('netaq_access_token'))
  const userDisplayName = computed(() => {
    if (!user.value) return ''
    const locale = localStorage.getItem('netaq_locale') || 'ar'
    return locale === 'ar' ? user.value.fullNameAr : user.value.fullNameEn
  })
  const organizationName = computed(() => {
    if (!user.value) return ''
    const locale = localStorage.getItem('netaq_locale') || 'ar'
    return locale === 'ar' ? user.value.organizationNameAr : user.value.organizationNameEn
  })

  async function login(email: string, password: string) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.post<ApiResponse<LoginResponse>>('/auth/login', { email, password })
      if (response.data.isSuccess && response.data.data) {
        if (response.data.data.requiresOtp) {
          requiresOtp.value = true
          otpEmail.value = email
          return { requiresOtp: true }
        }
        if (response.data.data.accessToken) {
          localStorage.setItem('netaq_access_token', response.data.data.accessToken)
          localStorage.setItem('netaq_refresh_token', response.data.data.refreshToken!)
          await fetchProfile()
          return { requiresOtp: false }
        }
      } else {
        error.value = response.data.error || 'Login failed'
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'An error occurred'
    } finally {
      isLoading.value = false
    }
    return null
  }

  async function verifyOtp(otpCode: string) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.post<ApiResponse<TokenResponse>>('/auth/verify-otp', {
        email: otpEmail.value,
        otpCode,
      })
      if (response.data.isSuccess && response.data.data) {
        localStorage.setItem('netaq_access_token', response.data.data.accessToken)
        localStorage.setItem('netaq_refresh_token', response.data.data.refreshToken)
        requiresOtp.value = false
        await fetchProfile()
        return true
      } else {
        error.value = response.data.error || 'OTP verification failed'
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'An error occurred'
    } finally {
      isLoading.value = false
    }
    return false
  }

  async function fetchProfile() {
    try {
      const response = await api.get<ApiResponse<UserProfile>>('/auth/me')
      if (response.data.isSuccess && response.data.data) {
        user.value = response.data.data
        setLocale(user.value.locale as 'ar' | 'en')
      }
    } catch {
      // Token might be invalid
      logout()
    }
  }

  async function logout() {
    try {
      await api.post('/auth/logout')
    } catch {
      // Ignore errors during logout
    }
    localStorage.removeItem('netaq_access_token')
    localStorage.removeItem('netaq_refresh_token')
    user.value = null
    requiresOtp.value = false
  }

  return {
    user,
    isLoading,
    error,
    requiresOtp,
    otpEmail,
    isAuthenticated,
    userDisplayName,
    organizationName,
    login,
    verifyOtp,
    fetchProfile,
    logout,
  }
})
