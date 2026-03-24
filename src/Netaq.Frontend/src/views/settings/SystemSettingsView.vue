<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSettingsStore } from '../../stores/settings'
import { getCurrentLocale } from '../../i18n'

const { t } = useI18n()
const settingsStore = useSettingsStore()
const locale = computed(() => getCurrentLocale())

const editingId = ref<string | null>(null)
const editingValue = ref('')
const saving = ref(false)
const successMessage = ref('')

onMounted(async () => {
  await settingsStore.fetchSystemSettings()
})

function getLabel(setting: any) {
  return locale.value === 'ar' ? (setting.labelAr || setting.settingKey) : (setting.labelEn || setting.settingKey)
}

function startEdit(setting: any) {
  editingId.value = setting.id
  editingValue.value = setting.settingValue
}

function cancelEdit() {
  editingId.value = null
  editingValue.value = ''
}

async function saveEdit(id: string) {
  saving.value = true
  const ok = await settingsStore.updateSystemSetting(id, editingValue.value)
  if (ok) {
    successMessage.value = t('settings.savedSuccessfully')
    setTimeout(() => { successMessage.value = '' }, 3000)
    await settingsStore.fetchSystemSettings()
  }
  editingId.value = null
  saving.value = false
}

function getCategoryIcon(category: string) {
  switch (category) {
    case 'General': return 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.066 2.573c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.573 1.066c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.066-2.573c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z'
    case 'Security': return 'M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z'
    case 'Notification': return 'M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9'
    case 'Email': return 'M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z'
    default: return 'M4 6h16M4 10h16M4 14h16M4 18h16'
  }
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div>
      <h1 class="text-2xl font-bold text-gray-900">{{ t('settings.systemSettings') }}</h1>
      <p class="text-gray-500 mt-1">{{ t('settings.systemSettingsDesc') }}</p>
    </div>

    <!-- Success Message -->
    <div v-if="successMessage" class="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-lg">
      {{ successMessage }}
    </div>

    <!-- Error -->
    <div v-if="settingsStore.error" class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg">
      {{ settingsStore.error }}
    </div>

    <!-- Loading -->
    <div v-if="settingsStore.isLoading" class="flex items-center justify-center py-12">
      <svg class="animate-spin w-8 h-8 text-primary-500" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
      </svg>
    </div>

    <!-- Settings by Category -->
    <div v-else class="space-y-6">
      <div
        v-for="(settings, category) in settingsStore.systemSettings"
        :key="category"
        class="card"
      >
        <div class="flex items-center gap-3 mb-4">
          <div class="p-2 bg-gray-100 rounded-lg">
            <svg class="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="getCategoryIcon(String(category))" />
            </svg>
          </div>
          <h2 class="text-lg font-semibold text-gray-900">{{ category }}</h2>
        </div>

        <div class="divide-y divide-gray-100">
          <div
            v-for="setting in settings"
            :key="setting.id"
            class="py-3 flex items-center justify-between"
          >
            <div class="flex-1">
              <p class="text-sm font-medium text-gray-900">{{ getLabel(setting) }}</p>
              <p class="text-xs text-gray-400">{{ setting.settingKey }}</p>
            </div>

            <div class="flex items-center gap-3">
              <template v-if="editingId === setting.id">
                <template v-if="setting.dataType === 'Boolean'">
                  <select v-model="editingValue" class="input-field w-24 text-sm">
                    <option value="true">{{ t('common.yes') }}</option>
                    <option value="false">{{ t('common.no') }}</option>
                  </select>
                </template>
                <template v-else-if="setting.dataType === 'Integer'">
                  <input v-model="editingValue" type="number" class="input-field w-32 text-sm" />
                </template>
                <template v-else>
                  <input v-model="editingValue" type="text" class="input-field w-48 text-sm" />
                </template>
                <button @click="saveEdit(setting.id)" :disabled="saving" class="text-green-600 hover:text-green-700">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                </button>
                <button @click="cancelEdit" class="text-gray-400 hover:text-gray-600">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </template>
              <template v-else>
                <span class="text-sm text-gray-600 font-mono bg-gray-50 px-2 py-1 rounded">{{ setting.settingValue }}</span>
                <button
                  v-if="setting.isEditable"
                  @click="startEdit(setting)"
                  class="text-gray-400 hover:text-primary-600"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                  </svg>
                </button>
              </template>
            </div>
          </div>
        </div>
      </div>

      <!-- Empty State -->
      <div v-if="Object.keys(settingsStore.systemSettings).length === 0" class="text-center py-12 text-gray-400">
        <p class="text-lg font-medium">{{ t('common.noData') }}</p>
      </div>
    </div>
  </div>
</template>
