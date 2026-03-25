<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import api from '../../services/api'
import type { AuditLog, AuditIntegrityResult, PaginatedResponse, ApiResponse } from '../../types'
import { getCurrentLocale } from '../../i18n'

const { t } = useI18n()
const locale = computed(() => getCurrentLocale())

const logs = ref<AuditLog[]>([])
const totalCount = ref(0)
const isLoading = ref(false)
const currentPage = ref(1)
const pageSize = 30
const categoryFilter = ref('')
const integrityResult = ref<AuditIntegrityResult | null>(null)
const verifyingIntegrity = ref(false)

async function fetchLogs() {
  isLoading.value = true
  try {
    const params: any = { pageNumber: currentPage.value, pageSize }
    if (categoryFilter.value) params.category = categoryFilter.value

    const response = await api.get<ApiResponse<PaginatedResponse<AuditLog>>>('/audit', { params })
    if (response.data.isSuccess && response.data.data) {
      logs.value = response.data.data.items
      totalCount.value = response.data.data.totalCount
    }
  } catch (err) {
    console.error('Failed to fetch audit logs', err)
  } finally {
    isLoading.value = false
  }
}

async function verifyIntegrity() {
  verifyingIntegrity.value = true
  try {
    const response = await api.get<ApiResponse<AuditIntegrityResult>>('/audit/verify-integrity')
    if (response.data.isSuccess && response.data.data) {
      integrityResult.value = response.data.data
    }
  } catch (err) {
    console.error('Failed to verify integrity', err)
  } finally {
    verifyingIntegrity.value = false
  }
}

function formatTimestamp(dateStr: string) {
  return new Date(dateStr).toLocaleString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  })
}

function getCategoryColor(category: string) {
  switch (category) {
    case 'Authentication': return 'bg-blue-100 text-blue-700'
    case 'WorkflowAction': return 'bg-purple-100 text-purple-700'
    case 'UserManagement': return 'bg-green-100 text-green-700'
    case 'SystemConfiguration': return 'bg-orange-100 text-orange-700'
    default: return 'bg-gray-100 text-gray-700'
  }
}

const totalPages = computed(() => Math.ceil(totalCount.value / pageSize))

onMounted(() => {
  fetchLogs()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ t('audit.title') }}</h1>
        <p class="text-gray-500 mt-1">Immutable cryptographic audit trail</p>
      </div>
      <button
        @click="verifyIntegrity"
        :disabled="verifyingIntegrity"
        class="btn-primary flex items-center gap-2"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
        </svg>
        {{ verifyingIntegrity ? t('common.loading') : t('audit.verifyIntegrity') }}
      </button>
    </div>

    <!-- Integrity Result -->
    <div
      v-if="integrityResult"
      class="p-4 rounded-lg border"
      :class="integrityResult.isValid
        ? 'bg-green-50 border-green-200'
        : 'bg-red-50 border-red-200'"
    >
      <div class="flex items-center gap-3">
        <svg v-if="integrityResult.isValid" class="w-6 h-6 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
        </svg>
        <svg v-else class="w-6 h-6 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
        </svg>
        <p :class="integrityResult.isValid ? 'text-green-700' : 'text-red-700'" class="font-medium">
          {{ integrityResult.isValid ? t('audit.integrityValid') : t('audit.integrityFailed') }}
        </p>
      </div>
    </div>

    <!-- Filter -->
    <div class="card">
      <select v-model="categoryFilter" @change="currentPage = 1; fetchLogs()" class="input-field w-auto min-w-[200px]">
        <option value="">{{ t('audit.actionType') }}: All</option>
        <option value="Authentication">Authentication</option>
        <option value="WorkflowAction">Workflow Action</option>
        <option value="UserManagement">User Management</option>
        <option value="SystemConfiguration">System Configuration</option>
        <option value="DataModification">Data Modification</option>
      </select>
    </div>

    <!-- Audit Log Table -->
    <div class="card p-0 overflow-hidden">
      <div v-if="isLoading" class="flex items-center justify-center py-12">
        <svg class="animate-spin w-8 h-8 text-primary-500" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
        </svg>
      </div>

      <div v-else-if="logs.length === 0" class="text-center py-12 text-gray-400">
        <p>{{ t('common.noData') }}</p>
      </div>

      <table v-else class="w-full text-sm">
        <thead class="bg-gray-50">
          <tr class="text-start text-gray-500">
            <th class="px-4 py-3 font-medium">#</th>
            <th class="px-4 py-3 font-medium">{{ t('audit.actionType') }}</th>
            <th class="px-4 py-3 font-medium">{{ t('audit.description') }}</th>
            <th class="px-4 py-3 font-medium">{{ t('audit.timestamp') }}</th>
            <th class="px-4 py-3 font-medium">{{ t('audit.ipAddress') }}</th>
            <th class="px-4 py-3 font-medium">{{ t('audit.hash') }}</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-100">
          <tr v-for="log in logs" :key="log.id" class="hover:bg-gray-50">
            <td class="px-4 py-3 text-gray-400">{{ log.sequenceNumber }}</td>
            <td class="px-4 py-3">
              <span :class="['text-xs px-2 py-0.5 rounded-full font-medium', getCategoryColor(log.actionCategory)]">
                {{ log.actionType }}
              </span>
            </td>
            <td class="px-4 py-3 text-gray-700 max-w-xs truncate">{{ log.actionDescription }}</td>
            <td class="px-4 py-3 text-gray-500 whitespace-nowrap">{{ formatTimestamp(log.timestamp) }}</td>
            <td class="px-4 py-3 text-gray-400 font-mono text-xs">{{ log.ipAddress }}</td>
            <td class="px-4 py-3 text-gray-400 font-mono text-xs max-w-[120px] truncate" :title="log.hash">
              {{ log.hash.substring(0, 16) }}...
            </td>
          </tr>
        </tbody>
      </table>

      <!-- Pagination -->
      <div v-if="totalPages > 1" class="flex items-center justify-between px-4 py-3 border-t">
        <button @click="currentPage--; fetchLogs()" :disabled="currentPage <= 1" class="btn-secondary text-sm">
          {{ t('common.previous') }}
        </button>
        <span class="text-sm text-gray-500">{{ currentPage }} / {{ totalPages }}</span>
        <button @click="currentPage++; fetchLogs()" :disabled="currentPage >= totalPages" class="btn-secondary text-sm">
          {{ t('common.next') }}
        </button>
      </div>
    </div>
  </div>
</template>
