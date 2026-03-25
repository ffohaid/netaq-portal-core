<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useTenderStore } from '../../stores/tenders'
import { getCurrentLocale } from '../../i18n'
import type { TenderStatus, TenderType } from '../../types'

const { t } = useI18n()
const router = useRouter()
const tenderStore = useTenderStore()

const statusFilter = ref<TenderStatus | ''>('')
const typeFilter = ref<TenderType | ''>('')
const searchTerm = ref('')
const currentPage = ref(1)
const pageSize = 20
const locale = computed(() => getCurrentLocale())

const tenderTypes: TenderType[] = [
  'GeneralSupply', 'PharmaceuticalSupply', 'MedicalSupply', 'MilitarySupply',
  'GeneralServices', 'CateringServices', 'CityCleaning', 'BuildingMaintenance',
  'GeneralConsulting', 'EngineeringDesign', 'EngineeringSupervision',
  'GeneralConstruction', 'RoadConstruction', 'RoadMaintenance',
  'InformationTechnology',
  'FrameworkAgreementSupply', 'FrameworkAgreementServices', 'FrameworkAgreementConsulting',
  'RevenueSharing', 'PerformanceBasedContract', 'CapacityStudy',
]

const tenderStatuses: TenderStatus[] = ['Draft', 'PendingApproval', 'Approved', 'EvaluationInProgress', 'EvaluationCompleted', 'Archived', 'Cancelled']

async function loadTenders() {
  await tenderStore.fetchTenders({
    pageNumber: currentPage.value,
    pageSize,
    status: statusFilter.value || undefined,
    type: typeFilter.value || undefined,
    search: searchTerm.value || undefined,
  })
}

function getTitle(tender: { titleAr: string; titleEn: string }) {
  return locale.value === 'ar' ? tender.titleAr : tender.titleEn
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString(locale.value === 'ar' ? 'ar-SA' : 'en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat(locale.value === 'ar' ? 'ar-SA' : 'en-US', {
    style: 'decimal',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value)
}

function getStatusClass(status: TenderStatus) {
  const classes: Record<TenderStatus, string> = {
    Draft: 'bg-gray-100 text-gray-700',
    PendingApproval: 'bg-yellow-100 text-yellow-700',
    Approved: 'bg-green-100 text-green-700',
    EvaluationInProgress: 'bg-blue-100 text-blue-700',
    EvaluationCompleted: 'bg-emerald-100 text-emerald-700',
    Archived: 'bg-purple-100 text-purple-700',
    Cancelled: 'bg-red-100 text-red-700',
  }
  return classes[status] || 'bg-gray-100 text-gray-700'
}

watch([statusFilter, typeFilter], () => {
  currentPage.value = 1
  loadTenders()
})

let searchTimeout: ReturnType<typeof setTimeout>
watch(searchTerm, () => {
  clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    currentPage.value = 1
    loadTenders()
  }, 400)
})

onMounted(() => {
  loadTenders()
})
</script>

<template>
  <div class="p-6">
    <!-- Header -->
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ t('tenders.title') }}</h1>
        <p class="text-gray-500 mt-1">{{ t('tenders.list') }}</p>
      </div>
      <button
        @click="router.push('/tenders/create')"
        class="bg-primary-600 text-white px-4 py-2.5 rounded-lg hover:bg-primary-700 transition-colors flex items-center gap-2"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('tenders.create') }}
      </button>
    </div>

    <!-- Filters -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4 mb-6">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <!-- Search -->
        <div class="relative">
          <svg class="absolute start-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input
            v-model="searchTerm"
            type="text"
            :placeholder="t('common.search')"
            class="w-full ps-10 pe-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
          />
        </div>
        <!-- Status Filter -->
        <select
          v-model="statusFilter"
          class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
        >
          <option value="">{{ t('common.all') }} - {{ t('common.status') }}</option>
          <option v-for="status in tenderStatuses" :key="status" :value="status">
            {{ t(`tenders.status.${status}`) }}
          </option>
        </select>
        <!-- Type Filter -->
        <select
          v-model="typeFilter"
          class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
        >
          <option value="">{{ t('common.all') }} - {{ t('tenders.tenderType') }}</option>
          <option v-for="type in tenderTypes" :key="type" :value="type">
            {{ t(`tenders.type.${type}`) }}
          </option>
        </select>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="tenderStore.isLoading" class="flex justify-center py-12">
      <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-primary-600"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="tenderStore.tenders.length === 0" class="bg-white rounded-xl shadow-sm border border-gray-200 p-12 text-center">
      <svg class="mx-auto h-16 w-16 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M19 20H5a2 2 0 01-2-2V6a2 2 0 012-2h10a2 2 0 012 2v1m2 13a2 2 0 01-2-2V7m2 13a2 2 0 002-2V9a2 2 0 00-2-2h-2m-4-3H9M7 16h6M7 8h6v4H7V8z" />
      </svg>
      <h3 class="mt-4 text-lg font-medium text-gray-900">{{ t('common.noData') }}</h3>
      <p class="mt-2 text-gray-500">{{ t('tenders.create') }}</p>
      <button
        @click="router.push('/tenders/create')"
        class="mt-4 bg-primary-600 text-white px-4 py-2 rounded-lg hover:bg-primary-700 transition-colors"
      >
        {{ t('tenders.create') }}
      </button>
    </div>

    <!-- Tenders Table -->
    <div v-else class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
      <table class="w-full">
        <thead class="bg-gray-50 border-b border-gray-200">
          <tr>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('tenders.referenceNumber') }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('tenders.titleAr').replace(' (عربي)', '').replace(' (Arabic)', '') }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('tenders.tenderType') }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('tenders.estimatedValue') }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('common.status') }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('tenders.completionPercentage') }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('common.actions') }}</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-100">
          <tr
            v-for="tender in tenderStore.tenders"
            :key="tender.id"
            class="hover:bg-gray-50 transition-colors cursor-pointer"
            @click="router.push(`/tenders/${tender.id}`)"
          >
            <td class="px-4 py-3 text-sm font-mono text-gray-600">{{ tender.referenceNumber }}</td>
            <td class="px-4 py-3">
              <div class="text-sm font-medium text-gray-900">{{ getTitle(tender) }}</div>
            </td>
            <td class="px-4 py-3 text-sm text-gray-600">{{ t(`tenders.type.${tender.tenderType}`) }}</td>
            <td class="px-4 py-3 text-sm text-gray-600">{{ formatCurrency(tender.estimatedValue) }} {{ t('common.sar') }}</td>
            <td class="px-4 py-3">
              <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium" :class="getStatusClass(tender.status)">
                {{ t(`tenders.status.${tender.status}`) }}
              </span>
            </td>
            <td class="px-4 py-3">
              <div class="flex items-center gap-2">
                <div class="flex-1 bg-gray-200 rounded-full h-2 max-w-[80px]">
                  <div
                    class="bg-primary-600 h-2 rounded-full transition-all"
                    :style="{ width: `${tender.completionPercentage}%` }"
                  ></div>
                </div>
                <span class="text-xs text-gray-500">{{ tender.completionPercentage }}%</span>
              </div>
            </td>
            <td class="px-4 py-3">
              <button
                @click.stop="router.push(`/tenders/${tender.id}`)"
                class="text-primary-600 hover:text-primary-700 text-sm font-medium"
              >
                {{ t('common.view') }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>

      <!-- Pagination -->
      <div v-if="tenderStore.totalPages > 1" class="flex items-center justify-between px-4 py-3 border-t border-gray-200">
        <div class="text-sm text-gray-500">
          {{ t('common.of') }} {{ tenderStore.totalCount }}
        </div>
        <div class="flex gap-2">
          <button
            @click="currentPage--; loadTenders()"
            :disabled="currentPage <= 1"
            class="px-3 py-1.5 text-sm border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
          >
            {{ t('common.previous') }}
          </button>
          <span class="px-3 py-1.5 text-sm text-gray-600">{{ currentPage }} / {{ tenderStore.totalPages }}</span>
          <button
            @click="currentPage++; loadTenders()"
            :disabled="currentPage >= tenderStore.totalPages"
            class="px-3 py-1.5 text-sm border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
          >
            {{ t('common.next') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
