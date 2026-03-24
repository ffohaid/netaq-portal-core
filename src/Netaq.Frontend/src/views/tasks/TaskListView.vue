<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useTaskStore } from '../../stores/tasks'
import { getCurrentLocale } from '../../i18n'

const { t } = useI18n()
const taskStore = useTaskStore()
const locale = computed(() => getCurrentLocale())

const statusFilter = ref<string>('')
const priorityFilter = ref<string>('')
const currentPage = ref(1)
const pageSize = 20

async function loadTasks() {
  await taskStore.fetchMyTasks({
    status: statusFilter.value || undefined,
    priority: priorityFilter.value || undefined,
    pageNumber: currentPage.value,
    pageSize,
  })
}

watch([statusFilter, priorityFilter], () => {
  currentPage.value = 1
  loadTasks()
})

onMounted(() => {
  loadTasks()
  taskStore.fetchStatistics()
})

function getTaskTitle(task: any) {
  return locale.value === 'ar' ? task.titleAr : task.titleEn
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString(locale.value === 'ar' ? 'ar-SA' : 'en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
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

const totalPages = computed(() => Math.ceil(taskStore.totalCount / pageSize))
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ t('tasks.title') }}</h1>
        <p class="text-gray-500 mt-1">{{ t('tasks.myTasks') }}</p>
      </div>
    </div>

    <!-- Stats Summary -->
    <div v-if="taskStore.statistics" class="grid grid-cols-2 sm:grid-cols-4 gap-3">
      <div class="bg-yellow-50 rounded-xl p-4 text-center">
        <p class="text-2xl font-bold text-yellow-700">{{ taskStore.statistics.pendingTasks }}</p>
        <p class="text-xs text-yellow-600 mt-1">{{ t('tasks.pending') }}</p>
      </div>
      <div class="bg-green-50 rounded-xl p-4 text-center">
        <p class="text-2xl font-bold text-green-700">{{ taskStore.statistics.completedTasks }}</p>
        <p class="text-xs text-green-600 mt-1">{{ t('tasks.completed') }}</p>
      </div>
      <div class="bg-red-50 rounded-xl p-4 text-center">
        <p class="text-2xl font-bold text-red-700">{{ taskStore.statistics.overdueTasks }}</p>
        <p class="text-xs text-red-600 mt-1">{{ t('tasks.overdue') }}</p>
      </div>
      <div class="bg-blue-50 rounded-xl p-4 text-center">
        <p class="text-2xl font-bold text-blue-700">{{ taskStore.statistics.totalTasks }}</p>
        <p class="text-xs text-blue-600 mt-1">{{ t('dashboard.totalTasks') }}</p>
      </div>
    </div>

    <!-- Filters -->
    <div class="card">
      <div class="flex flex-wrap gap-4">
        <select v-model="statusFilter" class="input-field w-auto min-w-[160px]">
          <option value="">{{ t('common.status') }}: {{ t('common.filter') }}</option>
          <option value="Pending">{{ t('tasks.pending') }}</option>
          <option value="InProgress">{{ t('tasks.inProgress') }}</option>
          <option value="Completed">{{ t('tasks.completed') }}</option>
          <option value="Rejected">{{ t('workflow.rejected') }}</option>
        </select>

        <select v-model="priorityFilter" class="input-field w-auto min-w-[160px]">
          <option value="">{{ t('tasks.priority') }}: {{ t('common.filter') }}</option>
          <option value="Low">{{ t('tasks.low') }}</option>
          <option value="Medium">{{ t('tasks.medium') }}</option>
          <option value="High">{{ t('tasks.high') }}</option>
          <option value="Critical">{{ t('tasks.critical') }}</option>
        </select>
      </div>
    </div>

    <!-- Task Table -->
    <div class="card p-0 overflow-hidden">
      <div v-if="taskStore.isLoading" class="flex items-center justify-center py-12">
        <svg class="animate-spin w-8 h-8 text-primary-500" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
        </svg>
      </div>

      <div v-else-if="taskStore.tasks.length === 0" class="text-center py-12 text-gray-400">
        <svg class="w-16 h-16 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
            d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
        </svg>
        <p class="text-lg">{{ t('common.noData') }}</p>
      </div>

      <table v-else class="w-full">
        <thead class="bg-gray-50">
          <tr class="text-start text-sm text-gray-500">
            <th class="px-6 py-3 font-medium">{{ t('tasks.myTasks') }}</th>
            <th class="px-6 py-3 font-medium">{{ t('common.status') }}</th>
            <th class="px-6 py-3 font-medium">{{ t('tasks.priority') }}</th>
            <th class="px-6 py-3 font-medium">SLA</th>
            <th class="px-6 py-3 font-medium">{{ t('tasks.dueDate') }}</th>
            <th class="px-6 py-3 font-medium">{{ t('common.actions') }}</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-100">
          <tr v-for="task in taskStore.tasks" :key="task.id" class="hover:bg-gray-50">
            <td class="px-6 py-4">
              <p class="text-sm font-medium text-gray-900">{{ getTaskTitle(task) }}</p>
            </td>
            <td class="px-6 py-4">
              <span class="badge-info">{{ task.status }}</span>
            </td>
            <td class="px-6 py-4">
              <span :class="{
                'badge-danger': task.priority === 'Critical' || task.priority === 'High',
                'badge-warning': task.priority === 'Medium',
                'badge-info': task.priority === 'Low',
              }">{{ t(`tasks.${task.priority.toLowerCase()}`) }}</span>
            </td>
            <td class="px-6 py-4">
              <span :class="getSlaClass(task.slaStatus)">{{ getSlaText(task.slaStatus) }}</span>
            </td>
            <td class="px-6 py-4 text-sm text-gray-500">{{ formatDate(task.dueDate) }}</td>
            <td class="px-6 py-4">
              <button class="text-primary-500 hover:text-primary-600 text-sm font-medium">
                {{ t('common.details') }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>

      <!-- Pagination -->
      <div v-if="totalPages > 1" class="flex items-center justify-between px-6 py-3 border-t">
        <button
          @click="currentPage--; loadTasks()"
          :disabled="currentPage <= 1"
          class="btn-secondary text-sm"
        >
          {{ t('common.previous') }}
        </button>
        <span class="text-sm text-gray-500">{{ currentPage }} / {{ totalPages }}</span>
        <button
          @click="currentPage++; loadTasks()"
          :disabled="currentPage >= totalPages"
          class="btn-secondary text-sm"
        >
          {{ t('common.next') }}
        </button>
      </div>
    </div>
  </div>
</template>
