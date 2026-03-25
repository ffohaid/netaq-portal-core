import { ref } from 'vue'
import { defineStore } from 'pinia'
import api from '../services/api'
import type {
  ExecutiveDashboard,
  OperationalDashboard,
  CommitteeDashboard,
  MonitoringDashboard,
  TenderStatusReport,
  SlaComplianceReport,
  UserActivityReport,
} from '../types/settings'

export const useDashboardStore = defineStore('dashboard', () => {
  // State
  const dashboardType = ref<string>('')
  const dashboardData = ref<any>(null)
  const executiveDashboard = ref<ExecutiveDashboard | null>(null)
  const operationalDashboard = ref<OperationalDashboard | null>(null)
  const committeeDashboard = ref<CommitteeDashboard | null>(null)
  const monitoringDashboard = ref<MonitoringDashboard | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  // Reports
  const tenderReport = ref<TenderStatusReport | null>(null)
  const slaReport = ref<SlaComplianceReport | null>(null)
  const userActivityReport = ref<UserActivityReport | null>(null)

  // Auto Dashboard (role-based)
  async function fetchAutoDashboard() {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get('/dashboard')
      if (response.data.isSuccess) {
        dashboardType.value = response.data.data.type
        dashboardData.value = response.data.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to load dashboard'
    } finally {
      isLoading.value = false
    }
  }

  // Specific Dashboards
  async function fetchExecutiveDashboard() {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get('/dashboard/executive')
      if (response.data.isSuccess) {
        executiveDashboard.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to load executive dashboard'
    } finally {
      isLoading.value = false
    }
  }

  async function fetchOperationalDashboard() {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get('/dashboard/operational')
      if (response.data.isSuccess) {
        operationalDashboard.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to load operational dashboard'
    } finally {
      isLoading.value = false
    }
  }

  async function fetchCommitteeDashboard() {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get('/dashboard/committee')
      if (response.data.isSuccess) {
        committeeDashboard.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to load committee dashboard'
    } finally {
      isLoading.value = false
    }
  }

  async function fetchMonitoringDashboard() {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get('/dashboard/monitoring')
      if (response.data.isSuccess) {
        monitoringDashboard.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to load monitoring dashboard'
    } finally {
      isLoading.value = false
    }
  }

  // Reports
  async function fetchTenderReport() {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get('/reports/tender-status')
      if (response.data.isSuccess) {
        tenderReport.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to load tender report'
    } finally {
      isLoading.value = false
    }
  }

  async function fetchSlaReport() {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get('/reports/sla-compliance')
      if (response.data.isSuccess) {
        slaReport.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to load SLA report'
    } finally {
      isLoading.value = false
    }
  }

  async function fetchUserActivityReport() {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get('/reports/user-activity')
      if (response.data.isSuccess) {
        userActivityReport.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to load user activity report'
    } finally {
      isLoading.value = false
    }
  }

  return {
    dashboardType,
    dashboardData,
    executiveDashboard,
    operationalDashboard,
    committeeDashboard,
    monitoringDashboard,
    isLoading,
    error,
    tenderReport,
    slaReport,
    userActivityReport,
    fetchAutoDashboard,
    fetchExecutiveDashboard,
    fetchOperationalDashboard,
    fetchCommitteeDashboard,
    fetchMonitoringDashboard,
    fetchTenderReport,
    fetchSlaReport,
    fetchUserActivityReport,
  }
})
