<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useCommitteeStore } from '../../stores/committees'
import { getCurrentLocale } from '../../i18n'

const { t } = useI18n()
const router = useRouter()
const store = useCommitteeStore()

const typeFilter = ref<string>('')
const activeFilter = ref<string>('')
const searchTerm = ref('')
const currentPage = ref(1)
const pageSize = 20

const locale = computed(() => getCurrentLocale())

async function loadCommittees() {
  await store.fetchCommittees({
    pageNumber: currentPage.value,
    pageSize,
    type: typeFilter.value || undefined,
    isActive: activeFilter.value === '' ? undefined : activeFilter.value === 'true',
    search: searchTerm.value || undefined,
  })
}

function getName(item: any) {
  return locale.value === 'ar' ? item.nameAr : item.nameEn
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString(locale.value === 'ar' ? 'ar-SA' : 'en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

function getTypeClass(type: string) {
  return type === 'Permanent'
    ? 'bg-blue-100 text-blue-700'
    : 'bg-amber-100 text-amber-700'
}

function getStatusClass(isActive: boolean) {
  return isActive
    ? 'bg-green-100 text-green-700'
    : 'bg-gray-200 text-gray-600'
}

watch([typeFilter, activeFilter], () => {
  currentPage.value = 1
  loadCommittees()
})

let searchTimeout: ReturnType<typeof setTimeout>
watch(searchTerm, () => {
  clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    currentPage.value = 1
    loadCommittees()
  }, 400)
})

onMounted(() => loadCommittees())
</script>

<template>
  <div class="p-6">
    <!-- Header -->
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ t('committees.title') }}</h1>
        <p class="text-gray-500 mt-1">{{ t('committees.subtitle') }}</p>
      </div>
      <button
        @click="router.push('/committees/create')"
        class="bg-primary-600 text-white px-4 py-2.5 rounded-lg hover:bg-primary-700 transition-colors flex items-center gap-2"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('committees.create') }}
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
        <!-- Type Filter -->
        <select
          v-model="typeFilter"
          class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
        >
          <option value="">{{ t('common.all') }} - {{ t('committees.type') }}</option>
          <option value="Permanent">{{ t('committees.permanent') }}</option>
          <option value="AdHoc">{{ t('committees.adHoc') }}</option>
        </select>
        <!-- Active Filter -->
        <select
          v-model="activeFilter"
          class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
        >
          <option value="">{{ t('common.all') }} - {{ t('common.status') }}</option>
          <option value="true">{{ t('committees.active') }}</option>
          <option value="false">{{ t('committees.dissolved') }}</option>
        </select>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="store.isLoading" class="flex justify-center py-12">
      <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-primary-600"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="store.committees.length === 0" class="bg-white rounded-xl shadow-sm border border-gray-200 p-12 text-center">
      <svg class="mx-auto h-16 w-16 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
      </svg>
      <h3 class="mt-4 text-lg font-medium text-gray-900">{{ t('committees.noCommittees') }}</h3>
      <p class="mt-2 text-gray-500">{{ t('committees.noCommitteesDesc') }}</p>
      <button
        @click="router.push('/committees/create')"
        class="mt-4 bg-primary-600 text-white px-4 py-2 rounded-lg hover:bg-primary-700 transition-colors"
      >
        {{ t('committees.create') }}
      </button>
    </div>

    <!-- Committees Table -->
    <div v-else class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
      <table class="w-full">
        <thead class="bg-gray-50 border-b border-gray-200">
          <tr>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('committees.name') }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('committees.type') }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('committees.members') }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('committees.formedAt') }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('common.status') }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('common.actions') }}</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-100">
          <tr
            v-for="committee in store.committees"
            :key="committee.id"
            class="hover:bg-gray-50 transition-colors cursor-pointer"
            @click="router.push(`/committees/${committee.id}`)"
          >
            <td class="px-4 py-3">
              <div class="text-sm font-medium text-gray-900">{{ getName(committee) }}</div>
            </td>
            <td class="px-4 py-3">
              <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium" :class="getTypeClass(committee.type)">
                {{ committee.type === 'Permanent' ? t('committees.permanent') : t('committees.adHoc') }}
              </span>
            </td>
            <td class="px-4 py-3 text-sm text-gray-600">
              <div class="flex items-center gap-1">
                <svg class="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
                </svg>
                {{ committee.memberCount }}
              </div>
            </td>
            <td class="px-4 py-3 text-sm text-gray-600">{{ committee.formedAt ? formatDate(committee.formedAt) : '-' }}</td>
            <td class="px-4 py-3">
              <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium" :class="getStatusClass(committee.isActive)">
                {{ committee.isActive ? t('committees.active') : t('committees.dissolved') }}
              </span>
            </td>
            <td class="px-4 py-3">
              <button
                @click.stop="router.push(`/committees/${committee.id}`)"
                class="text-primary-600 hover:text-primary-700 text-sm font-medium"
              >
                {{ t('common.view') }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>

      <!-- Pagination -->
      <div v-if="store.totalPages > 1" class="flex items-center justify-between px-4 py-3 border-t border-gray-200">
        <div class="text-sm text-gray-500">
          {{ t('common.of') }} {{ store.totalCount }}
        </div>
        <div class="flex gap-2">
          <button
            @click="currentPage--; loadCommittees()"
            :disabled="currentPage <= 1"
            class="px-3 py-1.5 text-sm border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
          >
            {{ t('common.previous') }}
          </button>
          <span class="px-3 py-1.5 text-sm text-gray-600">{{ currentPage }} / {{ store.totalPages }}</span>
          <button
            @click="currentPage++; loadCommittees()"
            :disabled="currentPage >= store.totalPages"
            class="px-3 py-1.5 text-sm border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
          >
            {{ t('common.next') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
