<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSettingsStore } from '../../stores/settings'
import { getCurrentLocale } from '../../i18n'

const { t } = useI18n()
const settingsStore = useSettingsStore()
const locale = computed(() => getCurrentLocale())

const showAddModal = ref(false)
const addForm = ref({
  titleAr: '',
  titleEn: '',
  descriptionAr: '',
  descriptionEn: '',
  sourceType: 'Manual',
})

const sourceTypes = [
  { value: 'Manual', label: 'settings.sourceTypes.manual' },
  { value: 'ApprovedDocument', label: 'settings.sourceTypes.approvedDocument' },
  { value: 'LegalReference', label: 'settings.sourceTypes.legalReference' },
  { value: 'PolicyDocument', label: 'settings.sourceTypes.policyDocument' },
]

onMounted(async () => {
  await settingsStore.fetchKnowledgeBase()
})

function getTitle(item: any) {
  return locale.value === 'ar' ? item.titleAr : item.titleEn
}

function getDescription(item: any) {
  return locale.value === 'ar' ? item.descriptionAr : item.descriptionEn
}

function formatDate(dateStr: string) {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString(locale.value === 'ar' ? 'ar-SA' : 'en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function getStatusColor(status: string) {
  switch (status) {
    case 'Indexed': return 'bg-green-100 text-green-700'
    case 'Pending': return 'bg-yellow-100 text-yellow-700'
    case 'Indexing': return 'bg-blue-100 text-blue-700'
    case 'Failed': return 'bg-red-100 text-red-700'
    default: return 'bg-gray-100 text-gray-700'
  }
}

function getStatusText(status: string) {
  switch (status) {
    case 'Indexed': return t('settings.indexed')
    case 'Pending': return t('settings.pending')
    case 'Indexing': return t('settings.indexing')
    case 'Failed': return t('settings.indexFailed')
    default: return status
  }
}

async function addSource() {
  const ok = await settingsStore.addKnowledgeSource(addForm.value)
  if (ok) {
    showAddModal.value = false
    addForm.value = { titleAr: '', titleEn: '', descriptionAr: '', descriptionEn: '', sourceType: 'Manual' }
  }
}

async function deleteSource(id: string) {
  if (confirm(t('settings.confirmDelete'))) {
    await settingsStore.deleteKnowledgeSource(id)
  }
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ t('settings.knowledgeBase') }}</h1>
        <p class="text-gray-500 mt-1">{{ t('settings.knowledgeBaseDesc') }}</p>
      </div>
      <button @click="showAddModal = true" class="btn-primary">
        <svg class="w-5 h-5 me-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('settings.addSource') }}
      </button>
    </div>

    <!-- Stats Cards -->
    <div v-if="settingsStore.knowledgeBase" class="grid grid-cols-1 sm:grid-cols-3 gap-4">
      <div class="card flex items-center gap-4">
        <div class="p-3 rounded-xl bg-blue-100">
          <svg class="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
        </div>
        <div>
          <p class="text-sm text-gray-500">{{ t('settings.totalDocuments') }}</p>
          <p class="text-2xl font-bold text-gray-900">{{ settingsStore.knowledgeBase.totalDocuments }}</p>
        </div>
      </div>
      <div class="card flex items-center gap-4">
        <div class="p-3 rounded-xl bg-green-100">
          <svg class="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        </div>
        <div>
          <p class="text-sm text-gray-500">{{ t('settings.indexedDocuments') }}</p>
          <p class="text-2xl font-bold text-gray-900">{{ settingsStore.knowledgeBase.totalIndexed }}</p>
        </div>
      </div>
      <div class="card flex items-center gap-4">
        <div class="p-3 rounded-xl bg-yellow-100">
          <svg class="w-6 h-6 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        </div>
        <div>
          <p class="text-sm text-gray-500">{{ t('settings.pendingDocuments') }}</p>
          <p class="text-2xl font-bold text-gray-900">{{ settingsStore.knowledgeBase.totalPending }}</p>
        </div>
      </div>
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

    <!-- Sources List -->
    <div v-else-if="settingsStore.knowledgeBase?.sources?.length" class="space-y-3">
      <div
        v-for="source in settingsStore.knowledgeBase.sources"
        :key="source.id"
        class="card flex items-start justify-between"
      >
        <div class="flex-1">
          <div class="flex items-center gap-3 mb-2">
            <h3 class="font-semibold text-gray-900">{{ getTitle(source) }}</h3>
            <span class="text-xs px-2 py-0.5 rounded-full" :class="getStatusColor(source.indexingStatus)">
              {{ getStatusText(source.indexingStatus) }}
            </span>
          </div>
          <p v-if="getDescription(source)" class="text-sm text-gray-500 mb-2">{{ getDescription(source) }}</p>
          <div class="flex gap-4 text-xs text-gray-400">
            <span>{{ t('settings.sourceType') }}: {{ t(`settings.sourceTypes.${source.sourceType.charAt(0).toLowerCase() + source.sourceType.slice(1)}`) }}</span>
            <span>{{ t('settings.documents') }}: {{ source.documentCount }}</span>
            <span v-if="source.lastIndexedAt">{{ t('settings.lastIndexed') }}: {{ formatDate(source.lastIndexedAt) }}</span>
          </div>
        </div>
        <button @click="deleteSource(source.id)" class="p-2 text-gray-400 hover:text-red-600 rounded">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="text-center py-12 text-gray-400">
      <svg class="w-16 h-16 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 6.042A8.967 8.967 0 006 3.75c-1.052 0-2.062.18-3 .512v14.25A8.987 8.987 0 016 18c2.305 0 4.408.867 6 2.292m0-14.25a8.966 8.966 0 016-2.292c1.052 0 2.062.18 3 .512v14.25A8.987 8.987 0 0018 18a8.967 8.967 0 00-6 2.292m0-14.25v14.25" />
      </svg>
      <p class="text-lg font-medium">{{ t('settings.noKnowledgeSources') }}</p>
      <p class="text-sm mt-1">{{ t('settings.addSourceHint') }}</p>
    </div>

    <!-- Add Modal -->
    <Teleport to="body">
      <div v-if="showAddModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
        <div class="bg-white rounded-xl shadow-xl w-full max-w-lg mx-4">
          <div class="p-6 border-b">
            <h2 class="text-lg font-semibold">{{ t('settings.addSource') }}</h2>
          </div>
          <div class="p-6 space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.sourceType') }}</label>
              <select v-model="addForm.sourceType" class="input-field">
                <option v-for="st in sourceTypes" :key="st.value" :value="st.value">{{ t(st.label) }}</option>
              </select>
            </div>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.titleAr') }}</label>
                <input v-model="addForm.titleAr" type="text" class="input-field" dir="rtl" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.titleEn') }}</label>
                <input v-model="addForm.titleEn" type="text" class="input-field" dir="ltr" />
              </div>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.descriptionAr') }}</label>
              <textarea v-model="addForm.descriptionAr" rows="2" class="input-field" dir="rtl"></textarea>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('settings.descriptionEn') }}</label>
              <textarea v-model="addForm.descriptionEn" rows="2" class="input-field" dir="ltr"></textarea>
            </div>
          </div>
          <div class="p-6 border-t flex justify-end gap-3">
            <button @click="showAddModal = false" class="btn-secondary">{{ t('common.cancel') }}</button>
            <button @click="addSource" class="btn-primary" :disabled="settingsStore.isLoading">
              {{ settingsStore.isLoading ? t('common.loading') : t('common.save') }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
