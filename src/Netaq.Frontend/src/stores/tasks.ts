import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '../services/api'
import type { UserTask, TaskStatistics, PaginatedResponse, ApiResponse } from '../types'

export const useTaskStore = defineStore('tasks', () => {
  const tasks = ref<UserTask[]>([])
  const statistics = ref<TaskStatistics | null>(null)
  const totalCount = ref(0)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  async function fetchMyTasks(params?: {
    status?: string
    priority?: string
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

  return {
    tasks,
    statistics,
    totalCount,
    isLoading,
    error,
    fetchMyTasks,
    fetchStatistics,
  }
})
