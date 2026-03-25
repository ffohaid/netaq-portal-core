<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useDashboardStore } from '../../stores/dashboard'
import { getCurrentLocale } from '../../i18n'

const { t } = useI18n()
const dashboardStore = useDashboardStore()
const locale = computed(() => getCurrentLocale())

const activeReport = ref<'tenders' | 'sla' | 'users'>('tenders')

onMounted(async () => {
  await dashboardStore.fetchTenderReport()
})

async function switchReport(report: 'tenders' | 'sla' | 'users') {
  activeReport.value = report
  switch (report) {
    case 'tenders':
      await dashboardStore.fetchTenderReport()
      break
    case 'sla':
      await dashboardStore.fetchSlaReport()
      break
    case 'users':
      await dashboardStore.fetchUserActivityReport()
      break
  }
}

function formatDate(dateStr: string) {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString(locale.value === 'ar' ? 'ar-SA' : 'en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function getUserName(user: any) {
  return locale.value === 'ar' ? user.fullNameAr : user.fullNameEn
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div>
      <h1 class="text-2xl font-bold text-gray-900">{{ t('reports.title') }}</h1>
      <p class="text-gray-500 mt-1">{{ t('reports.description') }}</p>
    </div>

    <!-- Report Tabs -->
    <div class="border-b border-gray-200">
      <nav class="flex gap-6">
        <button
          @click="switchReport('tenders')"
          class="pb-3 text-sm font-medium border-b-2 transition-colors"
          :class="activeReport === 'tenders' ? 'border-primary-500 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'"
        >
          {{ t('reports.tenderStatus') }}
        </button>
        <button
          @click="switchReport('sla')"
          class="pb-3 text-sm font-medium border-b-2 transition-colors"
          :class="activeReport === 'sla' ? 'border-primary-500 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'"
        >
          {{ t('reports.slaCompliance') }}
        </button>
        <button
          @click="switchReport('users')"
          class="pb-3 text-sm font-medium border-b-2 transition-colors"
          :class="activeReport === 'users' ? 'border-primary-500 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'"
        >
          {{ t('reports.userActivity') }}
        </button>
      </nav>
    </div>

    <!-- Loading -->
    <div v-if="dashboardStore.isLoading" class="flex items-center justify-center py-12">
      <svg class="animate-spin w-8 h-8 text-primary-500" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
      </svg>
    </div>

    <!-- Error State -->
    <div v-else-if="dashboardStore.error" class="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
      <svg class="w-12 h-12 text-red-400 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z" />
      </svg>
      <p class="text-red-700 font-medium">{{ dashboardStore.error }}</p>
    </div>

    <!-- Empty State -->
    <div v-else-if="!dashboardStore.isLoading && !dashboardStore.tenderReport && activeReport === 'tenders'" class="text-center py-12 text-gray-400">
      <svg class="w-16 h-16 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
      </svg>
      <p class="text-lg font-medium">{{ locale === 'ar' ? '\u0644\u0627 \u062a\u0648\u062c\u062f \u0628\u064a\u0627\u0646\u0627\u062a \u0644\u0644\u062a\u0642\u0631\u064a\u0631' : 'No report data available' }}</p>
    </div>

    <!-- Tender Status Report -->
    <template v-else-if="activeReport === 'tenders' && dashboardStore.tenderReport">
      <div class="space-y-6">
        <div class="card">
          <div class="flex items-center justify-between mb-6">
            <h2 class="text-lg font-semibold text-gray-900">{{ t('reports.tenderStatus') }}</h2>
            <span class="text-xs text-gray-400">{{ t('reports.generatedAt') }}: {{ formatDate(dashboardStore.tenderReport.generatedAt) }}</span>
          </div>

          <!-- Summary -->
          <div class="grid grid-cols-2 sm:grid-cols-3 gap-4 mb-6">
            <div class="p-4 bg-gray-50 rounded-lg text-center">
              <p class="text-3xl font-bold text-gray-900">{{ dashboardStore.tenderReport.totalTenders }}</p>
              <p class="text-sm text-gray-500">{{ t('reports.totalTenders') }}</p>
            </div>
            <div class="p-4 bg-blue-50 rounded-lg text-center">
              <p class="text-3xl font-bold text-blue-600">{{ dashboardStore.tenderReport.averageCompletionPercentage.toFixed(1) }}%</p>
              <p class="text-sm text-gray-500">{{ t('reports.avgCompletion') }}</p>
            </div>
          </div>

          <!-- By Status -->
          <h3 class="text-sm font-semibold text-gray-700 mb-3">{{ t('reports.byStatus') }}</h3>
          <div class="overflow-x-auto mb-6">
            <table class="w-full text-sm">
              <thead>
                <tr class="text-start text-gray-500 border-b">
                  <th class="pb-3 font-medium">{{ t('common.status') }}</th>
                  <th class="pb-3 font-medium">{{ t('reports.count') }}</th>
                  <th class="pb-3 font-medium">{{ t('reports.percentage') }}</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100">
                <tr v-for="(count, status) in dashboardStore.tenderReport.byStatus" :key="status">
                  <td class="py-3 font-medium text-gray-900">{{ t(`tenders.status.${status}`) }}</td>
                  <td class="py-3 text-gray-600">{{ count }}</td>
                  <td class="py-3">
                    <div class="flex items-center gap-2">
                      <div class="w-24 bg-gray-100 rounded-full h-1.5">
                        <div class="bg-primary-500 h-1.5 rounded-full" :style="{ width: `${dashboardStore.tenderReport!.totalTenders > 0 ? (count / dashboardStore.tenderReport!.totalTenders) * 100 : 0}%` }"></div>
                      </div>
                      <span class="text-xs text-gray-500">{{ dashboardStore.tenderReport!.totalTenders > 0 ? ((count / dashboardStore.tenderReport!.totalTenders) * 100).toFixed(1) : 0 }}%</span>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- By Type -->
          <h3 class="text-sm font-semibold text-gray-700 mb-3">{{ t('reports.byType') }}</h3>
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="text-start text-gray-500 border-b">
                  <th class="pb-3 font-medium">{{ t('tenders.tenderType') }}</th>
                  <th class="pb-3 font-medium">{{ t('reports.count') }}</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100">
                <tr v-for="(count, type) in dashboardStore.tenderReport.byType" :key="type">
                  <td class="py-3 font-medium text-gray-900">{{ t(`tenders.type.${type}`) }}</td>
                  <td class="py-3 text-gray-600">{{ count }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </template>

    <!-- SLA Compliance Report -->
    <template v-else-if="activeReport === 'sla' && dashboardStore.slaReport">
      <div class="space-y-6">
        <div class="card">
          <div class="flex items-center justify-between mb-6">
            <h2 class="text-lg font-semibold text-gray-900">{{ t('reports.slaCompliance') }}</h2>
            <span class="text-xs text-gray-400">{{ t('reports.generatedAt') }}: {{ formatDate(dashboardStore.slaReport.generatedAt) }}</span>
          </div>

          <!-- SLA Summary -->
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6">
            <div class="p-4 bg-green-50 rounded-lg text-center">
              <p class="text-3xl font-bold text-green-600">{{ dashboardStore.slaReport.onTrackCount }}</p>
              <p class="text-sm text-gray-500">{{ t('tasks.sla.onTrack') }}</p>
            </div>
            <div class="p-4 bg-yellow-50 rounded-lg text-center">
              <p class="text-3xl font-bold text-yellow-600">{{ dashboardStore.slaReport.atRiskCount }}</p>
              <p class="text-sm text-gray-500">{{ t('tasks.sla.atRisk') }}</p>
            </div>
            <div class="p-4 bg-red-50 rounded-lg text-center">
              <p class="text-3xl font-bold text-red-600">{{ dashboardStore.slaReport.overdueCount }}</p>
              <p class="text-sm text-gray-500">{{ t('tasks.sla.overdue') }}</p>
            </div>
            <div class="p-4 rounded-lg text-center" :class="dashboardStore.slaReport.complianceRate >= 80 ? 'bg-green-50' : 'bg-red-50'">
              <p class="text-3xl font-bold" :class="dashboardStore.slaReport.complianceRate >= 80 ? 'text-green-600' : 'text-red-600'">
                {{ dashboardStore.slaReport.complianceRate.toFixed(1) }}%
              </p>
              <p class="text-sm text-gray-500">{{ t('reports.complianceRate') }}</p>
            </div>
          </div>

          <!-- Task Metrics -->
          <h3 class="text-sm font-semibold text-gray-700 mb-3">{{ t('reports.taskMetrics') }}</h3>
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div class="p-4 bg-gray-50 rounded-lg">
              <p class="text-2xl font-bold text-gray-900">{{ dashboardStore.slaReport.totalTasks }}</p>
              <p class="text-sm text-gray-500">{{ t('dashboard.totalTasks') }}</p>
            </div>
            <div class="p-4 bg-gray-50 rounded-lg">
              <p class="text-2xl font-bold text-orange-600">{{ dashboardStore.slaReport.escalatedTaskCount }}</p>
              <p class="text-sm text-gray-500">{{ t('reports.escalatedTasks') }}</p>
            </div>
            <div class="p-4 bg-gray-50 rounded-lg">
              <p class="text-2xl font-bold text-gray-900">{{ dashboardStore.slaReport.averageTaskCompletionHours.toFixed(1) }}h</p>
              <p class="text-sm text-gray-500">{{ t('reports.avgCompletionTime') }}</p>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- User Activity Report -->
    <template v-else-if="activeReport === 'users' && dashboardStore.userActivityReport">
      <div class="space-y-6">
        <div class="card">
          <div class="flex items-center justify-between mb-6">
            <h2 class="text-lg font-semibold text-gray-900">{{ t('reports.userActivity') }}</h2>
            <span class="text-xs text-gray-400">{{ t('reports.generatedAt') }}: {{ formatDate(dashboardStore.userActivityReport.generatedAt) }}</span>
          </div>

          <!-- User Summary -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-6">
            <div class="p-4 bg-gray-50 rounded-lg text-center">
              <p class="text-3xl font-bold text-gray-900">{{ dashboardStore.userActivityReport.totalUsers }}</p>
              <p class="text-sm text-gray-500">{{ t('reports.totalUsers') }}</p>
            </div>
            <div class="p-4 bg-green-50 rounded-lg text-center">
              <p class="text-3xl font-bold text-green-600">{{ dashboardStore.userActivityReport.activeInLast30Days }}</p>
              <p class="text-sm text-gray-500">{{ t('reports.activeLast30Days') }}</p>
            </div>
          </div>

          <!-- Users by Role -->
          <h3 class="text-sm font-semibold text-gray-700 mb-3">{{ t('reports.usersByRole') }}</h3>
          <div class="overflow-x-auto mb-6">
            <table class="w-full text-sm">
              <thead>
                <tr class="text-start text-gray-500 border-b">
                  <th class="pb-3 font-medium">{{ t('invitation.role') }}</th>
                  <th class="pb-3 font-medium">{{ t('reports.count') }}</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100">
                <tr v-for="(count, role) in dashboardStore.userActivityReport.usersByRole" :key="role">
                  <td class="py-3 font-medium text-gray-900">{{ t(`roles.${role}`) }}</td>
                  <td class="py-3 text-gray-600">{{ count }}</td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Top Active Users -->
          <h3 class="text-sm font-semibold text-gray-700 mb-3">{{ t('reports.topActiveUsers') }}</h3>
          <div class="overflow-x-auto" v-if="dashboardStore.userActivityReport.topActiveUsers?.length">
            <table class="w-full text-sm">
              <thead>
                <tr class="text-start text-gray-500 border-b">
                  <th class="pb-3 font-medium">#</th>
                  <th class="pb-3 font-medium">{{ t('reports.userName') }}</th>
                  <th class="pb-3 font-medium">{{ t('invitation.role') }}</th>
                  <th class="pb-3 font-medium">{{ t('reports.actionCount') }}</th>
                  <th class="pb-3 font-medium">{{ t('reports.lastActive') }}</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100">
                <tr v-for="(user, index) in dashboardStore.userActivityReport.topActiveUsers" :key="user.userId">
                  <td class="py-3 text-gray-400">{{ index + 1 }}</td>
                  <td class="py-3 font-medium text-gray-900">{{ getUserName(user) }}</td>
                  <td class="py-3 text-gray-600">{{ t(`roles.${user.role}`) }}</td>
                  <td class="py-3 text-gray-600">{{ user.actionCount }}</td>
                  <td class="py-3 text-gray-500">{{ formatDate(user.lastActiveAt) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
