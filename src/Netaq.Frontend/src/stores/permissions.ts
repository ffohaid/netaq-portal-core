import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '../services/api'
import type { ApiResponse } from '../types'

export interface PermissionMatrix {
  id: string
  userId?: string
  userFullNameAr?: string
  userFullNameEn?: string
  userEmail?: string
  userRole: string
  tenderPhase: string
  canView: boolean
  canCreate: boolean
  canEdit: boolean
  canDelete: boolean
  canApprove: boolean
  canReject: boolean
  canDelegate: boolean
  canExport: boolean
}

export interface PermissionMatrixGroup {
  role: number
  roleName: string
  phases: PermissionMatrix[]
}

export interface UpdatePermissionEntry {
  userRole: string
  tenderPhase: string
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
  const groups = ref<PermissionMatrixGroup[]>([])
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const isSaving = ref(false)

  async function fetchMatrix() {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get<ApiResponse<PermissionMatrixGroup[]>>('/permissions')
      if (response.data.isSuccess && response.data.data) {
        groups.value = response.data.data
        // Flatten for easy lookup
        const flat: PermissionMatrix[] = []
        for (const group of response.data.data) {
          for (const phase of group.phases) {
            flat.push({
              ...phase,
              userRole: group.roleName,
              tenderPhase: phase.tenderPhase !== undefined ? String(phase.tenderPhase) : ''
            })
          }
        }
        matrix.value = flat
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to fetch permissions'
    } finally {
      isLoading.value = false
    }
  }

  async function bulkUpdate(entries: UpdatePermissionEntry[]) {
    isSaving.value = true
    error.value = null
    try {
      const response = await api.put<ApiResponse<void>>('/permissions', { entries })
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
    groups,
    isLoading,
    error,
    isSaving,
    fetchMatrix,
    bulkUpdate,
  }
})
