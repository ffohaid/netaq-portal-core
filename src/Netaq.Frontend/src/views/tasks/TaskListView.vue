<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useTaskStore } from '../../stores/tasks'
import { getCurrentLocale } from '../../i18n'

const { t } = useI18n()
const router = useRouter()
const taskStore = useTaskStore()
const locale = computed(() => getCurrentLocale())

const statusFilter = ref<string>('')
const priorityFilter = ref<string>('')
const searchQuery = ref('')
const currentPage = ref(1)
const pageSize = 20

// Task detail modal
const showDetailModal = ref(false)
const selectedTask = ref<any>(null)
const detailLoading = ref(false)

// Action modal
const showActionModal = ref(false)
const actionType = ref<string>('')
const actionComments = ref('')
const actionTaskId = ref('')
const actionLoading = ref(false)

// Delegate modal
const showDelegateModal = ref(false)
const delegateUserId = ref('')
const delegateReason = ref('')
const delegateTaskId = ref('')

// Success/Error messages
const successMessage = ref('')
const errorMessage = ref('')

async function loadTasks() {
  await taskStore.fetchMyTasks({
    status: statusFilter.value || undefined,
    priority: priorityFilter.value || undefined,
    search: searchQuery.value || undefined,
    pageNumber: currentPage.value,
    pageSize,
  })
}

watch([statusFilter, priorityFilter], () => {
  currentPage.value = 1
  loadTasks()
})

let searchTimeout: any = null
watch(searchQuery, () => {
  clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    currentPage.value = 1
    loadTasks()
  }, 400)
})

onMounted(() => {
  loadTasks()
  taskStore.fetchStatistics()
})

function getTaskTitle(task: any) {
  return locale.value === 'ar' ? (task.titleAr || task.title) : (task.titleEn || task.title)
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
    case 'Pending': return 'bg-yellow-100 text-yellow-700'
    case 'InProgress': return 'bg-blue-100 text-blue-700'
    case 'Completed': return 'bg-green-100 text-green-700'
    case 'Rejected': return 'bg-red-100 text-red-700'
    case 'Delegated': return 'bg-purple-100 text-purple-700'
    case 'Returned': return 'bg-orange-100 text-orange-700'
    default: return 'bg-gray-100 text-gray-700'
  }
}

function getStatusText(status: string) {
  switch (status) {
    case 'Pending': return t('tasks.pending')
    case 'InProgress': return t('tasks.inProgress')
    case 'Completed': return t('tasks.completed')
    case 'Rejected': return t('workflow.rejected')
    case 'Delegated': return t('tasks.delegated') || 'Delegated'
    case 'Returned': return t('tasks.returned') || 'Returned'
    default: return status
  }
}

async function viewTaskDetail(task: any) {
  showDetailModal.value = true
  detailLoading.value = true
  selectedTask.value = task
  try {
    const detail = await taskStore.fetchTaskDetail(task.id)
    if (detail) {
      selectedTask.value = detail
    }
  } finally {
    detailLoading.value = false
  }
}

function openActionModal(taskId: string, action: string) {
  actionTaskId.value = taskId
  actionType.value = action
  actionComments.value = ''
  showActionModal.value = true
}

async function submitAction() {
  actionLoading.value = true
  errorMessage.value = ''
  try {
    const success = await taskStore.takeAction(actionTaskId.value, actionType.value, actionComments.value)
    if (success) {
      showActionModal.value = false
      showDetailModal.value = false
      successMessage.value = t('tasks.actionSuccess') || 'Action completed successfully'
      setTimeout(() => { successMessage.value = '' }, 3000)
    } else {
      errorMessage.value = taskStore.error || 'Action failed'
    }
  } finally {
    actionLoading.value = false
  }
}

function openDelegateModal(taskId: string) {
  delegateTaskId.value = taskId
  delegateUserId.value = ''
  delegateReason.value = ''
  showDelegateModal.value = true
}

async function submitDelegate() {
  if (!delegateUserId.value) return
  actionLoading.value = true
  errorMessage.value = ''
  try {
    const success = await taskStore.delegateTask(delegateTaskId.value, delegateUserId.value, delegateReason.value)
    if (success) {
      showDelegateModal.value = false
      showDetailModal.value = false
      successMessage.value = t('tasks.delegateSuccess') || 'Task delegated successfully'
      setTimeout(() => { successMessage.value = '' }, 3000)
    } else {
      errorMessage.value = taskStore.error || 'Delegation failed'
    }
  } finally {
    actionLoading.value = false
  }
}

function navigateToEntity(task: any) {
  if (task.entityType === 'Tender' && task.entityId) {
    router.push(`/tenders/${task.entityId}`)
  } else if (task.entityType === 'Inquiry' && task.entityId) {
    router.push(`/inquiries`)
  }
}

const totalPages = computed(() => Math.ceil(taskStore.totalCount / pageSize))
const canTakeAction = (task: any) => task.status === 'Pending' || task.status === 'InProgress'
</script>

<template>
  <div class="space-y-6">
    <!-- Success Message -->
    <div v-if="successMessage" class="bg-green-50 border border-green-200 rounded-lg p-4 flex items-center gap-3">
      <svg class="w-5 h-5 text-green-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
      <p class="text-sm text-green-700">{{ successMessage }}</p>
    </div>

    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ t('tasks.title') }}</h1>
        <p class="text-gray-500 mt-1">{{ t('tasks.myTasks') }}</p>
      </div>
    </div>

    <!-- Stats Summary -->
    <div v-if="taskStore.statistics" class="grid grid-cols-2 sm:grid-cols-4 gap-3">
      <div class="bg-yellow-50 rounded-xl p-4 text-center cursor-pointer hover:bg-yellow-100 transition-colors" @click="statusFilter = 'Pending'">
        <p class="text-2xl font-bold text-yellow-700">{{ taskStore.statistics.pendingTasks }}</p>
        <p class="text-xs text-yellow-600 mt-1">{{ t('tasks.pending') }}</p>
      </div>
      <div class="bg-green-50 rounded-xl p-4 text-center cursor-pointer hover:bg-green-100 transition-colors" @click="statusFilter = 'Completed'">
        <p class="text-2xl font-bold text-green-700">{{ taskStore.statistics.completedTasks }}</p>
        <p class="text-xs text-green-600 mt-1">{{ t('tasks.completed') }}</p>
      </div>
      <div class="bg-red-50 rounded-xl p-4 text-center cursor-pointer hover:bg-red-100 transition-colors" @click="statusFilter = 'Overdue'">
        <p class="text-2xl font-bold text-red-700">{{ taskStore.statistics.overdueTasks }}</p>
        <p class="text-xs text-red-600 mt-1">{{ t('tasks.overdue') }}</p>
      </div>
      <div class="bg-blue-50 rounded-xl p-4 text-center cursor-pointer hover:bg-blue-100 transition-colors" @click="statusFilter = ''">
        <p class="text-2xl font-bold text-blue-700">{{ taskStore.statistics.totalTasks }}</p>
        <p class="text-xs text-blue-600 mt-1">{{ t('dashboard.totalTasks') }}</p>
      </div>
    </div>

    <!-- Filters -->
    <div class="card">
      <div class="flex flex-wrap gap-4">
        <div class="relative flex-1 min-w-[200px]">
          <svg class="absolute start-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input
            v-model="searchQuery"
            type="text"
            :placeholder="t('common.search') + '...'"
            class="input-field ps-10"
          />
        </div>
        <select v-model="statusFilter" class="input-field w-auto min-w-[160px]">
          <option value="">{{ t('common.status') }}: {{ t('common.all') || 'All' }}</option>
          <option value="Pending">{{ t('tasks.pending') }}</option>
          <option value="InProgress">{{ t('tasks.inProgress') }}</option>
          <option value="Completed">{{ t('tasks.completed') }}</option>
          <option value="Rejected">{{ t('workflow.rejected') }}</option>
        </select>
        <select v-model="priorityFilter" class="input-field w-auto min-w-[160px]">
          <option value="">{{ t('tasks.priority') }}: {{ t('common.all') || 'All' }}</option>
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
              <button @click="viewTaskDetail(task)" class="text-start">
                <p class="text-sm font-medium text-primary-600 hover:text-primary-700">{{ getTaskTitle(task) }}</p>
                <p class="text-xs text-gray-400 mt-0.5" v-if="task.entityType">{{ task.entityType }}</p>
              </button>
            </td>
            <td class="px-6 py-4">
              <span class="px-2 py-1 text-xs font-medium rounded-full" :class="getStatusClass(task.status)">
                {{ getStatusText(task.status) }}
              </span>
            </td>
            <td class="px-6 py-4">
              <span :class="{
                'badge-danger': task.priority === 'Critical' || task.priority === 'High',
                'badge-warning': task.priority === 'Medium',
                'badge-info': task.priority === 'Low',
              }">{{ t(`tasks.${task.priority?.toLowerCase()}`) }}</span>
            </td>
            <td class="px-6 py-4">
              <span :class="getSlaClass(task.slaStatus)">{{ getSlaText(task.slaStatus) }}</span>
            </td>
            <td class="px-6 py-4 text-sm text-gray-500">{{ formatDate(task.dueDate) }}</td>
            <td class="px-6 py-4">
              <div class="flex items-center gap-2">
                <!-- View Detail -->
                <button @click="viewTaskDetail(task)" class="p-1.5 text-gray-400 hover:text-primary-500 rounded-lg hover:bg-gray-100" :title="t('common.details')">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                  </svg>
                </button>
                <!-- Approve -->
                <button v-if="canTakeAction(task)" @click="openActionModal(task.id, 'Approve')" class="p-1.5 text-green-500 hover:text-green-600 rounded-lg hover:bg-green-50" :title="t('workflow.approve')">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                </button>
                <!-- Reject -->
                <button v-if="canTakeAction(task)" @click="openActionModal(task.id, 'Reject')" class="p-1.5 text-red-500 hover:text-red-600 rounded-lg hover:bg-red-50" :title="t('workflow.reject')">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
                <!-- Delegate -->
                <button v-if="canTakeAction(task)" @click="openDelegateModal(task.id)" class="p-1.5 text-purple-500 hover:text-purple-600 rounded-lg hover:bg-purple-50" :title="t('tasks.delegate') || 'Delegate'">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z" />
                  </svg>
                </button>
                <!-- Navigate to Entity -->
                <button v-if="task.entityId" @click="navigateToEntity(task)" class="p-1.5 text-blue-500 hover:text-blue-600 rounded-lg hover:bg-blue-50" :title="t('common.open') || 'Open'">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                  </svg>
                </button>
              </div>
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

    <!-- Task Detail Modal -->
    <Teleport to="body">
      <div v-if="showDetailModal" class="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div class="fixed inset-0 bg-black/50" @click="showDetailModal = false"></div>
        <div class="relative bg-white rounded-xl shadow-2xl w-full max-w-2xl max-h-[85vh] overflow-y-auto">
          <!-- Modal Header -->
          <div class="sticky top-0 bg-white border-b px-6 py-4 flex items-center justify-between rounded-t-xl">
            <h3 class="text-lg font-bold text-gray-900">{{ t('common.details') }}</h3>
            <button @click="showDetailModal = false" class="p-1 text-gray-400 hover:text-gray-600 rounded">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <!-- Modal Body -->
          <div class="p-6 space-y-4" v-if="selectedTask">
            <div v-if="detailLoading" class="flex items-center justify-center py-8">
              <svg class="animate-spin w-8 h-8 text-primary-500" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
            </div>
            <template v-else>
              <div class="grid grid-cols-2 gap-4">
                <div>
                  <p class="text-xs text-gray-500 mb-1">{{ t('tasks.myTasks') }}</p>
                  <p class="font-semibold text-gray-900">{{ getTaskTitle(selectedTask) }}</p>
                </div>
                <div>
                  <p class="text-xs text-gray-500 mb-1">{{ t('common.status') }}</p>
                  <span class="px-2 py-1 text-xs font-medium rounded-full" :class="getStatusClass(selectedTask.status)">
                    {{ getStatusText(selectedTask.status) }}
                  </span>
                </div>
                <div>
                  <p class="text-xs text-gray-500 mb-1">{{ t('tasks.priority') }}</p>
                  <span :class="{
                    'badge-danger': selectedTask.priority === 'Critical' || selectedTask.priority === 'High',
                    'badge-warning': selectedTask.priority === 'Medium',
                    'badge-info': selectedTask.priority === 'Low',
                  }">{{ t(`tasks.${selectedTask.priority?.toLowerCase()}`) }}</span>
                </div>
                <div>
                  <p class="text-xs text-gray-500 mb-1">SLA</p>
                  <span :class="getSlaClass(selectedTask.slaStatus)">{{ getSlaText(selectedTask.slaStatus) }}</span>
                </div>
                <div>
                  <p class="text-xs text-gray-500 mb-1">{{ t('tasks.dueDate') }}</p>
                  <p class="text-sm text-gray-700">{{ formatDateTime(selectedTask.dueDate) }}</p>
                </div>
                <div v-if="selectedTask.entityType">
                  <p class="text-xs text-gray-500 mb-1">{{ t('tasks.entityType') || 'Entity Type' }}</p>
                  <p class="text-sm text-gray-700">{{ selectedTask.entityType }}</p>
                </div>
                <div v-if="selectedTask.createdAt" class="col-span-2">
                  <p class="text-xs text-gray-500 mb-1">{{ t('common.createdAt') || 'Created At' }}</p>
                  <p class="text-sm text-gray-700">{{ formatDateTime(selectedTask.createdAt) }}</p>
                </div>
              </div>

              <!-- Description -->
              <div v-if="selectedTask.description">
                <p class="text-xs text-gray-500 mb-1">{{ t('common.description') || 'Description' }}</p>
                <p class="text-sm text-gray-700 bg-gray-50 p-3 rounded-lg">{{ selectedTask.description }}</p>
              </div>

              <!-- Action Buttons -->
              <div v-if="canTakeAction(selectedTask)" class="flex flex-wrap gap-3 pt-4 border-t">
                <button @click="openActionModal(selectedTask.id, 'Approve')" class="btn-primary flex items-center gap-2">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                  {{ t('workflow.approve') }}
                </button>
                <button @click="openActionModal(selectedTask.id, 'Reject')" class="btn-secondary text-red-600 border-red-200 hover:bg-red-50 flex items-center gap-2">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                  {{ t('workflow.reject') }}
                </button>
                <button @click="openActionModal(selectedTask.id, 'Return')" class="btn-secondary text-orange-600 border-orange-200 hover:bg-orange-50 flex items-center gap-2">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h10a8 8 0 018 8v2M3 10l6 6m-6-6l6-6" />
                  </svg>
                  {{ t('workflow.return') || 'Return' }}
                </button>
                <button @click="openDelegateModal(selectedTask.id)" class="btn-secondary text-purple-600 border-purple-200 hover:bg-purple-50 flex items-center gap-2">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z" />
                  </svg>
                  {{ t('tasks.delegate') || 'Delegate' }}
                </button>
                <button v-if="selectedTask.entityId" @click="navigateToEntity(selectedTask); showDetailModal = false" class="btn-secondary flex items-center gap-2">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                  </svg>
                  {{ t('common.open') || 'Open Entity' }}
                </button>
              </div>
            </template>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Action Confirmation Modal -->
    <Teleport to="body">
      <div v-if="showActionModal" class="fixed inset-0 z-[60] flex items-center justify-center p-4">
        <div class="fixed inset-0 bg-black/50" @click="showActionModal = false"></div>
        <div class="relative bg-white rounded-xl shadow-2xl w-full max-w-md">
          <div class="p-6 space-y-4">
            <h3 class="text-lg font-bold text-gray-900">
              {{ actionType === 'Approve' ? t('workflow.approve') : actionType === 'Reject' ? t('workflow.reject') : t('workflow.return') || 'Return' }}
            </h3>
            <p class="text-sm text-gray-500">
              {{ t('tasks.confirmAction') || 'Are you sure you want to perform this action?' }}
            </p>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('workflow.comments') || 'Comments' }}</label>
              <textarea
                v-model="actionComments"
                rows="3"
                class="input-field"
                :placeholder="t('workflow.addComments') || 'Add comments (optional)...'"
                :required="actionType === 'Reject'"
              ></textarea>
            </div>
            <div v-if="errorMessage" class="text-sm text-red-600 bg-red-50 p-2 rounded">{{ errorMessage }}</div>
            <div class="flex justify-end gap-3 pt-2">
              <button @click="showActionModal = false" class="btn-secondary">{{ t('common.cancel') }}</button>
              <button
                @click="submitAction"
                :disabled="actionLoading || (actionType === 'Reject' && !actionComments)"
                class="px-4 py-2 rounded-lg text-white font-medium"
                :class="{
                  'bg-green-600 hover:bg-green-700': actionType === 'Approve',
                  'bg-red-600 hover:bg-red-700': actionType === 'Reject',
                  'bg-orange-600 hover:bg-orange-700': actionType === 'Return',
                }"
              >
                <svg v-if="actionLoading" class="animate-spin w-4 h-4 inline me-2" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
                {{ t('common.confirm') || 'Confirm' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Delegate Modal -->
    <Teleport to="body">
      <div v-if="showDelegateModal" class="fixed inset-0 z-[60] flex items-center justify-center p-4">
        <div class="fixed inset-0 bg-black/50" @click="showDelegateModal = false"></div>
        <div class="relative bg-white rounded-xl shadow-2xl w-full max-w-md">
          <div class="p-6 space-y-4">
            <h3 class="text-lg font-bold text-gray-900">{{ t('tasks.delegate') || 'Delegate Task' }}</h3>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('tasks.delegateTo') || 'Delegate To (User ID)' }}</label>
              <input
                v-model="delegateUserId"
                type="text"
                class="input-field"
                :placeholder="t('tasks.enterUserId') || 'Enter user ID...'"
                required
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('tasks.delegateReason') || 'Reason' }}</label>
              <textarea
                v-model="delegateReason"
                rows="3"
                class="input-field"
                :placeholder="t('tasks.enterReason') || 'Enter reason for delegation...'"
              ></textarea>
            </div>
            <div v-if="errorMessage" class="text-sm text-red-600 bg-red-50 p-2 rounded">{{ errorMessage }}</div>
            <div class="flex justify-end gap-3 pt-2">
              <button @click="showDelegateModal = false" class="btn-secondary">{{ t('common.cancel') }}</button>
              <button
                @click="submitDelegate"
                :disabled="actionLoading || !delegateUserId"
                class="btn-primary"
              >
                <svg v-if="actionLoading" class="animate-spin w-4 h-4 inline me-2" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
                {{ t('tasks.delegate') || 'Delegate' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
