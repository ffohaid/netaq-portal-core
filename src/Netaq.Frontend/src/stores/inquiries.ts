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
  status: 'Submitted' | 'UnderReview' | 'Responded' | 'Closed' | 'Escalated'
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
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const totalCount = ref(0)
  const totalPages = ref(0)

  async function fetchInquiries(params?: {
    pageNumber?: number
    pageSize?: number
    tenderId?: string
    status?: string
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

  return {
    inquiries,
    currentInquiry,
    isLoading,
    error,
    totalCount,
    totalPages,
    fetchInquiries,
    fetchInquiry,
    createInquiry,
    respondToInquiry,
    assignInquiry,
    closeInquiry,
  }
})
