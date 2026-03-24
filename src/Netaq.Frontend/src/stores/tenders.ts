import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '../services/api'
import type {
  ApiResponse,
  PaginatedResponse,
  Tender,
  TenderDetail,
  TenderSection,
  TenderCriteria,
  BookletTemplate,
  BookletTemplateDetail,
  CreateTenderRequest,
  UpdateSectionRequest,
  SaveCriteriaRequest,
  AiSuggestion,
  AiComplianceCheck,
  AiCriteriaSuggestion,
  TenderStatus,
  TenderType,
} from '../types'

export const useTenderStore = defineStore('tenders', () => {
  // State
  const tenders = ref<Tender[]>([])
  const currentTender = ref<TenderDetail | null>(null)
  const templates = ref<BookletTemplate[]>([])
  const currentTemplate = ref<BookletTemplateDetail | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const totalCount = ref(0)
  const totalPages = ref(0)

  // ===== Tender Actions =====

  async function fetchTenders(params?: {
    pageNumber?: number
    pageSize?: number
    status?: TenderStatus
    type?: TenderType
    search?: string
  }) {
    isLoading.value = true
    error.value = null
    try {
      const queryParams = new URLSearchParams()
      if (params?.pageNumber) queryParams.set('pageNumber', params.pageNumber.toString())
      if (params?.pageSize) queryParams.set('pageSize', params.pageSize.toString())
      if (params?.status) queryParams.set('status', params.status)
      if (params?.type) queryParams.set('type', params.type)
      if (params?.search) queryParams.set('search', params.search)

      const response = await api.get<ApiResponse<PaginatedResponse<Tender>>>(
        `/tenders?${queryParams.toString()}`
      )
      if (response.data.isSuccess && response.data.data) {
        tenders.value = response.data.data.items
        totalCount.value = response.data.data.totalCount
        totalPages.value = response.data.data.totalPages
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to fetch tenders'
    } finally {
      isLoading.value = false
    }
  }

  async function fetchTenderById(id: string) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get<ApiResponse<TenderDetail>>(`/tenders/${id}`)
      if (response.data.isSuccess && response.data.data) {
        currentTender.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to fetch tender'
    } finally {
      isLoading.value = false
    }
  }

  async function createTender(request: CreateTenderRequest): Promise<TenderDetail | null> {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.post<ApiResponse<TenderDetail>>('/tenders', request)
      if (response.data.isSuccess && response.data.data) {
        currentTender.value = response.data.data
        return response.data.data
      }
      error.value = response.data.error || 'Failed to create tender'
      return null
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to create tender'
      return null
    } finally {
      isLoading.value = false
    }
  }

  async function submitForApproval(tenderId: string): Promise<boolean> {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.post<ApiResponse<Tender>>(`/tenders/${tenderId}/submit`)
      if (response.data.isSuccess) {
        if (currentTender.value && currentTender.value.id === tenderId) {
          currentTender.value.status = 'UnderReview'
        }
        return true
      }
      error.value = response.data.error || 'Failed to submit tender'
      return false
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to submit tender'
      return false
    } finally {
      isLoading.value = false
    }
  }

  // ===== Section Actions =====

  async function updateSection(
    tenderId: string,
    request: UpdateSectionRequest
  ): Promise<TenderSection | null> {
    try {
      const response = await api.put<ApiResponse<TenderSection>>(
        `/tenders/${tenderId}/sections/${request.sectionId}`,
        request
      )
      if (response.data.isSuccess && response.data.data) {
        // Update local state
        if (currentTender.value) {
          const idx = currentTender.value.sections.findIndex(s => s.id === request.sectionId)
          if (idx >= 0) {
            currentTender.value.sections[idx] = response.data.data
          }
        }
        return response.data.data
      }
      return null
    } catch {
      return null
    }
  }

  // ===== Criteria Actions =====

  async function saveCriteria(request: SaveCriteriaRequest): Promise<TenderCriteria[] | null> {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.put<ApiResponse<TenderCriteria[]>>(
        `/tenders/${request.tenderId}/criteria`,
        request
      )
      if (response.data.isSuccess && response.data.data) {
        if (currentTender.value) {
          currentTender.value.criteria = response.data.data
        }
        return response.data.data
      }
      error.value = response.data.error || 'Failed to save criteria'
      return null
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to save criteria'
      return null
    } finally {
      isLoading.value = false
    }
  }

  // ===== Template Actions =====

  async function fetchTemplates(params?: {
    category?: string
    tenderType?: string
    activeOnly?: boolean
  }) {
    isLoading.value = true
    error.value = null
    try {
      const queryParams = new URLSearchParams()
      if (params?.category) queryParams.set('category', params.category)
      if (params?.tenderType) queryParams.set('tenderType', params.tenderType)
      if (params?.activeOnly !== undefined) queryParams.set('activeOnly', params.activeOnly.toString())

      const response = await api.get<ApiResponse<BookletTemplate[]>>(
        `/templates?${queryParams.toString()}`
      )
      if (response.data.isSuccess && response.data.data) {
        templates.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to fetch templates'
    } finally {
      isLoading.value = false
    }
  }

  async function fetchTemplateById(id: string) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get<ApiResponse<BookletTemplateDetail>>(`/templates/${id}`)
      if (response.data.isSuccess && response.data.data) {
        currentTemplate.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to fetch template'
    } finally {
      isLoading.value = false
    }
  }

  // ===== AI Actions =====

  async function aiSuggestCriteria(
    tenderId: string,
    criteriaType: 'Technical' | 'Financial',
    additionalContext?: string
  ): Promise<AiCriteriaSuggestion | null> {
    try {
      const response = await api.post<ApiResponse<AiCriteriaSuggestion>>(
        '/ai/drafting/suggest-criteria',
        { tenderId, criteriaType, additionalContext }
      )
      if (response.data.isSuccess && response.data.data) {
        return response.data.data
      }
      return null
    } catch {
      return null
    }
  }

  async function aiCheckCompliance(
    tenderId: string,
    sectionId?: string
  ): Promise<AiComplianceCheck | null> {
    try {
      const response = await api.post<ApiResponse<AiComplianceCheck>>(
        '/ai/drafting/compliance-check',
        { tenderId, sectionId }
      )
      if (response.data.isSuccess && response.data.data) {
        return response.data.data
      }
      return null
    } catch {
      return null
    }
  }

  async function aiGenerateBoilerplate(
    tenderId: string,
    sectionType: string,
    additionalContext?: string
  ): Promise<AiSuggestion | null> {
    try {
      const response = await api.post<ApiResponse<AiSuggestion>>(
        '/ai/drafting/generate-boilerplate',
        { tenderId, sectionType, additionalContext }
      )
      if (response.data.isSuccess && response.data.data) {
        return response.data.data
      }
      return null
    } catch {
      return null
    }
  }

  // ===== Export Actions =====

  async function exportPdf(tenderId: string) {
    try {
      const response = await api.get(`/export/tenders/${tenderId}/pdf`, {
        responseType: 'blob',
      })
      const url = window.URL.createObjectURL(new Blob([response.data]))
      const link = document.createElement('a')
      link.href = url
      link.setAttribute('download', `booklet_${tenderId}.pdf`)
      document.body.appendChild(link)
      link.click()
      link.remove()
      window.URL.revokeObjectURL(url)
    } catch {
      error.value = 'Failed to export PDF'
    }
  }

  async function exportDocx(tenderId: string) {
    try {
      const response = await api.get(`/export/tenders/${tenderId}/docx`, {
        responseType: 'blob',
      })
      const url = window.URL.createObjectURL(new Blob([response.data]))
      const link = document.createElement('a')
      link.href = url
      link.setAttribute('download', `booklet_${tenderId}.docx`)
      document.body.appendChild(link)
      link.click()
      link.remove()
      window.URL.revokeObjectURL(url)
    } catch {
      error.value = 'Failed to export DOCX'
    }
  }

  function clearError() {
    error.value = null
  }

  return {
    // State
    tenders,
    currentTender,
    templates,
    currentTemplate,
    isLoading,
    error,
    totalCount,
    totalPages,
    // Tender Actions
    fetchTenders,
    fetchTenderById,
    createTender,
    submitForApproval,
    // Section Actions
    updateSection,
    // Criteria Actions
    saveCriteria,
    // Template Actions
    fetchTemplates,
    fetchTemplateById,
    // AI Actions
    aiSuggestCriteria,
    aiCheckCompliance,
    aiGenerateBoilerplate,
    // Export Actions
    exportPdf,
    exportDocx,
    // Utility
    clearError,
  }
})
