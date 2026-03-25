<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '../../stores/auth'
import { useTaskStore } from '../../stores/tasks'
import { getCurrentLocale } from '../../i18n'

const { t } = useI18n()
const authStore = useAuthStore()
const taskStore = useTaskStore()

const locale = computed(() => getCurrentLocale())

onMounted(async () => {
  await taskStore.fetchStatistics()
  await taskStore.fetchMyTasks({ pageSize: 5 })
})

const statsCards = computed(() => [
  {
    title: t('dashboard.pendingTasks'),
    value: taskStore.statistics?.pendingTasks ?? 0,
    icon: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z',
    color: 'bg-yellow-50 text-yellow-600',
    iconBg: 'bg-yellow-100',
  },
  {
    title: t('dashboard.completedTasks'),
    value: taskStore.statistics?.completedTasks ?? 0,
    icon: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z',
    color: 'bg-green-50 text-green-600',
    iconBg: 'bg-green-100',
  },
  {
    title: t('dashboard.overdueTasks'),
    value: taskStore.statistics?.overdueTasks ?? 0,
    icon: 'M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z',
    color: 'bg-red-50 text-red-600',
    iconBg: 'bg-red-100',
  },
  {
    title: t('dashboard.totalTasks'),
    value: taskStore.statistics?.totalTasks ?? 0,
    icon: 'M4 6h16M4 10h16M4 14h16M4 18h16',
    color: 'bg-blue-50 text-blue-600',
    iconBg: 'bg-blue-100',
  },
])

function getSlaStatusClass(status: string) {
  switch (status) {
    case 'OnTrack': return 'badge-success'
    case 'AtRisk': return 'badge-warning'
    case 'Overdue': return 'badge-danger'
    default: return 'badge-info'
  }
}

function getSlaStatusText(status: string) {
  switch (status) {
    case 'OnTrack': return t('tasks.sla.onTrack')
    case 'AtRisk': return t('tasks.sla.atRisk')
    case 'Overdue': return t('tasks.sla.overdue')
    default: return status
  }
}

function getTaskTitle(task: any) {
  return locale.value === 'ar' ? task.titleAr : task.titleEn
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}
</script>

<template>
  <div class="space-y-6">
    <!-- Welcome -->
    <div>
      <h1 class="text-2xl font-bold text-gray-900">
        {{ t('dashboard.welcome') }}{{ authStore.userDisplayName ? `, ${authStore.userDisplayName}` : '' }}
      </h1>
      <p class="text-gray-500 mt-1">{{ t('dashboard.title') }}</p>
    </div>

    <!-- Stats Cards -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <div
        v-for="(card, index) in statsCards"
        :key="index"
        class="card flex items-center gap-4"
      >
        <div :class="[card.iconBg, 'p-3 rounded-xl']">
          <svg class="w-6 h-6" :class="card.color.split(' ')[1]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="card.icon" />
          </svg>
        </div>
        <div>
          <p class="text-sm text-gray-500">{{ card.title }}</p>
          <p class="text-2xl font-bold text-gray-900">{{ card.value }}</p>
        </div>
      </div>
    </div>

    <!-- Recent Tasks -->
    <div class="card">
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
        <svg class="w-12 h-12 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
        </svg>
        <p>{{ t('common.noData') }}</p>
      </div>

      <div v-else class="overflow-x-auto">
        <table class="w-full">
          <thead>
            <tr class="text-start text-sm text-gray-500 border-b">
              <th class="pb-3 font-medium">{{ t('tasks.myTasks') }}</th>
              <th class="pb-3 font-medium">{{ t('common.status') }}</th>
              <th class="pb-3 font-medium">{{ t('tasks.priority') }}</th>
              <th class="pb-3 font-medium">SLA</th>
              <th class="pb-3 font-medium">{{ t('tasks.dueDate') }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-for="task in taskStore.tasks" :key="task.id" class="hover:bg-gray-50">
              <td class="py-3 text-sm font-medium text-gray-900">{{ getTaskTitle(task) }}</td>
              <td class="py-3">
                <span class="badge-info">{{ task.status }}</span>
              </td>
              <td class="py-3">
                <span :class="{
                  'badge-danger': task.priority === 'Critical' || task.priority === 'High',
                  'badge-warning': task.priority === 'Medium',
                  'badge-info': task.priority === 'Low',
                }">{{ t(`tasks.${task.priority.toLowerCase()}`) }}</span>
              </td>
              <td class="py-3">
                <span :class="getSlaStatusClass(task.slaStatus)">{{ getSlaStatusText(task.slaStatus) }}</span>
              </td>
              <td class="py-3 text-sm text-gray-500">{{ formatDate(task.dueDate) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
