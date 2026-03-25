import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '../services/api'
import type { UserTask, TaskStatistics, PaginatedResponse, ApiResponse } from '../types'

export const useTaskStore = defineStore('tasks', () => {
  const tasks = ref<UserTask[]>([])
  const currentTask = ref<UserTask | null>(null)
  const statistics = ref<TaskStatistics | null>(null)
  const totalCount = ref(0)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  async function fetchMyTasks(params?: {
    status?: string
    priority?: string
    entityType?: string
    search?: string
    pageNumber?: number
    pageSize?: number
  }) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get<ApiResponse<PaginatedResponse<UserTask>>>('/task/my-tasks', { params })
      if (response.data.isSuccess && response.data.data) {
        tasks.value = response.data.data.items
        totalCount.value = response.data.data.totalCount
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to fetch tasks'
    } finally {
      isLoading.value = false
    }
  }

  async function fetchTaskDetail(taskId: string) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get<ApiResponse<UserTask>>(`/task/${taskId}`)
      if (response.data.isSuccess && response.data.data) {
        currentTask.value = response.data.data
        return response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to fetch task detail'
    } finally {
      isLoading.value = false
    }
    return null
  }

  async function fetchStatistics() {
    try {
      const response = await api.get<ApiResponse<TaskStatistics>>('/task/statistics')
      if (response.data.isSuccess && response.data.data) {
        statistics.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to fetch statistics'
    }
  }

  async function takeAction(taskId: string, action: string, comments?: string) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.post<ApiResponse<any>>(`/task/${taskId}/action`, {
        action,
        comments: comments || ''
      })
      if (response.data.isSuccess) {
        await fetchMyTasks()
        await fetchStatistics()
        return true
      } else {
        error.value = response.data.error || 'Action failed'
        return false
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to perform action'
      return false
    } finally {
      isLoading.value = false
    }
  }

  async function delegateTask(taskId: string, delegateToUserId: string, reason?: string) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.post<ApiResponse<any>>(`/task/${taskId}/delegate`, {
        delegateToUserId,
        reason: reason || ''
      })
      if (response.data.isSuccess) {
        await fetchMyTasks()
        await fetchStatistics()
        return true
      } else {
        error.value = response.data.error || 'Delegation failed'
        return false
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to delegate task'
      return false
    } finally {
      isLoading.value = false
    }
  }

  return {
    tasks,
    currentTask,
    statistics,
    totalCount,
    isLoading,
    error,
    fetchMyTasks,
    fetchTaskDetail,
    fetchStatistics,
    takeAction,
    delegateTask,
  }
})
