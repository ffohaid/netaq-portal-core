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

  // All Committees (for admin)
  const allCommittees = ref<any>(null)

  // Reports
  const tenderReport = ref<TenderStatusReport | null>(null)
  const slaReport = ref<SlaComplianceReport | null>(null)
  const userActivityReport = ref<UserActivityReport | null>(null)
  const customReport = ref<any>(null)
  const customReportLoading = ref(false)

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

  async function fetchAllCommittees() {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.get('/dashboard/all-committees')
      if (response.data.isSuccess) {
        allCommittees.value = response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to load committees'
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

  async function fetchCustomReport(params: {
    reportType: string
    startDate?: string
    endDate?: string
    status?: string
    priority?: string
    departmentId?: string
    committeeId?: string
    format?: string
  }) {
    customReportLoading.value = true
    error.value = null
    customReport.value = null
    try {
      const response = await api.post('/reports/custom', params)
      if (response.data.isSuccess) {
        customReport.value = response.data.data
        return response.data.data
      } else {
        error.value = response.data.error || 'Failed to generate report'
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to generate custom report'
    } finally {
      customReportLoading.value = false
    }
    return null
  }

  async function fetchAvailableReportTypes() {
    try {
      const response = await api.get('/reports/available-types')
      if (response.data.isSuccess) {
        return response.data.data
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to load report types'
    }
    return []
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
    allCommittees,
    fetchAllCommittees,
    fetchTenderReport,
    fetchSlaReport,
    fetchUserActivityReport,
    customReport,
    customReportLoading,
    fetchCustomReport,
    fetchAvailableReportTypes,
  }
})
