import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '../services/api'
import type { WorkflowTemplate, WorkflowTemplateDetail, ApiResponse } from '../types'

export const useWorkflowStore = defineStore('workflow', () => {
  const templates = ref<WorkflowTemplate[]>([])
  const currentTemplate = ref<WorkflowTemplateDetail | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  async function fetchTemplates() {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get<ApiResponse<WorkflowTemplate[]>>('/workflow/templates')
      if (response.data.isSuccess && response.data.data) {
        templates.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to fetch templates'
    } finally {
      isLoading.value = false
    }
  }

  async function fetchTemplateDetail(id: string) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get<ApiResponse<WorkflowTemplateDetail>>(`/workflow/templates/${id}`)
      if (response.data.isSuccess && response.data.data) {
        currentTemplate.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to fetch template detail'
    } finally {
      isLoading.value = false
    }
  }

  async function createTemplate(data: {
    nameAr: string
    nameEn: string
    descriptionAr?: string
    descriptionEn?: string
    steps: Array<{
      nameAr: string
      nameEn: string
      order: number
      stepType: string
      requiredRole: string
      slaDurationHours: number
    }>
  }) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.post<ApiResponse<string>>('/workflow/templates', data)
      if (response.data.isSuccess) {
        await fetchTemplates()
        return response.data.data
      } else {
        error.value = response.data.error || 'Failed to create template'
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to create template'
    } finally {
      isLoading.value = false
    }
    return null
  }

  async function startWorkflow(data: {
    workflowTemplateId: string
    entityId: string
    entityType: string
  }) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.post<ApiResponse<string>>('/workflow/instances', data)
      if (response.data.isSuccess) {
        return response.data.data
      } else {
        error.value = response.data.error || 'Failed to start workflow'
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to start workflow'
    } finally {
      isLoading.value = false
    }
    return null
  }

  async function takeAction(data: {
    workflowInstanceId: string
    actionType: string
    justification?: string
    delegatedToUserId?: string
    notes?: string
  }) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.post<ApiResponse<boolean>>('/workflow/actions', data)
      if (response.data.isSuccess) {
        return true
      } else {
        error.value = response.data.error || 'Failed to take action'
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to take action'
    } finally {
      isLoading.value = false
    }
    return false
  }

  return {
    templates,
    currentTemplate,
    isLoading,
    error,
    fetchTemplates,
    fetchTemplateDetail,
    createTemplate,
    startWorkflow,
    takeAction,
  }
})
