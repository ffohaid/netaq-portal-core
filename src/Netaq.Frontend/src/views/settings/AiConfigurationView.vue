<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSettingsStore } from '../../stores/settings'
import { getCurrentLocale } from '../../i18n'
import type { AiConfiguration, AiTestResult } from '../../types/settings'

const { t } = useI18n()
const settingsStore = useSettingsStore()
const locale = computed(() => getCurrentLocale())

const showModal = ref(false)
const editingConfig = ref<AiConfiguration | null>(null)
const testResult = ref<AiTestResult | null>(null)
const testingId = ref<string | null>(null)

const form = ref({
  providerType: 'Gemini',
  providerName: '',
  isActive: true,
  endpoint: '',
  apiKey: '',
  modelName: '',
  temperature: 0.3,
  maxTokens: 4096,
  vectorDbEndpoint: '',
  embeddingModel: '',
  chunkSize: 512,
})

const providerTypes = [
  { value: 'Gemini', label: 'Google Gemini' },
  { value: 'Ollama', label: 'Ollama (Local)' },
  { value: 'OpenAI', label: 'OpenAI Compatible' },
]

onMounted(async () => {
  await settingsStore.fetchAiConfigs()
})

function openCreateModal() {
  editingConfig.value = null
  form.value = {
    providerType: 'Gemini',
    providerName: '',
    isActive: true,
    endpoint: '',
    apiKey: '',
    modelName: '',
    temperature: 0.3,
    maxTokens: 4096,
    vectorDbEndpoint: '',
    embeddingModel: '',
    chunkSize: 512,
  }
  showModal.value = true
}

function openEditModal(config: AiConfiguration) {
  editingConfig.value = config
  form.value = {
    providerType: config.providerType,
    providerName: config.providerName,
    isActive: config.isActive,
    endpoint: config.endpoint,
    apiKey: '',
    modelName: config.modelName,
    temperature: config.temperature,
    maxTokens: config.maxTokens,
    vectorDbEndpoint: config.vectorDbEndpoint || '',
    embeddingModel: config.embeddingModel || '',
    chunkSize: config.chunkSize,
  }
  showModal.value = true
}

async function saveConfig() {
  if (editingConfig.value) {
    const ok = await settingsStore.updateAiConfig(editingConfig.value.id, form.value)
    if (ok) showModal.value = false
  } else {
    const ok = await settingsStore.createAiConfig(form.value)
    if (ok) showModal.value = false
  }
}

async function deleteConfig(id: string) {
  if (confirm(t('settings.confirmDelete'))) {
    await settingsStore.deleteAiConfig(id)
  }
}

async function testConfig(id: string) {
  testingId.value = id
  testResult.value = null
  const result = await settingsStore.testAiConfig(id)
  testResult.value = result
  testingId.value = null
}

function getProviderIcon(type: string) {
  switch (type) {
    case 'Gemini': return 'M9.813 15.904L9 18.75l-.813-2.846a4.5 4.5 0 00-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 003.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 003.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 00-3.09 3.09zM18.259 8.715L18 9.75l-.259-1.035a3.375 3.375 0 00-2.455-2.456L14.25 6l1.036-.259a3.375 3.375 0 002.455-2.456L18 2.25l.259 1.035a3.375 3.375 0 002.455 2.456L21.75 6l-1.036.259a3.375 3.375 0 00-2.455 2.456zM16.894 20.567L16.5 21.75l-.394-1.183a2.25 2.25 0 00-1.423-1.423L13.5 18.75l1.183-.394a2.25 2.25 0 001.423-1.423l.394-1.183.394 1.183a2.25 2.25 0 001.423 1.423l1.183.394-1.183.394a2.25 2.25 0 00-1.423 1.423z'
    case 'Ollama': return 'M21 7.5l-2.25-1.313M21 7.5v2.25m0-2.25l-2.25 1.313M3 7.5l2.25-1.313M3 7.5l2.25 1.313M3 7.5v2.25m9 3l2.25-1.313M12 12.75l-2.25-1.313M12 12.75V15m0 6.75l2.25-1.313M12 21.75V19.5m0 2.25l-2.25-1.313m0-16.875L12 2.25l2.25 1.313M21 14.25v2.25l-2.25 1.313m-13.5 0L3 16.5v-2.25'
    default: return 'M9.75 3.104v5.714a2.25 2.25 0 01-.659 1.591L5 14.5M9.75 3.104c-.251.023-.501.05-.75.082m.75-.082a24.301 24.301 0 014.5 0m0 0v5.714c0 .597.237 1.17.659 1.591L19.8 15.3M14.25 3.104c.251.023.501.05.75.082M19.8 15.3l-1.57.393A9.065 9.065 0 0112 15a9.065 9.065 0 00-6.23.693L5 14.5m14.8.8l1.402 1.402c1.232 1.232.65 3.318-1.067 3.611A48.309 48.309 0 0112 21c-2.773 0-5.491-.235-8.135-.687-1.718-.293-2.3-2.379-1.067-3.61L5 14.5'
  }
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ t('settings.aiConfiguration') }}</h1>
        <p class="text-gray-500 mt-1">{{ t('settings.aiConfigDesc') }}</p>
      </div>
      <button @click="openCreateModal" class="btn-primary">
        <svg class="w-5 h-5 me-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('settings.addProvider') }}
      </button>
    </div>

    <!-- Error -->
    <div v-if="settingsStore.error" class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg">
      {{ settingsStore.error }}
    </div>

    <!-- Test Result -->
    <div v-if="testResult" class="px-4 py-3 rounded-lg border" :class="testResult.success ? 'bg-green-50 border-green-200 text-green-700' : 'bg-red-50 border-red-200 text-red-700'">
      <div class="flex items-center justify-between">
        <span>{{ testResult.message }}</span>
        <span v-if="testResult.responseTime" class="text-xs">{{ testResult.responseTime }}ms</span>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="settingsStore.isLoading" class="flex items-center justify-center py-12">
      <svg class="animate-spin w-8 h-8 text-primary-500" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
      </svg>
    </div>

    <!-- Config Cards -->
    <div v-else class="grid grid-cols-1 lg:grid-cols-2 gap-4">
      <div
        v-for="config in settingsStore.aiConfigs"
        :key="config.id"
        class="card border-s-4 transition-shadow hover:shadow-md"
        :class="config.isActive ? 'border-s-green-500' : 'border-s-gray-300'"
      >
        <div class="flex items-start justify-between mb-4">
          <div class="flex items-center gap-3">
            <div class="p-2 rounded-lg" :class="config.isActive ? 'bg-green-100' : 'bg-gray-100'">
              <svg class="w-6 h-6" :class="config.isActive ? 'text-green-600' : 'text-gray-400'" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" :d="getProviderIcon(config.providerType)" />
              </svg>
            </div>
            <div>
              <h3 class="font-semibold text-gray-900">{{ config.providerName }}</h3>
              <span class="text-xs px-2 py-0.5 rounded-full" :class="config.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'">
                {{ config.isActive ? t('settings.active') : t('settings.inactive') }}
              </span>
            </div>
          </div>
          <div class="flex gap-1">
            <button @click="testConfig(config.id)" class="p-1.5 text-gray-400 hover:text-blue-600 rounded" :disabled="testingId === config.id">
              <svg v-if="testingId === config.id" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
              <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5.636 18.364a9 9 0 010-12.728m12.728 0a9 9 0 010 12.728m-9.9-2.829a5 5 0 010-7.07m7.072 0a5 5 0 010 7.07M13 12a1 1 0 11-2 0 1 1 0 012 0z" />
              </svg>
            </button>
            <button @click="openEditModal(config)" class="p-1.5 text-gray-400 hover:text-primary-600 rounded">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
              </svg>
            </button>
            <button @click="deleteConfig(config.id)" class="p-1.5 text-gray-400 hover:text-red-600 rounded">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
              </svg>
            </button>
          </div>
        </div>

        <div class="space-y-2 text-sm">
          <div class="flex justify-between">
            <span class="text-gray-500">{{ t('settings.providerType') }}</span>
            <span class="font-medium">{{ config.providerType }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-gray-500">{{ t('settings.model') }}</span>
            <span class="font-medium">{{ config.modelName }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-gray-500">{{ t('settings.temperature') }}</span>
            <span class="font-medium">{{ config.temperature }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-gray-500">{{ t('settings.maxTokens') }}</span>
            <span class="font-medium">{{ new Intl.NumberFormat('en-US').format(config.maxTokens) }}</span>
          </div>
          <div v-if="config.vectorDbEndpoint" class="flex justify-between">
            <span class="text-gray-500">{{ t('settings.vectorDb') }}</span>
            <span class="font-medium text-xs truncate max-w-[200px]">{{ config.vectorDbEndpoint }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-gray-500">{{ t('settings.apiKeyStatus') }}</span>
            <span :class="config.hasApiKey ? 'text-green-600' : 'text-red-500'" class="font-medium">
              {{ config.hasApiKey ? t('settings.configured') : t('settings.notConfigured') }}
            </span>
          </div>
        </div>
      </div>

      <!-- Empty State -->
      <div v-if="settingsStore.aiConfigs.length === 0" class="col-span-2 text-center py-12 text-gray-400">
        <svg class="w-16 h-16 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9.813 15.904L9 18.75l-.813-2.846a4.5 4.5 0 00-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 003.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 003.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 00-3.09 3.09z" />
        </svg>
        <p class="text-lg font-medium">{{ t('settings.noAiProviders') }}</p>
        <p class="text-sm mt-1">{{ t('settings.addProviderHint') }}</p>
      </div>
    </div>

    <!-- Modal -->
    <Teleport to="body">
      <div v-if="showModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
        <div class="bg-white rounded-xl shadow-xl w-full max-w-2xl mx-4 max-h-[90vh] overflow-y-auto">
          <div class="p-6 border-b">
            <h2 class="text-lg font-semibold">
              {{ editingConfig ? t('settings.editProvider') : t('settings.addProvider') }}
            </h2>
          </div>

          <div class="p-6 space-y-4">
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.providerType') }}</label>
                <select v-model="form.providerType" class="input-field">
                  <option v-for="pt in providerTypes" :key="pt.value" :value="pt.value">{{ pt.label }}</option>
                </select>
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.providerName') }}</label>
                <input v-model="form.providerName" type="text" class="input-field" :placeholder="t('settings.providerNamePlaceholder')" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.endpoint') }}</label>
                <input v-model="form.endpoint" type="url" class="input-field" dir="ltr" placeholder="https://..." />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.apiKey') }}</label>
                <input v-model="form.apiKey" type="password" class="input-field" dir="ltr" :placeholder="editingConfig ? t('settings.leaveBlankToKeep') : ''" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.model') }}</label>
                <input v-model="form.modelName" type="text" class="input-field" dir="ltr" placeholder="gemini-pro" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.temperature') }} (0-1)</label>
                <input v-model.number="form.temperature" type="number" step="0.1" min="0" max="1" class="input-field" dir="ltr" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.maxTokens') }}</label>
                <input v-model.number="form.maxTokens" type="number" min="256" max="32768" class="input-field" dir="ltr" />
              </div>
              <div class="flex items-center gap-2 pt-6">
                <input id="isActive" v-model="form.isActive" type="checkbox" class="h-4 w-4 text-primary-600 rounded border-gray-300" />
                <label for="isActive" class="text-sm font-medium text-gray-700">{{ t('settings.markActive') }}</label>
              </div>
            </div>

            <!-- RAG Settings -->
            <div class="border-t pt-4 mt-4">
              <h3 class="text-sm font-semibold text-gray-700 mb-3">{{ t('settings.ragSettings') }}</h3>
              <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.vectorDb') }}</label>
                  <input v-model="form.vectorDbEndpoint" type="url" class="input-field" dir="ltr" placeholder="http://qdrant:6333" />
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.embeddingModel') }}</label>
                  <input v-model="form.embeddingModel" type="text" class="input-field" dir="ltr" placeholder="text-embedding-004" />
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.chunkSize') }}</label>
                  <input v-model.number="form.chunkSize" type="number" min="128" max="2048" class="input-field" dir="ltr" />
                </div>
              </div>
            </div>
          </div>

          <div class="p-6 border-t flex justify-end gap-3">
            <button @click="showModal = false" class="btn-secondary">{{ t('common.cancel') }}</button>
            <button @click="saveConfig" class="btn-primary" :disabled="settingsStore.isLoading">
              {{ settingsStore.isLoading ? t('common.loading') : t('common.save') }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
