<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '../../stores/auth'
import { useDashboardStore } from '../../stores/dashboard'
import { useTaskStore } from '../../stores/tasks'
import { getCurrentLocale } from '../../i18n'

const { t } = useI18n()
const authStore = useAuthStore()
const dashboardStore = useDashboardStore()
const taskStore = useTaskStore()
const locale = computed(() => getCurrentLocale())

const activeView = ref<'executive' | 'operational' | 'committee' | 'monitoring'>('executive')

const isAdmin = computed(() => {
  return authStore.user?.role === 'SystemAdmin' || authStore.user?.role === 'OrganizationAdmin'
})

const isCommitteeMember = computed(() => {
  return authStore.user?.role === 'CommitteeChair' || authStore.user?.role === 'CommitteeMember'
})

onMounted(async () => {
  // Auto-select view based on role
  if (isAdmin.value) {
    activeView.value = 'executive'
    await dashboardStore.fetchExecutiveDashboard()
  } else if (isCommitteeMember.value) {
    activeView.value = 'committee'
    await dashboardStore.fetchCommitteeDashboard()
  } else {
    activeView.value = 'operational'
    await dashboardStore.fetchOperationalDashboard()
  }
  await taskStore.fetchStatistics()
  await taskStore.fetchMyTasks({ pageSize: 5 })
})

async function switchView(view: 'executive' | 'operational' | 'committee' | 'monitoring') {
  activeView.value = view
  switch (view) {
    case 'executive':
      await dashboardStore.fetchExecutiveDashboard()
      break
    case 'operational':
      await dashboardStore.fetchOperationalDashboard()
      break
    case 'committee':
      await dashboardStore.fetchCommitteeDashboard()
      break
    case 'monitoring':
      await dashboardStore.fetchMonitoringDashboard()
      break
  }
}

function getTitle(item: any) {
  return locale.value === 'ar' ? (item.titleAr || item.nameAr || item.committeeNameAr) : (item.titleEn || item.nameEn || item.committeeNameEn)
}

function formatDate(dateStr: string) {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString(locale.value === 'ar' ? 'ar-SA' : 'en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

function formatDateTime(dateStr: string) {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString(locale.value === 'ar' ? 'ar-SA' : 'en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function formatCurrency(value: number) {
  if (!value) return '0'
  return new Intl.NumberFormat(locale.value === 'ar' ? 'ar-SA' : 'en-US').format(value)
}

function getSlaClass(status: string) {
  switch (status) {
    case 'OnTrack': return 'badge-success'
    case 'AtRisk': return 'badge-warning'
    case 'Overdue': return 'badge-danger'
    default: return 'badge-info'
  }
}

function getSlaText(status: string) {
  switch (status) {
    case 'OnTrack': return t('tasks.sla.onTrack')
    case 'AtRisk': return t('tasks.sla.atRisk')
    case 'Overdue': return t('tasks.sla.overdue')
    default: return status
  }
}

function getStatusClass(status: string) {
  switch (status) {
    case 'Draft': return 'bg-gray-100 text-gray-700'
    case 'PendingApproval': return 'bg-yellow-100 text-yellow-700'
    case 'Approved': return 'bg-green-100 text-green-700'
    case 'EvaluationInProgress': return 'bg-blue-100 text-blue-700'
    case 'EvaluationCompleted': return 'bg-emerald-100 text-emerald-700'
    case 'Archived': return 'bg-purple-100 text-purple-700'
    case 'Cancelled': return 'bg-red-100 text-red-700'
    default: return 'bg-gray-100 text-gray-700'
  }
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">
          {{ t('dashboard.welcome') }}{{ authStore.userDisplayName ? `, ${authStore.userDisplayName}` : '' }}
        </h1>
        <p class="text-gray-500 mt-1">{{ t('dashboard.title') }}</p>
      </div>
    </div>

    <!-- View Tabs -->
    <div class="border-b border-gray-200">
      <nav class="flex gap-6">
        <button
          v-if="isAdmin"
          @click="switchView('executive')"
          class="pb-3 text-sm font-medium border-b-2 transition-colors"
          :class="activeView === 'executive' ? 'border-primary-500 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'"
        >
          {{ t('dashboard.executiveView') }}
        </button>
        <button
          @click="switchView('operational')"
          class="pb-3 text-sm font-medium border-b-2 transition-colors"
          :class="activeView === 'operational' ? 'border-primary-500 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'"
        >
          {{ t('dashboard.operationalView') }}
        </button>
        <button
          v-if="isCommitteeMember || isAdmin"
          @click="switchView('committee')"
          class="pb-3 text-sm font-medium border-b-2 transition-colors"
          :class="activeView === 'committee' ? 'border-primary-500 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'"
        >
          {{ t('dashboard.committeeView') }}
        </button>
        <button
          v-if="isAdmin"
          @click="switchView('monitoring')"
          class="pb-3 text-sm font-medium border-b-2 transition-colors"
          :class="activeView === 'monitoring' ? 'border-primary-500 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'"
        >
          {{ t('dashboard.monitoringView') }}
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

    <!-- Executive Dashboard -->
    <template v-else-if="activeView === 'executive' && dashboardStore.executiveDashboard">
      <div class="space-y-6">
        <!-- Tender Stats -->
        <div class="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-4 gap-4">
          <div class="card bg-gradient-to-br from-primary-50 to-white border border-primary-100">
            <div class="flex items-center justify-between">
              <div>
                <p class="text-xs text-gray-500 mb-1">{{ t('dashboard.totalTenders') }}</p>
                <p class="text-3xl font-bold text-primary-700">{{ dashboardStore.executiveDashboard.tenderStatistics.totalTenders }}</p>
              </div>
              <div class="w-12 h-12 bg-primary-100 rounded-xl flex items-center justify-center">
                <svg class="w-6 h-6 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 20H5a2 2 0 01-2-2V6a2 2 0 012-2h10a2 2 0 012 2v1m2 13a2 2 0 01-2-2V7m2 13a2 2 0 002-2V9a2 2 0 00-2-2h-2m-4-3H9M7 16h6M7 8h6v4H7V8z" />
                </svg>
              </div>
            </div>
            <div class="mt-3 flex items-center gap-3 text-xs">
              <span class="text-gray-500">{{ t('tenders.status.Draft') }}: <strong class="text-gray-700">{{ dashboardStore.executiveDashboard.tenderStatistics.draftCount }}</strong></span>
              <span class="text-yellow-600">{{ t('tenders.status.PendingApproval') }}: <strong>{{ dashboardStore.executiveDashboard.tenderStatistics.pendingApprovalCount }}</strong></span>
            </div>
          </div>
          <div class="card bg-gradient-to-br from-green-50 to-white border border-green-100">
            <div class="flex items-center justify-between">
              <div>
                <p class="text-xs text-gray-500 mb-1">{{ t('tenders.status.Approved') }}</p>
                <p class="text-3xl font-bold text-green-700">{{ dashboardStore.executiveDashboard.tenderStatistics.approvedCount }}</p>
              </div>
              <div class="w-12 h-12 bg-green-100 rounded-xl flex items-center justify-center">
                <svg class="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
            </div>
            <div class="mt-3 text-xs text-gray-500">
              {{ t('dashboard.evaluating') }}: <strong class="text-blue-600">{{ dashboardStore.executiveDashboard.tenderStatistics.evaluationInProgressCount }}</strong>
            </div>
          </div>
          <div class="card bg-gradient-to-br from-blue-50 to-white border border-blue-100">
            <div class="flex items-center justify-between">
              <div>
                <p class="text-xs text-gray-500 mb-1">{{ t('dashboard.completed') }}</p>
                <p class="text-3xl font-bold text-blue-700">{{ dashboardStore.executiveDashboard.tenderStatistics.evaluationCompletedCount }}</p>
              </div>
              <div class="w-12 h-12 bg-blue-100 rounded-xl flex items-center justify-center">
                <svg class="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                </svg>
              </div>
            </div>
            <div class="mt-3 text-xs text-gray-500">
              {{ t('tenders.status.Cancelled') }}: <strong class="text-red-600">{{ dashboardStore.executiveDashboard.tenderStatistics.cancelledCount }}</strong>
            </div>
          </div>
          <div class="card bg-gradient-to-br from-amber-50 to-white border border-amber-100">
            <div class="flex items-center justify-between">
              <div>
                <p class="text-xs text-gray-500 mb-1">{{ t('dashboard.estimatedValue') }}</p>
                <p class="text-2xl font-bold text-amber-700">{{ formatCurrency(dashboardStore.executiveDashboard.tenderStatistics.totalEstimatedValue) }}</p>
              </div>
              <div class="w-12 h-12 bg-amber-100 rounded-xl flex items-center justify-center">
                <svg class="w-6 h-6 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
            </div>
            <div class="mt-3 text-xs text-gray-500">{{ t('common.sar') }}</div>
          </div>
        </div>

        <!-- Workflow & User Stats -->
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-4">
          <div class="card">
            <h3 class="text-sm font-semibold text-gray-700 mb-4">{{ t('dashboard.workflowStats') }}</h3>
            <div class="grid grid-cols-2 gap-4">
              <div class="text-center p-3 bg-gray-50 rounded-lg">
                <p class="text-2xl font-bold text-gray-900">{{ dashboardStore.executiveDashboard.workflowStatistics.totalInstances }}</p>
                <p class="text-xs text-gray-500">{{ t('dashboard.total') }}</p>
              </div>
              <div class="text-center p-3 bg-blue-50 rounded-lg">
                <p class="text-2xl font-bold text-blue-600">{{ dashboardStore.executiveDashboard.workflowStatistics.activeCount }}</p>
                <p class="text-xs text-gray-500">{{ t('dashboard.active') }}</p>
              </div>
              <div class="text-center p-3 bg-green-50 rounded-lg">
                <p class="text-2xl font-bold text-green-600">{{ dashboardStore.executiveDashboard.workflowStatistics.completedCount }}</p>
                <p class="text-xs text-gray-500">{{ t('dashboard.completed') }}</p>
              </div>
              <div class="text-center p-3 bg-red-50 rounded-lg">
                <p class="text-2xl font-bold text-red-600">{{ dashboardStore.executiveDashboard.workflowStatistics.rejectedCount }}</p>
                <p class="text-xs text-gray-500">{{ t('dashboard.rejected') }}</p>
              </div>
            </div>
          </div>

          <div class="card">
            <h3 class="text-sm font-semibold text-gray-700 mb-4">{{ t('dashboard.userStats') }}</h3>
            <div class="grid grid-cols-3 gap-4">
              <div class="text-center p-3 bg-gray-50 rounded-lg">
                <p class="text-2xl font-bold text-gray-900">{{ dashboardStore.executiveDashboard.userStatistics.totalUsers }}</p>
                <p class="text-xs text-gray-500">{{ t('dashboard.total') }}</p>
              </div>
              <div class="text-center p-3 bg-green-50 rounded-lg">
                <p class="text-2xl font-bold text-green-600">{{ dashboardStore.executiveDashboard.userStatistics.activeUsers }}</p>
                <p class="text-xs text-gray-500">{{ t('dashboard.active') }}</p>
              </div>
              <div class="text-center p-3 bg-yellow-50 rounded-lg">
                <p class="text-2xl font-bold text-yellow-600">{{ dashboardStore.executiveDashboard.userStatistics.invitedUsers }}</p>
                <p class="text-xs text-gray-500">{{ t('dashboard.invited') }}</p>
              </div>
            </div>
          </div>
        </div>

        <!-- Evaluation Stats -->
        <div class="card" v-if="dashboardStore.executiveDashboard.evaluationStatistics">
          <h3 class="text-sm font-semibold text-gray-700 mb-4">{{ t('dashboard.evaluationStats') }}</h3>
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-4">
            <div class="text-center p-3 bg-gray-50 rounded-lg">
              <p class="text-2xl font-bold text-gray-900">{{ dashboardStore.executiveDashboard.evaluationStatistics.totalProposals }}</p>
              <p class="text-xs text-gray-500">{{ t('dashboard.totalProposals') }}</p>
            </div>
            <div class="text-center p-3 bg-green-50 rounded-lg">
              <p class="text-2xl font-bold text-green-600">{{ dashboardStore.executiveDashboard.evaluationStatistics.compliancePassedCount }}</p>
              <p class="text-xs text-gray-500">{{ t('dashboard.compliancePassed') }}</p>
            </div>
            <div class="text-center p-3 bg-blue-50 rounded-lg">
              <p class="text-2xl font-bold text-blue-600">{{ dashboardStore.executiveDashboard.evaluationStatistics.recommendedCount }}</p>
              <p class="text-xs text-gray-500">{{ t('dashboard.recommended') }}</p>
            </div>
            <div class="text-center p-3 bg-red-50 rounded-lg">
              <p class="text-2xl font-bold text-red-600">{{ dashboardStore.executiveDashboard.evaluationStatistics.excludedCount }}</p>
              <p class="text-xs text-gray-500">{{ t('dashboard.excluded') }}</p>
            </div>
          </div>
        </div>

        <!-- Tenders by Type -->
        <div class="card" v-if="dashboardStore.executiveDashboard.tenderStatistics.tendersByType?.length">
          <h3 class="text-sm font-semibold text-gray-700 mb-4">{{ t('dashboard.tendersByType') }}</h3>
          <div class="space-y-2">
            <div
              v-for="item in dashboardStore.executiveDashboard.tenderStatistics.tendersByType"
              :key="item.tenderType"
              class="flex items-center justify-between"
            >
              <span class="text-sm text-gray-600">{{ t(`tenders.type.${item.tenderType}`) }}</span>
              <div class="flex items-center gap-2">
                <div class="w-32 bg-gray-100 rounded-full h-2">
                  <div
                    class="bg-primary-500 h-2 rounded-full"
                    :style="{ width: `${(item.count / dashboardStore.executiveDashboard!.tenderStatistics.totalTenders) * 100}%` }"
                  ></div>
                </div>
                <span class="text-sm font-medium text-gray-900 w-8 text-end">{{ item.count }}</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Recent Activity -->
        <div class="card" v-if="dashboardStore.executiveDashboard.recentActivity?.length">
          <h3 class="text-sm font-semibold text-gray-700 mb-4">{{ t('dashboard.recentActivity') }}</h3>
          <div class="space-y-3">
            <div
              v-for="log in dashboardStore.executiveDashboard.recentActivity"
              :key="log.id"
              class="flex items-start gap-3 text-sm"
            >
              <div class="w-2 h-2 rounded-full bg-primary-400 mt-1.5 flex-shrink-0"></div>
              <div class="flex-1">
                <p class="text-gray-700">{{ log.actionDescription }}</p>
                <p class="text-xs text-gray-400">{{ log.userName }} - {{ formatDateTime(log.timestamp) }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- Operational Dashboard -->
    <template v-else-if="activeView === 'operational' && dashboardStore.operationalDashboard">
      <div class="space-y-6">
        <!-- Task Stats -->
        <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <div class="card text-center">
            <p class="text-xs text-gray-500">{{ t('dashboard.pendingTasks') }}</p>
            <p class="text-2xl font-bold text-yellow-600">{{ dashboardStore.operationalDashboard.taskDashboard.pendingCount }}</p>
          </div>
          <div class="card text-center">
            <p class="text-xs text-gray-500">{{ t('tasks.inProgress') }}</p>
            <p class="text-2xl font-bold text-blue-600">{{ dashboardStore.operationalDashboard.taskDashboard.inProgressCount }}</p>
          </div>
          <div class="card text-center">
            <p class="text-xs text-gray-500">{{ t('dashboard.completedTasks') }}</p>
            <p class="text-2xl font-bold text-green-600">{{ dashboardStore.operationalDashboard.taskDashboard.completedCount }}</p>
          </div>
          <div class="card text-center">
            <p class="text-xs text-gray-500">{{ t('dashboard.overdueTasks') }}</p>
            <p class="text-2xl font-bold text-red-600">{{ dashboardStore.operationalDashboard.taskDashboard.overdueCount }}</p>
          </div>
        </div>

        <!-- My Tenders -->
        <div class="card" v-if="dashboardStore.operationalDashboard.myTenders?.length">
          <h3 class="text-sm font-semibold text-gray-700 mb-4">{{ t('dashboard.myTenders') }}</h3>
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="text-start text-gray-500 border-b">
                  <th class="pb-3 font-medium">{{ t('tenders.title') }}</th>
                  <th class="pb-3 font-medium">{{ t('common.status') }}</th>
                  <th class="pb-3 font-medium">{{ t('tenders.completionPercentage') }}</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100">
                <tr v-for="tender in dashboardStore.operationalDashboard.myTenders" :key="tender.id" class="hover:bg-gray-50">
                  <td class="py-3 font-medium text-gray-900">
                    <router-link :to="`/tenders/${tender.id}`" class="hover:text-primary-600">
                      {{ getTitle(tender) }}
                    </router-link>
                  </td>
                  <td class="py-3">
                    <span class="text-xs px-2 py-0.5 rounded-full" :class="getStatusClass(tender.status)">
                      {{ t(`tenders.status.${tender.status}`) }}
                    </span>
                  </td>
                  <td class="py-3">
                    <div class="flex items-center gap-2">
                      <div class="w-20 bg-gray-100 rounded-full h-1.5">
                        <div class="bg-primary-500 h-1.5 rounded-full" :style="{ width: `${tender.completionPercentage}%` }"></div>
                      </div>
                      <span class="text-xs text-gray-500">{{ tender.completionPercentage }}%</span>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Upcoming Deadlines -->
        <div class="card" v-if="dashboardStore.operationalDashboard.taskDashboard.upcomingDeadlines?.length">
          <h3 class="text-sm font-semibold text-gray-700 mb-4">{{ t('dashboard.upcomingDeadlines') }}</h3>
          <div class="space-y-3">
            <div
              v-for="deadline in dashboardStore.operationalDashboard.taskDashboard.upcomingDeadlines"
              :key="deadline.taskId"
              class="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
            >
              <div>
                <p class="text-sm font-medium text-gray-900">{{ getTitle(deadline) }}</p>
                <p class="text-xs text-gray-500">{{ formatDate(deadline.dueDate) }}</p>
              </div>
              <span :class="getSlaClass(deadline.slaStatus)">{{ getSlaText(deadline.slaStatus) }}</span>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- Committee Dashboard -->
    <template v-else-if="activeView === 'committee' && dashboardStore.committeeDashboard">
      <div class="space-y-6">
        <!-- Active Committees -->
        <div class="card" v-if="dashboardStore.committeeDashboard.myCommittees?.length">
          <h3 class="text-sm font-semibold text-gray-700 mb-4">{{ t('dashboard.activeCommittees') }}</h3>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div
              v-for="committee in dashboardStore.committeeDashboard.myCommittees"
              :key="committee.committeeId"
              class="p-4 border rounded-lg"
            >
              <h4 class="font-semibold text-gray-900">{{ getTitle(committee) }}</h4>
              <div class="flex gap-4 mt-2 text-xs text-gray-500">
                <span>{{ committee.memberRole }}</span>
                <span>{{ committee.committeeType }}</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Pending Evaluations -->
        <div class="card" v-if="dashboardStore.committeeDashboard.pendingEvaluations?.length">
          <h3 class="text-sm font-semibold text-gray-700 mb-4">{{ t('dashboard.pendingEvaluations') }}</h3>
          <div class="space-y-3">
            <div
              v-for="evaluation in dashboardStore.committeeDashboard.pendingEvaluations"
              :key="evaluation.taskId"
              class="flex items-center justify-between p-3 bg-yellow-50 rounded-lg"
            >
              <div>
                <p class="text-sm font-medium text-gray-900">{{ getTitle(evaluation) }}</p>
                <p class="text-xs text-gray-500">{{ t('tasks.dueDate') }}: {{ formatDate(evaluation.dueDate) }}</p>
              </div>
              <span :class="getSlaClass(evaluation.slaStatus)">{{ getSlaText(evaluation.slaStatus) }}</span>
            </div>
          </div>
        </div>

        <!-- Pending Signatures -->
        <div class="card" v-if="dashboardStore.committeeDashboard.pendingSignatures?.length">
          <h3 class="text-sm font-semibold text-gray-700 mb-4">{{ t('dashboard.pendingSignatures') }}</h3>
          <div class="space-y-3">
            <div
              v-for="sig in dashboardStore.committeeDashboard.pendingSignatures"
              :key="sig.reportId"
              class="flex items-center justify-between p-3 bg-blue-50 rounded-lg"
            >
              <div>
                <p class="text-sm font-medium text-gray-900">{{ sig.reportType }} - {{ formatDate(sig.createdAt) }}</p>
              </div>
            </div>
          </div>
        </div>

        <!-- Empty State -->
        <div v-if="!dashboardStore.committeeDashboard.myCommittees?.length && !dashboardStore.committeeDashboard.pendingEvaluations?.length" class="text-center py-12 text-gray-400">
          <p class="text-lg font-medium">{{ t('dashboard.noCommitteeData') }}</p>
        </div>
      </div>
    </template>

    <!-- Monitoring Dashboard -->
    <template v-else-if="activeView === 'monitoring' && dashboardStore.monitoringDashboard">
      <div class="space-y-6">
        <!-- SLA Stats -->
        <div class="grid grid-cols-2 sm:grid-cols-5 gap-3">
          <div class="card text-center">
            <p class="text-xs text-gray-500">{{ t('dashboard.totalTracked') }}</p>
            <p class="text-2xl font-bold text-gray-900">{{ dashboardStore.monitoringDashboard.slaStatistics.totalTracked }}</p>
          </div>
          <div class="card text-center">
            <p class="text-xs text-gray-500">{{ t('tasks.sla.onTrack') }}</p>
            <p class="text-2xl font-bold text-green-600">{{ dashboardStore.monitoringDashboard.slaStatistics.onTrackCount }}</p>
          </div>
          <div class="card text-center">
            <p class="text-xs text-gray-500">{{ t('tasks.sla.atRisk') }}</p>
            <p class="text-2xl font-bold text-yellow-600">{{ dashboardStore.monitoringDashboard.slaStatistics.atRiskCount }}</p>
          </div>
          <div class="card text-center">
            <p class="text-xs text-gray-500">{{ t('tasks.sla.overdue') }}</p>
            <p class="text-2xl font-bold text-red-600">{{ dashboardStore.monitoringDashboard.slaStatistics.overdueCount }}</p>
          </div>
          <div class="card text-center">
            <p class="text-xs text-gray-500">{{ t('dashboard.complianceRate') }}</p>
            <p class="text-2xl font-bold" :class="dashboardStore.monitoringDashboard.slaStatistics.complianceRate >= 80 ? 'text-green-600' : 'text-red-600'">
              {{ dashboardStore.monitoringDashboard.slaStatistics.complianceRate.toFixed(1) }}%
            </p>
          </div>
        </div>

        <!-- Escalated Tasks -->
        <div class="card" v-if="dashboardStore.monitoringDashboard.escalatedTasks?.length">
          <h3 class="text-sm font-semibold text-red-700 mb-4">{{ t('dashboard.escalatedTasks') }}</h3>
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="text-start text-gray-500 border-b">
                  <th class="pb-3 font-medium">{{ t('tasks.myTasks') }}</th>
                  <th class="pb-3 font-medium">{{ t('tasks.dueDate') }}</th>
                  <th class="pb-3 font-medium">{{ t('dashboard.escalatedAt') }}</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100">
                <tr v-for="task in dashboardStore.monitoringDashboard.escalatedTasks" :key="task.taskId" class="hover:bg-red-50">
                  <td class="py-3 font-medium text-gray-900">{{ getTitle(task) }}</td>
                  <td class="py-3 text-gray-500">{{ formatDate(task.dueDate) }}</td>
                  <td class="py-3 text-red-600">{{ formatDateTime(task.escalatedAt) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Audit Categories -->
        <div class="card" v-if="dashboardStore.monitoringDashboard.auditCategories?.length">
          <h3 class="text-sm font-semibold text-gray-700 mb-4">{{ t('dashboard.auditCategories') }}</h3>
          <div class="space-y-2">
            <div
              v-for="cat in dashboardStore.monitoringDashboard.auditCategories"
              :key="cat.category"
              class="flex items-center justify-between"
            >
              <span class="text-sm text-gray-600">{{ cat.category }}</span>
              <span class="text-sm font-medium text-gray-900">{{ cat.count }}</span>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- Fallback: Recent Tasks (same as original dashboard) -->
    <div class="card" v-if="!dashboardStore.isLoading">
      <div class="flex items-center justify-between mb-4">
        <h2 class="text-lg font-semibold text-gray-900">{{ t('tasks.myTasks') }}</h2>
        <router-link to="/tasks" class="text-sm text-primary-500 hover:text-primary-600">
          {{ t('tasks.allTasks') }} &rarr;
        </router-link>
      </div>

      <div v-if="taskStore.isLoading" class="flex items-center justify-center py-8">
        <svg class="animate-spin w-8 h-8 text-primary-500" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
        </svg>
      </div>

      <div v-else-if="taskStore.tasks.length === 0" class="text-center py-8 text-gray-400">
        <p>{{ t('common.noData') }}</p>
      </div>

      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="text-start text-gray-500 border-b">
              <th class="pb-3 font-medium">{{ t('tasks.myTasks') }}</th>
              <th class="pb-3 font-medium">{{ t('common.status') }}</th>
              <th class="pb-3 font-medium">{{ t('tasks.priority') }}</th>
              <th class="pb-3 font-medium">SLA</th>
              <th class="pb-3 font-medium">{{ t('tasks.dueDate') }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-for="task in taskStore.tasks" :key="task.id" class="hover:bg-gray-50">
              <td class="py-3 font-medium text-gray-900">{{ getTitle(task) }}</td>
              <td class="py-3"><span class="badge-info">{{ task.status }}</span></td>
              <td class="py-3">
                <span :class="{
                  'badge-danger': task.priority === 'Critical' || task.priority === 'High',
                  'badge-warning': task.priority === 'Medium',
                  'badge-info': task.priority === 'Low',
                }">{{ t(`tasks.${task.priority.toLowerCase()}`) }}</span>
              </td>
              <td class="py-3"><span :class="getSlaClass(task.slaStatus)">{{ getSlaText(task.slaStatus) }}</span></td>
              <td class="py-3 text-gray-500">{{ formatDate(task.dueDate) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
