import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '../services/api'
import type { ApiResponse } from '../types'

export interface PermissionMatrix {
  id: string
  tenderPhase: string
  userRole: string
  canView: boolean
  canCreate: boolean
  canEdit: boolean
  canDelete: boolean
  canApprove: boolean
  canReject: boolean
  canDelegate: boolean
  canExport: boolean
}

export interface UpdatePermissionRequest {
  id: string
  canView: boolean
  canCreate: boolean
  canEdit: boolean
  canDelete: boolean
  canApprove: boolean
  canReject: boolean
  canDelegate: boolean
  canExport: boolean
}

export const usePermissionStore = defineStore('permissions', () => {
  const matrix = ref<PermissionMatrix[]>([])
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const isSaving = ref(false)

  async function fetchMatrix() {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get<ApiResponse<PermissionMatrix[]>>('/permissions')
      if (response.data.isSuccess && response.data.data) {
        matrix.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to fetch permissions'
    } finally {
      isLoading.value = false
    }
  }

  async function updatePermission(request: UpdatePermissionRequest) {
    isSaving.value = true
    error.value = null
    try {
      const response = await api.put<ApiResponse<void>>(`/permissions/${request.id}`, request)
      if (response.data.isSuccess) {
        const idx = matrix.value.findIndex(p => p.id === request.id)
        if (idx !== -1) {
          matrix.value[idx] = { ...matrix.value[idx], ...request }
        }
        return true
      }
      error.value = response.data.error || 'Failed to update permission'
      return false
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to update permission'
      return false
    } finally {
      isSaving.value = false
    }
  }

  async function bulkUpdate(requests: UpdatePermissionRequest[]) {
    isSaving.value = true
    error.value = null
    try {
      const response = await api.put<ApiResponse<void>>('/permissions/bulk', { permissions: requests })
      if (response.data.isSuccess) {
        await fetchMatrix()
        return true
      }
      error.value = response.data.error || 'Failed to update permissions'
      return false
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to update permissions'
      return false
    } finally {
      isSaving.value = false
    }
  }

  return {
    matrix,
    isLoading,
    error,
    isSaving,
    fetchMatrix,
    updatePermission,
    bulkUpdate,
  }
})
