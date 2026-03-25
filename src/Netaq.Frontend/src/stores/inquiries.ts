import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '../services/api'
import type { ApiResponse, PaginatedResponse } from '../types'

export interface Inquiry {
  id: string
  tenderId: string
  tenderTitleAr?: string
  tenderTitleEn?: string
  subjectAr: string
  subjectEn: string
  questionAr: string
  questionEn: string
  responseAr?: string
  responseEn?: string
  status: 'Submitted' | 'UnderReview' | 'Responded' | 'Closed' | 'Escalated' | 'Reopened'
  priority: 'Low' | 'Normal' | 'High' | 'Urgent'
  category: 'General' | 'Technical' | 'Financial' | 'Legal' | 'Administrative' | 'Clarification'
  submittedByUserId: string
  submittedByUserNameAr?: string
  submittedByUserNameEn?: string
  assignedToUserId?: string
  assignedToUserNameAr?: string
  assignedToUserNameEn?: string
  tenderSectionId?: string
  dueDate?: string
  respondedAt?: string
  closedAt?: string
  createdAt: string
  internalNotesAr?: string
  internalNotesEn?: string
}

export interface InquiryStats {
  totalInquiries: number
  submittedCount: number
  underReviewCount: number
  respondedCount: number
  closedCount: number
  overdueCount: number
}

export interface CreateInquiryRequest {
  tenderId: string
  subjectAr: string
  subjectEn: string
  questionAr: string
  questionEn: string
  priority: string
  category: string
  tenderSectionId?: string
  assignedToUserId?: string
}

export interface RespondToInquiryRequest {
  responseAr: string
  responseEn: string
}

export const useInquiryStore = defineStore('inquiries', () => {
  const inquiries = ref<Inquiry[]>([])
  const currentInquiry = ref<Inquiry | null>(null)
  const statistics = ref<InquiryStats | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const totalCount = ref(0)
  const totalPages = ref(0)

  async function fetchInquiries(params?: {
    pageNumber?: number
    pageSize?: number
    tenderId?: string
    status?: string
    category?: string
    search?: string
  }) {
    isLoading.value = true
    error.value = null
    try {
      const queryParams = new URLSearchParams()
      if (params?.pageNumber) queryParams.set('pageNumber', params.pageNumber.toString())
      if (params?.pageSize) queryParams.set('pageSize', params.pageSize.toString())
      if (params?.tenderId) queryParams.set('tenderId', params.tenderId)
      if (params?.status) queryParams.set('status', params.status)
      if (params?.category) queryParams.set('category', params.category)
      if (params?.search) queryParams.set('search', params.search)

      const response = await api.get<ApiResponse<PaginatedResponse<Inquiry>>>(
        `/inquiries?${queryParams.toString()}`
      )
      if (response.data.isSuccess && response.data.data) {
        inquiries.value = response.data.data.items
        totalCount.value = response.data.data.totalCount
        totalPages.value = response.data.data.totalPages
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to fetch inquiries'
    } finally {
      isLoading.value = false
    }
  }

  async function fetchInquiry(id: string) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get<ApiResponse<Inquiry>>(`/inquiries/${id}`)
      if (response.data.isSuccess && response.data.data) {
        currentInquiry.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to fetch inquiry'
    } finally {
      isLoading.value = false
    }
  }

  async function fetchStatistics(tenderId?: string) {
    try {
      const params = tenderId ? { tenderId } : {}
      const response = await api.get<ApiResponse<InquiryStats>>('/inquiries/statistics', { params })
      if (response.data.isSuccess && response.data.data) {
        statistics.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to fetch statistics'
    }
  }

  async function createInquiry(request: CreateInquiryRequest) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.post<ApiResponse<Inquiry>>('/inquiries', request)
      if (response.data.isSuccess && response.data.data) {
        return response.data.data.id
      }
      error.value = response.data.error || 'Failed to create inquiry'
      return null
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to create inquiry'
      return null
    } finally {
      isLoading.value = false
    }
  }

  async function respondToInquiry(id: string, request: RespondToInquiryRequest) {
    error.value = null
    try {
      const response = await api.put<ApiResponse<Inquiry>>(`/inquiries/${id}/respond`, request)
      if (response.data.isSuccess) {
        await fetchInquiry(id)
        return true
      }
      error.value = response.data.error || 'Failed to respond to inquiry'
      return false
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to respond to inquiry'
      return false
    }
  }

  async function assignInquiry(id: string, assignedToUserId: string) {
    error.value = null
    try {
      const response = await api.put<ApiResponse<Inquiry>>(`/inquiries/${id}/assign`, { assignedToUserId })
      if (response.data.isSuccess) {
        await fetchInquiry(id)
        return true
      }
      error.value = response.data.error || 'Failed to assign inquiry'
      return false
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to assign inquiry'
      return false
    }
  }

  async function closeInquiry(id: string) {
    error.value = null
    try {
      const response = await api.put<ApiResponse<Inquiry>>(`/inquiries/${id}/close`, {})
      if (response.data.isSuccess) {
        await fetchInquiry(id)
        return true
      }
      error.value = response.data.error || 'Failed to close inquiry'
      return false
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to close inquiry'
      return false
    }
  }

  async function escalateInquiry(id: string, escalatedToUserId: string, reason: string) {
    error.value = null
    try {
      const response = await api.put<ApiResponse<Inquiry>>(`/inquiries/${id}/escalate`, {
        escalatedToUserId,
        reason
      })
      if (response.data.isSuccess) {
        await fetchInquiry(id)
        return true
      }
      error.value = response.data.error || 'Failed to escalate inquiry'
      return false
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to escalate inquiry'
      return false
    }
  }

  async function reopenInquiry(id: string) {
    error.value = null
    try {
      const response = await api.put<ApiResponse<Inquiry>>(`/inquiries/${id}/reopen`, {})
      if (response.data.isSuccess) {
        await fetchInquiry(id)
        return true
      }
      error.value = response.data.error || 'Failed to reopen inquiry'
      return false
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to reopen inquiry'
      return false
    }
  }

  async function addNote(id: string, noteAr: string, noteEn: string) {
    error.value = null
    try {
      const response = await api.post<ApiResponse<Inquiry>>(`/inquiries/${id}/notes`, {
        noteAr,
        noteEn
      })
      if (response.data.isSuccess) {
        await fetchInquiry(id)
        return true
      }
      error.value = response.data.error || 'Failed to add note'
      return false
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to add note'
      return false
    }
  }

  async function exportInquiries(tenderId?: string, status?: string) {
    try {
      const params: any = {}
      if (tenderId) params.tenderId = tenderId
      if (status) params.status = status
      const response = await api.get('/inquiries/export', {
        params,
        responseType: 'blob'
      })
      const blob = new Blob([response.data as any], { type: 'text/csv;charset=utf-8;' })
      const url = window.URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `inquiries_${new Date().toISOString().split('T')[0]}.csv`
      link.click()
      window.URL.revokeObjectURL(url)
      return true
    } catch (err: any) {
      error.value = 'Failed to export inquiries'
      return false
    }
  }

  return {
    inquiries,
    currentInquiry,
    statistics,
    isLoading,
    error,
    totalCount,
    totalPages,
    fetchInquiries,
    fetchInquiry,
    fetchStatistics,
    createInquiry,
    respondToInquiry,
    assignInquiry,
    closeInquiry,
    escalateInquiry,
    reopenInquiry,
    addNote,
    exportInquiries,
  }
})
