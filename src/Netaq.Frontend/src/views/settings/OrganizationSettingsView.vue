<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSettingsStore } from '../../stores/settings'
import { getCurrentLocale } from '../../i18n'

const { t } = useI18n()
const settingsStore = useSettingsStore()
const locale = computed(() => getCurrentLocale())

const activeTab = ref<'branding' | 'auth'>('branding')
const saving = ref(false)
const successMessage = ref('')

// Branding form
const brandingForm = ref({
  nameAr: '',
  nameEn: '',
  descriptionAr: '',
  descriptionEn: '',
  address: '',
  phone: '',
  email: '',
  website: '',
  showPlatformLogo: true,
})

// Auth form
const authForm = ref({
  activeAuthProvider: 'CustomAuth',
  isOtpEnabled: true,
  ssoEndpoint: '',
  ssoClientId: '',
  adDomain: '',
  adLdapUrl: '',
})

onMounted(async () => {
  await settingsStore.fetchOrgSettings()
  if (settingsStore.orgSettings) {
    const s = settingsStore.orgSettings
    brandingForm.value = {
      nameAr: s.nameAr || '',
      nameEn: s.nameEn || '',
      descriptionAr: s.descriptionAr || '',
      descriptionEn: s.descriptionEn || '',
      address: s.address || '',
      phone: s.phone || '',
      email: s.email || '',
      website: s.website || '',
      showPlatformLogo: s.showPlatformLogo,
    }
    authForm.value = {
      activeAuthProvider: s.activeAuthProvider || 'CustomAuth',
      isOtpEnabled: s.isOtpEnabled,
      ssoEndpoint: s.ssoEndpoint || '',
      ssoClientId: s.ssoClientId || '',
      adDomain: s.adDomain || '',
      adLdapUrl: s.adLdapUrl || '',
    }
  }
})

async function saveBranding() {
  saving.value = true
  successMessage.value = ''
  const ok = await settingsStore.updateOrgBranding(brandingForm.value)
  if (ok) {
    successMessage.value = t('settings.savedSuccessfully')
    setTimeout(() => { successMessage.value = '' }, 3000)
  }
  saving.value = false
}

async function saveAuth() {
  saving.value = true
  successMessage.value = ''
  const ok = await settingsStore.updateAuthSettings(authForm.value)
  if (ok) {
    successMessage.value = t('settings.savedSuccessfully')
    setTimeout(() => { successMessage.value = '' }, 3000)
  }
  saving.value = false
}

const authProviders = [
  { value: 'Nafath', label: 'settings.authProviders.nafath' },
  { value: 'ActiveDirectory', label: 'settings.authProviders.activeDirectory' },
  { value: 'CustomAuth', label: 'settings.authProviders.customAuth' },
]
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div>
      <h1 class="text-2xl font-bold text-gray-900">{{ t('settings.orgSettings') }}</h1>
      <p class="text-gray-500 mt-1">{{ t('settings.orgSettingsDesc') }}</p>
    </div>

    <!-- Success Message -->
    <div v-if="successMessage" class="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-lg">
      {{ successMessage }}
    </div>

    <!-- Error Message -->
    <div v-if="settingsStore.error" class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg">
      {{ settingsStore.error }}
    </div>

    <!-- Tabs -->
    <div class="border-b border-gray-200">
      <nav class="flex gap-6">
        <button
          @click="activeTab = 'branding'"
          class="pb-3 text-sm font-medium border-b-2 transition-colors"
          :class="activeTab === 'branding' ? 'border-primary-500 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'"
        >
          {{ t('settings.branding') }}
        </button>
        <button
          @click="activeTab = 'auth'"
          class="pb-3 text-sm font-medium border-b-2 transition-colors"
          :class="activeTab === 'auth' ? 'border-primary-500 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'"
        >
          {{ t('settings.authentication') }}
        </button>
      </nav>
    </div>

    <!-- Loading -->
    <div v-if="settingsStore.isLoading" class="flex items-center justify-center py-12">
      <svg class="animate-spin w-8 h-8 text-primary-500" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
      </svg>
    </div>

    <!-- Branding Tab -->
    <div v-else-if="activeTab === 'branding'" class="card space-y-6">
      <h2 class="text-lg font-semibold text-gray-900">{{ t('settings.brandingInfo') }}</h2>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.orgNameAr') }}</label>
          <input v-model="brandingForm.nameAr" type="text" class="input-field" dir="rtl" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.orgNameEn') }}</label>
          <input v-model="brandingForm.nameEn" type="text" class="input-field" dir="ltr" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.descriptionAr') }}</label>
          <textarea v-model="brandingForm.descriptionAr" rows="3" class="input-field" dir="rtl"></textarea>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.descriptionEn') }}</label>
          <textarea v-model="brandingForm.descriptionEn" rows="3" class="input-field" dir="ltr"></textarea>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.address') }}</label>
          <input v-model="brandingForm.address" type="text" class="input-field" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.phone') }}</label>
          <input v-model="brandingForm.phone" type="tel" class="input-field" dir="ltr" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.email') }}</label>
          <input v-model="brandingForm.email" type="email" class="input-field" dir="ltr" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.website') }}</label>
          <input v-model="brandingForm.website" type="url" class="input-field" dir="ltr" />
        </div>
      </div>

      <div class="flex items-center gap-3">
        <input id="showPlatformLogo" v-model="brandingForm.showPlatformLogo" type="checkbox" class="h-4 w-4 text-primary-600 rounded border-gray-300" />
        <label for="showPlatformLogo" class="text-sm text-gray-700">{{ t('settings.showPlatformLogo') }}</label>
      </div>

      <div class="flex justify-end">
        <button @click="saveBranding" :disabled="saving" class="btn-primary">
          <span v-if="saving">{{ t('common.loading') }}</span>
          <span v-else>{{ t('common.save') }}</span>
        </button>
      </div>
    </div>

    <!-- Auth Tab -->
    <div v-else-if="activeTab === 'auth'" class="card space-y-6">
      <h2 class="text-lg font-semibold text-gray-900">{{ t('settings.authSettings') }}</h2>

      <div class="space-y-6">
        <!-- Auth Provider -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">{{ t('settings.authProvider') }}</label>
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <label
              v-for="provider in authProviders"
              :key="provider.value"
              class="flex items-center gap-3 p-4 border rounded-lg cursor-pointer transition-colors"
              :class="authForm.activeAuthProvider === provider.value ? 'border-primary-500 bg-primary-50' : 'border-gray-200 hover:border-gray-300'"
            >
              <input
                v-model="authForm.activeAuthProvider"
                :value="provider.value"
                type="radio"
                class="h-4 w-4 text-primary-600"
              />
              <span class="text-sm font-medium">{{ t(provider.label) }}</span>
            </label>
          </div>
        </div>

        <!-- OTP Toggle -->
        <div class="flex items-center gap-3 p-4 bg-gray-50 rounded-lg">
          <input id="otpEnabled" v-model="authForm.isOtpEnabled" type="checkbox" class="h-4 w-4 text-primary-600 rounded border-gray-300" />
          <div>
            <label for="otpEnabled" class="text-sm font-medium text-gray-900">{{ t('settings.enableOtp') }}</label>
            <p class="text-xs text-gray-500">{{ t('settings.otpDescription') }}</p>
          </div>
        </div>

        <!-- Nafath SSO Settings -->
        <div v-if="authForm.activeAuthProvider === 'Nafath'" class="space-y-4 p-4 bg-blue-50 rounded-lg">
          <h3 class="text-sm font-semibold text-blue-800">{{ t('settings.nafathSettings') }}</h3>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.ssoEndpoint') }}</label>
              <input v-model="authForm.ssoEndpoint" type="url" class="input-field" dir="ltr" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.ssoClientId') }}</label>
              <input v-model="authForm.ssoClientId" type="text" class="input-field" dir="ltr" />
            </div>
          </div>
        </div>

        <!-- Active Directory Settings -->
        <div v-if="authForm.activeAuthProvider === 'ActiveDirectory'" class="space-y-4 p-4 bg-purple-50 rounded-lg">
          <h3 class="text-sm font-semibold text-purple-800">{{ t('settings.adSettings') }}</h3>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.adDomain') }}</label>
              <input v-model="authForm.adDomain" type="text" class="input-field" dir="ltr" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.adLdapUrl') }}</label>
              <input v-model="authForm.adLdapUrl" type="url" class="input-field" dir="ltr" />
            </div>
          </div>
        </div>
      </div>

      <div class="flex justify-end">
        <button @click="saveAuth" :disabled="saving" class="btn-primary">
          <span v-if="saving">{{ t('common.loading') }}</span>
          <span v-else>{{ t('common.save') }}</span>
        </button>
      </div>
    </div>
  </div>
</template>
