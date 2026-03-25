<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '../../stores/auth'
import api from '../../services/api'

const { t, locale } = useI18n({ useScope: 'global' })
const authStore = useAuthStore()
const isRtl = computed(() => locale.value === 'ar')

const activeTab = ref<'profile' | 'password'>('profile')
const isLoading = ref(false)
const isSaving = ref(false)
const successMessage = ref<string | null>(null)
const errorMessage = ref<string | null>(null)

// Profile form
const profile = ref({
  fullNameAr: '',
  fullNameEn: '',
  phone: '',
  jobTitleAr: '',
  jobTitleEn: '',
  departmentAr: '',
  departmentEn: '',
  locale: 'ar',
})
const profileEmail = ref('')
const profileRole = ref('')
const profileOrgAr = ref('')
const profileOrgEn = ref('')

// Password form
const currentPassword = ref('')
const newPassword = ref('')
const confirmNewPassword = ref('')
const showCurrentPassword = ref(false)
const showNewPassword = ref(false)
const showConfirmPassword = ref(false)

onMounted(async () => {
  await loadProfile()
})

async function loadProfile() {
  isLoading.value = true
  try {
    const res = await api.get('/api/auth/me')
    const data = res.data?.data
    if (data) {
      profile.value = {
        fullNameAr: data.fullNameAr || '',
        fullNameEn: data.fullNameEn || '',
        phone: data.phone || '',
        jobTitleAr: data.jobTitleAr || '',
        jobTitleEn: data.jobTitleEn || '',
        departmentAr: data.departmentAr || '',
        departmentEn: data.departmentEn || '',
        locale: data.locale || 'ar',
      }
      profileEmail.value = data.email
      profileRole.value = data.role
      profileOrgAr.value = data.organizationNameAr
      profileOrgEn.value = data.organizationNameEn
    }
  } catch {
    errorMessage.value = isRtl.value ? 'فشل تحميل البيانات' : 'Failed to load profile'
  } finally {
    isLoading.value = false
  }
}

async function saveProfile() {
  isSaving.value = true
  successMessage.value = null
  errorMessage.value = null
  try {
    await api.put('/api/auth/profile', profile.value)
    successMessage.value = isRtl.value ? 'تم تحديث البيانات بنجاح' : 'Profile updated successfully'
    await authStore.fetchProfile()
  } catch (err: any) {
    errorMessage.value = err?.response?.data?.message || (isRtl.value ? 'فشل تحديث البيانات' : 'Failed to update profile')
  } finally {
    isSaving.value = false
  }
}

async function changePassword() {
  isSaving.value = true
  successMessage.value = null
  errorMessage.value = null
  
  if (newPassword.value.length < 8) {
    errorMessage.value = isRtl.value ? 'كلمة المرور يجب أن تكون 8 أحرف على الأقل' : 'Password must be at least 8 characters'
    isSaving.value = false
    return
  }
  if (newPassword.value !== confirmNewPassword.value) {
    errorMessage.value = isRtl.value ? 'كلمتا المرور غير متطابقتين' : 'Passwords do not match'
    isSaving.value = false
    return
  }

  try {
    await api.post('/api/auth/change-password', {
      currentPassword: currentPassword.value,
      newPassword: newPassword.value,
      confirmNewPassword: confirmNewPassword.value,
    })
    successMessage.value = isRtl.value ? 'تم تغيير كلمة المرور بنجاح' : 'Password changed successfully'
    currentPassword.value = ''
    newPassword.value = ''
    confirmNewPassword.value = ''
  } catch (err: any) {
    errorMessage.value = err?.response?.data?.message || (isRtl.value ? 'فشل تغيير كلمة المرور' : 'Failed to change password')
  } finally {
    isSaving.value = false
  }
}

const roleLabel = computed(() => {
  const roles: Record<string, { ar: string; en: string }> = {
    SystemAdmin: { ar: 'مدير النظام', en: 'System Admin' },
    OrganizationAdmin: { ar: 'مدير الجهة', en: 'Organization Admin' },
    DepartmentManager: { ar: 'مدير الإدارة', en: 'Department Manager' },
    CommitteeChair: { ar: 'رئيس اللجنة', en: 'Committee Chair' },
    CommitteeMember: { ar: 'عضو لجنة', en: 'Committee Member' },
    Auditor: { ar: 'مراجع', en: 'Auditor' },
    Viewer: { ar: 'مشاهد', en: 'Viewer' },
  }
  const r = roles[profileRole.value]
  return r ? (isRtl.value ? r.ar : r.en) : profileRole.value
})
</script>

<template>
  <div class="max-w-4xl mx-auto">
    <!-- Header -->
    <div class="mb-8">
      <h1 class="text-2xl font-bold text-gray-900">{{ isRtl ? 'الملف الشخصي' : 'My Profile' }}</h1>
      <p class="text-gray-500 mt-1">{{ isRtl ? 'إدارة بياناتك الشخصية وكلمة المرور' : 'Manage your personal information and password' }}</p>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="flex justify-center py-20">
      <svg class="animate-spin h-10 w-10 text-blue-600" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" fill="none" />
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
      </svg>
    </div>

    <div v-else>
      <!-- Profile Card Header -->
      <div class="bg-gradient-to-r from-blue-600 to-indigo-700 rounded-t-2xl p-8 text-white">
        <div class="flex items-center gap-6">
          <div class="w-20 h-20 bg-white/20 backdrop-blur-sm rounded-2xl flex items-center justify-center text-3xl font-bold border-2 border-white/30">
            {{ (profile.fullNameAr || profile.fullNameEn || 'U').charAt(0) }}
          </div>
          <div>
            <h2 class="text-2xl font-bold">{{ isRtl ? profile.fullNameAr : profile.fullNameEn }}</h2>
            <p class="text-blue-200 mt-1">{{ profileEmail }}</p>
            <span class="inline-block mt-2 px-3 py-1 bg-white/20 rounded-full text-sm">{{ roleLabel }}</span>
          </div>
        </div>
      </div>

      <!-- Tabs -->
      <div class="bg-white border-x border-gray-200">
        <div class="flex border-b border-gray-200">
          <button
            @click="activeTab = 'profile'; successMessage = null; errorMessage = null"
            :class="[
              'flex-1 py-4 text-sm font-medium text-center transition-colors border-b-2',
              activeTab === 'profile' ? 'border-blue-600 text-blue-600' : 'border-transparent text-gray-500 hover:text-gray-700'
            ]"
          >
            <div class="flex items-center justify-center gap-2">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/></svg>
              {{ isRtl ? 'البيانات الشخصية' : 'Personal Info' }}
            </div>
          </button>
          <button
            @click="activeTab = 'password'; successMessage = null; errorMessage = null"
            :class="[
              'flex-1 py-4 text-sm font-medium text-center transition-colors border-b-2',
              activeTab === 'password' ? 'border-blue-600 text-blue-600' : 'border-transparent text-gray-500 hover:text-gray-700'
            ]"
          >
            <div class="flex items-center justify-center gap-2">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"/></svg>
              {{ isRtl ? 'تغيير كلمة المرور' : 'Change Password' }}
            </div>
          </button>
        </div>
      </div>

      <!-- Messages -->
      <div v-if="successMessage" class="mx-0 mt-0 p-4 bg-green-50 border-x border-green-200">
        <div class="flex items-center gap-3">
          <svg class="w-5 h-5 text-green-600 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/></svg>
          <p class="text-green-700 text-sm">{{ successMessage }}</p>
        </div>
      </div>
      <div v-if="errorMessage" class="mx-0 mt-0 p-4 bg-red-50 border-x border-red-200">
        <div class="flex items-center gap-3">
          <svg class="w-5 h-5 text-red-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
          <p class="text-red-700 text-sm">{{ errorMessage }}</p>
        </div>
      </div>

      <!-- Profile Tab -->
      <div v-if="activeTab === 'profile'" class="bg-white rounded-b-2xl border border-t-0 border-gray-200 p-8">
        <form @submit.prevent="saveProfile" class="space-y-6">
          <!-- Read-only info -->
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6 pb-6 border-b border-gray-100">
            <div>
              <label class="block text-xs font-medium text-gray-400 mb-1">{{ isRtl ? 'البريد الإلكتروني' : 'Email' }}</label>
              <p class="text-gray-700 font-medium" dir="ltr">{{ profileEmail }}</p>
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-400 mb-1">{{ isRtl ? 'الجهة' : 'Organization' }}</label>
              <p class="text-gray-700 font-medium">{{ isRtl ? profileOrgAr : profileOrgEn }}</p>
            </div>
          </div>

          <!-- Editable fields -->
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">{{ isRtl ? 'الاسم بالعربية' : 'Name (Arabic)' }}</label>
              <input v-model="profile.fullNameAr" type="text" dir="rtl" class="w-full px-4 py-3 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 bg-gray-50 focus:bg-white text-sm" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">{{ isRtl ? 'الاسم بالإنجليزية' : 'Name (English)' }}</label>
              <input v-model="profile.fullNameEn" type="text" dir="ltr" class="w-full px-4 py-3 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 bg-gray-50 focus:bg-white text-sm" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">{{ isRtl ? 'رقم الهاتف' : 'Phone' }}</label>
              <input v-model="profile.phone" type="tel" dir="ltr" class="w-full px-4 py-3 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 bg-gray-50 focus:bg-white text-sm" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">{{ isRtl ? 'اللغة المفضلة' : 'Preferred Language' }}</label>
              <select v-model="profile.locale" class="w-full px-4 py-3 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 bg-gray-50 focus:bg-white text-sm">
                <option value="ar">العربية</option>
                <option value="en">English</option>
              </select>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">{{ isRtl ? 'المسمى الوظيفي (عربي)' : 'Job Title (Arabic)' }}</label>
              <input v-model="profile.jobTitleAr" type="text" dir="rtl" class="w-full px-4 py-3 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 bg-gray-50 focus:bg-white text-sm" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">{{ isRtl ? 'المسمى الوظيفي (إنجليزي)' : 'Job Title (English)' }}</label>
              <input v-model="profile.jobTitleEn" type="text" dir="ltr" class="w-full px-4 py-3 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 bg-gray-50 focus:bg-white text-sm" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">{{ isRtl ? 'الإدارة (عربي)' : 'Department (Arabic)' }}</label>
              <input v-model="profile.departmentAr" type="text" dir="rtl" class="w-full px-4 py-3 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 bg-gray-50 focus:bg-white text-sm" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">{{ isRtl ? 'الإدارة (إنجليزي)' : 'Department (English)' }}</label>
              <input v-model="profile.departmentEn" type="text" dir="ltr" class="w-full px-4 py-3 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 bg-gray-50 focus:bg-white text-sm" />
            </div>
          </div>

          <div class="flex justify-end pt-4">
            <button
              type="submit"
              :disabled="isSaving"
              class="px-8 py-3 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-700 hover:to-indigo-700 text-white font-semibold rounded-xl transition-all shadow-lg shadow-blue-500/25 disabled:opacity-50 flex items-center gap-2"
            >
              <svg v-if="isSaving" class="animate-spin h-5 w-5" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" fill="none" /><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" /></svg>
              {{ isSaving ? t('common.loading') : (isRtl ? 'حفظ التغييرات' : 'Save Changes') }}
            </button>
          </div>
        </form>
      </div>

      <!-- Password Tab -->
      <div v-if="activeTab === 'password'" class="bg-white rounded-b-2xl border border-t-0 border-gray-200 p-8">
        <form @submit.prevent="changePassword" class="space-y-6 max-w-lg">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">{{ isRtl ? 'كلمة المرور الحالية' : 'Current Password' }}</label>
            <div class="relative">
              <input
                v-model="currentPassword"
                :type="showCurrentPassword ? 'text' : 'password'"
                dir="ltr"
                required
                class="w-full px-4 pe-12 py-3 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 bg-gray-50 focus:bg-white text-sm"
              />
              <button type="button" @click="showCurrentPassword = !showCurrentPassword" class="absolute inset-y-0 end-0 flex items-center pe-4 text-gray-400 hover:text-gray-600">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/></svg>
              </button>
            </div>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">{{ isRtl ? 'كلمة المرور الجديدة' : 'New Password' }}</label>
            <div class="relative">
              <input
                v-model="newPassword"
                :type="showNewPassword ? 'text' : 'password'"
                dir="ltr"
                required
                minlength="8"
                :placeholder="isRtl ? '8 أحرف على الأقل' : 'At least 8 characters'"
                class="w-full px-4 pe-12 py-3 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 bg-gray-50 focus:bg-white text-sm"
              />
              <button type="button" @click="showNewPassword = !showNewPassword" class="absolute inset-y-0 end-0 flex items-center pe-4 text-gray-400 hover:text-gray-600">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/></svg>
              </button>
            </div>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">{{ isRtl ? 'تأكيد كلمة المرور الجديدة' : 'Confirm New Password' }}</label>
            <div class="relative">
              <input
                v-model="confirmNewPassword"
                :type="showConfirmPassword ? 'text' : 'password'"
                dir="ltr"
                required
                minlength="8"
                class="w-full px-4 pe-12 py-3 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 bg-gray-50 focus:bg-white text-sm"
              />
              <button type="button" @click="showConfirmPassword = !showConfirmPassword" class="absolute inset-y-0 end-0 flex items-center pe-4 text-gray-400 hover:text-gray-600">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/></svg>
              </button>
            </div>
          </div>

          <div class="pt-4">
            <button
              type="submit"
              :disabled="isSaving"
              class="px-8 py-3 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-700 hover:to-indigo-700 text-white font-semibold rounded-xl transition-all shadow-lg shadow-blue-500/25 disabled:opacity-50 flex items-center gap-2"
            >
              <svg v-if="isSaving" class="animate-spin h-5 w-5" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" fill="none" /><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" /></svg>
              {{ isSaving ? t('common.loading') : (isRtl ? 'تغيير كلمة المرور' : 'Change Password') }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
