<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useInquiryStore } from '../../stores/inquiries'
import { getCurrentLocale } from '../../i18n'

const { t } = useI18n()
const router = useRouter()
const store = useInquiryStore()

const statusFilter = ref('')
const searchTerm = ref('')
const currentPage = ref(1)
const pageSize = 20
const locale = computed(() => getCurrentLocale())

async function loadInquiries() {
  await store.fetchInquiries({
    pageNumber: currentPage.value,
    pageSize,
    status: statusFilter.value || undefined,
    search: searchTerm.value || undefined,
  })
}

function getSubject(item: any) {
  return locale.value === 'ar' ? item.subjectAr : item.subjectEn
}

function getTenderTitle(item: any) {
  return locale.value === 'ar' ? item.tenderTitleAr : item.tenderTitleEn
}

function getSubmitter(item: any) {
  return locale.value === 'ar' ? item.submittedByNameAr : item.submittedByNameEn
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString(locale.value === 'ar' ? 'ar-SA' : 'en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

function getStatusClass(status: string) {
  const map: Record<string, string> = {
    Pending: 'bg-yellow-100 text-yellow-700',
    Answered: 'bg-green-100 text-green-700',
    Rejected: 'bg-red-100 text-red-700',
    Escalated: 'bg-orange-100 text-orange-700',
  }
  return map[status] || 'bg-gray-100 text-gray-700'
}

function getPriorityClass(priority: string) {
  const map: Record<string, string> = {
    Low: 'bg-blue-100 text-blue-700',
    Medium: 'bg-yellow-100 text-yellow-700',
    High: 'bg-orange-100 text-orange-700',
    Critical: 'bg-red-100 text-red-700',
  }
  return map[priority] || 'bg-gray-100 text-gray-700'
}

watch(statusFilter, () => {
  currentPage.value = 1
  loadInquiries()
})

let searchTimeout: ReturnType<typeof setTimeout>
watch(searchTerm, () => {
  clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    currentPage.value = 1
    loadInquiries()
  }, 400)
})

onMounted(() => loadInquiries())
</script>

<template>
  <div class="p-6">
    <!-- Header -->
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ t('inquiries.title') }}</h1>
        <p class="text-gray-500 mt-1">{{ t('inquiries.subtitle') }}</p>
      </div>
      <button
        @click="router.push('/inquiries/create')"
        class="bg-primary-600 text-white px-4 py-2.5 rounded-lg hover:bg-primary-700 transition-colors flex items-center gap-2"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('inquiries.create') }}
      </button>
    </div>

    <!-- Filters -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4 mb-6">
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div class="relative">
          <svg class="absolute start-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input v-model="searchTerm" type="text" :placeholder="t('common.search')"
            class="w-full ps-10 pe-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500" />
        </div>
        <select v-model="statusFilter"
          class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500">
          <option value="">{{ t('common.all') }} - {{ t('common.status') }}</option>
          <option value="Pending">{{ t('inquiries.statusPending') }}</option>
          <option value="Answered">{{ t('inquiries.statusAnswered') }}</option>
          <option value="Rejected">{{ t('inquiries.statusRejected') }}</option>
          <option value="Escalated">{{ t('inquiries.statusEscalated') }}</option>
        </select>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="store.isLoading" class="flex justify-center py-12">
      <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-primary-600"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="store.inquiries.length === 0" class="bg-white rounded-xl shadow-sm border border-gray-200 p-12 text-center">
      <svg class="mx-auto h-16 w-16 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
      <h3 class="mt-4 text-lg font-medium text-gray-900">{{ t('inquiries.noInquiries') }}</h3>
      <p class="mt-2 text-gray-500">{{ t('inquiries.noInquiriesDesc') }}</p>
    </div>

    <!-- Inquiries Table -->
    <div v-else class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
      <table class="w-full">
        <thead class="bg-gray-50 border-b border-gray-200">
          <tr>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('inquiries.subject') }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('inquiries.tender') }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('inquiries.submittedBy') }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('inquiries.priority') }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('common.status') }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('inquiries.submittedAt') }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('common.actions') }}</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-100">
          <tr
            v-for="inquiry in store.inquiries"
            :key="inquiry.id"
            class="hover:bg-gray-50 transition-colors cursor-pointer"
            @click="router.push(`/inquiries/${inquiry.id}`)"
          >
            <td class="px-4 py-3 text-sm font-medium text-gray-900">{{ getSubject(inquiry) }}</td>
            <td class="px-4 py-3 text-sm text-gray-600">
              <div>{{ getTenderTitle(inquiry) }}</div>
              <div class="text-xs text-gray-400 font-mono">{{ inquiry.tenderReferenceNumber }}</div>
            </td>
            <td class="px-4 py-3 text-sm text-gray-600">{{ getSubmitter(inquiry) }}</td>
            <td class="px-4 py-3">
              <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium" :class="getPriorityClass(inquiry.priority)">
                {{ t(`inquiries.priority${inquiry.priority}`) }}
              </span>
            </td>
            <td class="px-4 py-3">
              <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium" :class="getStatusClass(inquiry.status)">
                {{ t(`inquiries.status${inquiry.status}`) }}
              </span>
            </td>
            <td class="px-4 py-3 text-sm text-gray-600">{{ formatDate(inquiry.submittedAt) }}</td>
            <td class="px-4 py-3">
              <button @click.stop="router.push(`/inquiries/${inquiry.id}`)" class="text-primary-600 hover:text-primary-700 text-sm font-medium">
                {{ t('common.view') }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>

      <!-- Pagination -->
      <div v-if="store.totalPages > 1" class="flex items-center justify-between px-4 py-3 border-t border-gray-200">
        <div class="text-sm text-gray-500">{{ t('common.of') }} {{ store.totalCount }}</div>
        <div class="flex gap-2">
          <button @click="currentPage--; loadInquiries()" :disabled="currentPage <= 1"
            class="px-3 py-1.5 text-sm border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50">
            {{ t('common.previous') }}
          </button>
          <span class="px-3 py-1.5 text-sm text-gray-600">{{ currentPage }} / {{ store.totalPages }}</span>
          <button @click="currentPage++; loadInquiries()" :disabled="currentPage >= store.totalPages"
            class="px-3 py-1.5 text-sm border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50">
            {{ t('common.next') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
