import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '../services/api'
import type { ApiResponse, PaginatedResponse } from '../types'

export interface CommitteeMember {
  id: string
  userId: string
  fullNameAr: string
  fullNameEn: string
  email: string
  role: string
  isActive: boolean
  joinedAt: string
}

export interface Committee {
  id: string
  nameAr: string
  nameEn: string
  type: 'Permanent' | 'AdHoc'
  purposeAr?: string
  purposeEn?: string
  isActive: boolean
  memberCount: number
  formedAt?: string
  dissolvedAt?: string
  createdAt: string
}

export interface CommitteeDetail extends Committee {
  members: CommitteeMember[]
  tenderId?: string
  tenderTitleAr?: string
  tenderTitleEn?: string
}

export interface CreateCommitteeRequest {
  nameAr: string
  nameEn: string
  type: 'Permanent' | 'AdHoc'
  purposeAr?: string
  purposeEn?: string
  tenderId?: string
  memberUserIds: string[]
  chairUserId: string
}

export interface AddMemberRequest {
  userId: string
  role: string
}

export const useCommitteeStore = defineStore('committees', () => {
  const committees = ref<Committee[]>([])
  const currentCommittee = ref<CommitteeDetail | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const totalCount = ref(0)
  const totalPages = ref(0)

  async function fetchCommittees(params?: {
    pageNumber?: number
    pageSize?: number
    type?: string
    isActive?: boolean
    search?: string
  }) {
    isLoading.value = true
    error.value = null
    try {
      const queryParams = new URLSearchParams()
      if (params?.pageNumber) queryParams.set('pageNumber', params.pageNumber.toString())
      if (params?.pageSize) queryParams.set('pageSize', params.pageSize.toString())
      if (params?.type) queryParams.set('type', params.type)
      if (params?.isActive !== undefined) queryParams.set('isActive', params.isActive.toString())
      if (params?.search) queryParams.set('search', params.search)

      const response = await api.get<ApiResponse<PaginatedResponse<Committee>>>(
        `/committees?${queryParams.toString()}`
      )
      if (response.data.isSuccess && response.data.data) {
        committees.value = response.data.data.items
        totalCount.value = response.data.data.totalCount
        totalPages.value = response.data.data.totalPages
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to fetch committees'
    } finally {
      isLoading.value = false
    }
  }

  async function fetchCommittee(id: string) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get<ApiResponse<CommitteeDetail>>(`/committees/${id}`)
      if (response.data.isSuccess && response.data.data) {
        currentCommittee.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to fetch committee'
    } finally {
      isLoading.value = false
    }
  }

  async function createCommittee(request: CreateCommitteeRequest) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.post<ApiResponse<{ id: string }>>('/committees', request)
      if (response.data.isSuccess) {
        return response.data.data?.id
      }
      error.value = response.data.error || 'Failed to create committee'
      return null
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to create committee'
      return null
    } finally {
      isLoading.value = false
    }
  }

  async function addMember(committeeId: string, request: AddMemberRequest) {
    error.value = null
    try {
      const response = await api.post<ApiResponse<void>>(`/committees/${committeeId}/members`, request)
      if (response.data.isSuccess) {
        await fetchCommittee(committeeId)
        return true
      }
      error.value = response.data.error || 'Failed to add member'
      return false
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to add member'
      return false
    }
  }

  async function removeMember(committeeId: string, memberId: string) {
    error.value = null
    try {
      const response = await api.delete<ApiResponse<void>>(`/committees/${committeeId}/members/${memberId}`)
      if (response.data.isSuccess) {
        await fetchCommittee(committeeId)
        return true
      }
      error.value = response.data.error || 'Failed to remove member'
      return false
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to remove member'
      return false
    }
  }

  async function dissolveCommittee(committeeId: string) {
    error.value = null
    try {
      const response = await api.put<ApiResponse<void>>(`/committees/${committeeId}/dissolve`, {})
      if (response.data.isSuccess) {
        await fetchCommittee(committeeId)
        return true
      }
      error.value = response.data.error || 'Failed to dissolve committee'
      return false
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to dissolve committee'
      return false
    }
  }

  return {
    committees,
    currentCommittee,
    isLoading,
    error,
    totalCount,
    totalPages,
    fetchCommittees,
    fetchCommittee,
    createCommittee,
    addMember,
    removeMember,
    dissolveCommittee,
  }
})
