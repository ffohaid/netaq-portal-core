import { ref } from 'vue'
import { defineStore } from 'pinia'
import api from '../services/api'
import type {
  OrganizationSettings,
  AiConfiguration,
  AiTestResult,
  KnowledgeSourceList,
  SystemSetting,
} from '../types/settings'

export const useSettingsStore = defineStore('settings', () => {
  // State
  const orgSettings = ref<OrganizationSettings | null>(null)
  const aiConfigs = ref<AiConfiguration[]>([])
  const knowledgeBase = ref<KnowledgeSourceList | null>(null)
  const systemSettings = ref<Record<string, SystemSetting[]>>({})
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  // Organization Settings
  async function fetchOrgSettings() {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get('/settings/organization')
      if (response.data.isSuccess) {
        orgSettings.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to load organization settings'
    } finally {
      isLoading.value = false
    }
  }

  async function updateOrgBranding(data: Partial<OrganizationSettings>) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.put('/settings/organization/branding', data)
      if (response.data.isSuccess) {
        await fetchOrgSettings()
      }
      return response.data.isSuccess
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to update branding'
      return false
    } finally {
      isLoading.value = false
    }
  }

  async function updateAuthSettings(data: any) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.put('/settings/organization/auth', data)
      if (response.data.isSuccess) {
        await fetchOrgSettings()
      }
      return response.data.isSuccess
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to update auth settings'
      return false
    } finally {
      isLoading.value = false
    }
  }

  // AI Configuration
  async function fetchAiConfigs() {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get('/settings/ai')
      if (response.data.isSuccess) {
        aiConfigs.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to load AI configurations'
    } finally {
      isLoading.value = false
    }
  }

  async function createAiConfig(data: any) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.post('/settings/ai', data)
      if (response.data.isSuccess) {
        await fetchAiConfigs()
      }
      return response.data.isSuccess
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to create AI configuration'
      return false
    } finally {
      isLoading.value = false
    }
  }

  async function updateAiConfig(id: string, data: any) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.put(`/settings/ai/${id}`, data)
      if (response.data.isSuccess) {
        await fetchAiConfigs()
      }
      return response.data.isSuccess
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to update AI configuration'
      return false
    } finally {
      isLoading.value = false
    }
  }

  async function deleteAiConfig(id: string) {
    try {
      const response = await api.delete(`/settings/ai/${id}`)
      if (response.data.isSuccess) {
        await fetchAiConfigs()
      }
      return response.data.isSuccess
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to delete AI configuration'
      return false
    }
  }

  async function testAiConfig(id: string): Promise<AiTestResult | null> {
    try {
      const response = await api.post(`/settings/ai/${id}/test`)
      if (response.data.isSuccess) {
        return response.data.data
      }
      return null
    } catch (err: any) {
      error.value = err.response?.data?.error || 'AI test failed'
      return null
    }
  }

  // Knowledge Base
  async function fetchKnowledgeBase() {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get('/settings/knowledge-base')
      if (response.data.isSuccess) {
        knowledgeBase.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to load knowledge base'
    } finally {
      isLoading.value = false
    }
  }

  async function addKnowledgeSource(data: any) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.post('/settings/knowledge-base', data)
      if (response.data.isSuccess) {
        await fetchKnowledgeBase()
      }
      return response.data.isSuccess
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to add knowledge source'
      return false
    } finally {
      isLoading.value = false
    }
  }

  async function deleteKnowledgeSource(id: string) {
    try {
      const response = await api.delete(`/settings/knowledge-base/${id}`)
      if (response.data.isSuccess) {
        await fetchKnowledgeBase()
      }
      return response.data.isSuccess
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to delete knowledge source'
      return false
    }
  }

  // System Settings
  async function fetchSystemSettings() {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get('/settings/system')
      if (response.data.isSuccess) {
        systemSettings.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to load system settings'
    } finally {
      isLoading.value = false
    }
  }

  async function updateSystemSetting(id: string, value: string) {
    try {
      const response = await api.put(`/settings/system/${id}`, { value })
      return response.data.isSuccess
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to update setting'
      return false
    }
  }

  return {
    orgSettings,
    aiConfigs,
    knowledgeBase,
    systemSettings,
    isLoading,
    error,
    fetchOrgSettings,
    updateOrgBranding,
    updateAuthSettings,
    fetchAiConfigs,
    createAiConfig,
    updateAiConfig,
    deleteAiConfig,
    testAiConfig,
    fetchKnowledgeBase,
    addKnowledgeSource,
    deleteKnowledgeSource,
    fetchSystemSettings,
    updateSystemSetting,
  }
})
