<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useDashboardStore } from '../../stores/dashboard'
import { getCurrentLocale } from '../../i18n'
import { formatCurrency } from '../../utils/formatters'

const { t } = useI18n()
const dashboardStore = useDashboardStore()
const locale = computed(() => getCurrentLocale())
const activeReport = ref<string>('tenders')
const reportError = ref<string | null>(null)

// Custom Report Builder
const showCustomBuilder = ref(false)
const customReportType = ref('TenderSummary')
const customStartDate = ref('')
const customEndDate = ref('')
const customStatus = ref('')
const customPriority = ref('')
const customGenerating = ref(false)
const customResult = ref<any>(null)

const reportTypes = [
  { value: 'TenderSummary', labelAr: 'ملخص المنافسات', labelEn: 'Tender Summary' },
  { value: 'TenderDetailed', labelAr: 'تقرير المنافسات التفصيلي', labelEn: 'Detailed Tender Report' },
  { value: 'CommitteePerformance', labelAr: 'أداء اللجان', labelEn: 'Committee Performance' },
  { value: 'InquiryAnalysis', labelAr: 'تحليل الاستفسارات', labelEn: 'Inquiry Analysis' },
  { value: 'WorkflowEfficiency', labelAr: 'كفاءة سير العمل', labelEn: 'Workflow Efficiency' },
  { value: 'SlaCompliance', labelAr: 'امتثال SLA', labelEn: 'SLA Compliance' },
  { value: 'UserProductivity', labelAr: 'إنتاجية المستخدمين', labelEn: 'User Productivity' },
  { value: 'AuditTrail', labelAr: 'سجل التدقيق', labelEn: 'Audit Trail' },
]

onMounted(async () => {
  await loadReport('tenders')
})

async function loadReport(report: string) {
  activeReport.value = report
  reportError.value = null
  showCustomBuilder.value = false
  customResult.value = null
  try {
    switch (report) {
      case 'tenders': await dashboardStore.fetchTenderReport(); break
      case 'sla': await dashboardStore.fetchSlaReport(); break
      case 'users': await dashboardStore.fetchUserActivityReport(); break
      case 'custom': showCustomBuilder.value = true; return
    }
    if (dashboardStore.error) {
      reportError.value = dashboardStore.error
    }
  } catch (err: any) {
    reportError.value = err.message || 'Failed to load report'
  }
}

async function generateCustomReport() {
  customGenerating.value = true
  reportError.value = null
  customResult.value = null
  try {
    const result = await dashboardStore.fetchCustomReport({
      reportType: customReportType.value,
      startDate: customStartDate.value || undefined,
      endDate: customEndDate.value || undefined,
      status: customStatus.value || undefined,
      priority: customPriority.value || undefined,
    })
    if (result) {
      customResult.value = result
    } else if (dashboardStore.error) {
      reportError.value = dashboardStore.error
    }
  } catch (err: any) {
    reportError.value = err.message || 'Failed to generate report'
  } finally {
    customGenerating.value = false
  }
}

function exportCustomReport(format: string) {
  if (!customResult.value) return
  const data = JSON.stringify(customResult.value, null, 2)
  if (format === 'json') {
    const blob = new Blob([data], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `report_${customReportType.value}_${new Date().toISOString().slice(0,10)}.json`
    a.click()
    URL.revokeObjectURL(url)
  } else if (format === 'csv') {
    let csv = ''
    if (customResult.value.data && Array.isArray(customResult.value.data)) {
      const items = customResult.value.data
      if (items.length > 0) {
        csv = Object.keys(items[0]).join(',') + '\n'
        items.forEach((item: any) => {
          csv += Object.values(item).map(v => `"${v}"`).join(',') + '\n'
        })
      }
    } else if (customResult.value.summary) {
      csv = Object.entries(customResult.value.summary).map(([k, v]) => `${k},${v}`).join('\n')
    }
    const blob = new Blob(['\ufeff' + csv], { type: 'text/csv;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `report_${customReportType.value}_${new Date().toISOString().slice(0,10)}.csv`
    a.click()
    URL.revokeObjectURL(url)
  }
}

function printReport() {
  window.print()
}

function formatDate(dateStr: string) {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString(locale.value === 'ar' ? 'ar-SA' : 'en-US', {
    year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit'
  })
}

function getStatusLabel(status: string) {
  if (locale.value === 'ar') {
    const m: Record<string, string> = { Draft: 'مسودة', PendingApproval: 'بانتظار الاعتماد', Approved: 'معتمدة', Published: 'منشورة', UnderEvaluation: 'قيد التقييم', EvaluationCompleted: 'اكتمل التقييم', Awarded: 'مرسى عليها', Cancelled: 'ملغاة', Archived: 'مؤرشفة' }
    return m[status] || status
  }
  const m: Record<string, string> = { Draft: 'Draft', PendingApproval: 'Pending Approval', Approved: 'Approved', Published: 'Published', UnderEvaluation: 'Under Evaluation', EvaluationCompleted: 'Evaluation Completed', Awarded: 'Awarded', Cancelled: 'Cancelled', Archived: 'Archived' }
  return m[status] || status
}

function getStatusColor(status: string) {
  const m: Record<string, string> = { Draft: 'bg-gray-100 text-gray-700', PendingApproval: 'bg-yellow-100 text-yellow-700', Approved: 'bg-green-100 text-green-700', Published: 'bg-blue-100 text-blue-700', UnderEvaluation: 'bg-purple-100 text-purple-700', EvaluationCompleted: 'bg-indigo-100 text-indigo-700', Awarded: 'bg-emerald-100 text-emerald-700', Cancelled: 'bg-red-100 text-red-700', Archived: 'bg-gray-100 text-gray-600' }
  return m[status] || 'bg-gray-100 text-gray-700'
}

function getMonthName(month: number) {
  const months = locale.value === 'ar'
    ? ['يناير','فبراير','مارس','أبريل','مايو','يونيو','يوليو','أغسطس','سبتمبر','أكتوبر','نوفمبر','ديسمبر']
    : ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec']
  return months[month - 1] || ''
}

function getReportTypeLabel(value: string) {
  const rt = reportTypes.find(r => r.value === value)
  return rt ? (locale.value === 'ar' ? rt.labelAr : rt.labelEn) : value
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ locale === 'ar' ? 'التقارير' : 'Reports' }}</h1>
        <p class="text-gray-500 mt-1">{{ locale === 'ar' ? 'تقارير شاملة ومخصصة عن المنافسات والأداء' : 'Comprehensive and custom reports on tenders and performance' }}</p>
      </div>
      <button @click="printReport" class="btn-secondary flex items-center gap-2">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z" />
        </svg>
        {{ locale === 'ar' ? 'طباعة' : 'Print' }}
      </button>
    </div>

    <!-- Report Tabs -->
    <div class="border-b border-gray-200">
      <nav class="flex gap-6 overflow-x-auto">
        <button @click="loadReport('tenders')" class="pb-3 text-sm font-medium border-b-2 transition-colors whitespace-nowrap"
          :class="activeReport === 'tenders' ? 'border-primary-500 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'">
          {{ locale === 'ar' ? 'حالة المنافسات' : 'Tender Status' }}
        </button>
        <button @click="loadReport('sla')" class="pb-3 text-sm font-medium border-b-2 transition-colors whitespace-nowrap"
          :class="activeReport === 'sla' ? 'border-primary-500 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'">
          {{ locale === 'ar' ? 'امتثال SLA' : 'SLA Compliance' }}
        </button>
        <button @click="loadReport('users')" class="pb-3 text-sm font-medium border-b-2 transition-colors whitespace-nowrap"
          :class="activeReport === 'users' ? 'border-primary-500 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'">
          {{ locale === 'ar' ? 'نشاط المستخدمين' : 'User Activity' }}
        </button>
        <button @click="loadReport('custom')" class="pb-3 text-sm font-medium border-b-2 transition-colors whitespace-nowrap"
          :class="activeReport === 'custom' ? 'border-primary-500 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'">
          <span class="flex items-center gap-1.5">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4" />
            </svg>
            {{ locale === 'ar' ? 'تقرير مخصص' : 'Custom Report' }}
          </span>
        </button>
      </nav>
    </div>

    <!-- Loading -->
    <div v-if="dashboardStore.isLoading && !showCustomBuilder" class="flex justify-center py-12">
      <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-primary-600"></div>
    </div>

    <!-- Error -->
    <div v-else-if="reportError && !showCustomBuilder" class="bg-yellow-50 border border-yellow-200 rounded-xl p-8 text-center">
      <svg class="mx-auto h-12 w-12 text-yellow-400 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z" />
      </svg>
      <h3 class="text-lg font-medium text-yellow-800">{{ locale === 'ar' ? 'لا تتوفر بيانات كافية' : 'Insufficient data' }}</h3>
      <p class="text-yellow-600 mt-2 text-sm">{{ locale === 'ar' ? 'سيتم توفير التقارير تلقائياً عند إضافة بيانات كافية' : 'Reports will be available once sufficient data is added' }}</p>
      <button @click="loadReport(activeReport)" class="mt-4 px-4 py-2 bg-yellow-100 text-yellow-800 rounded-lg hover:bg-yellow-200 text-sm font-medium">
        {{ locale === 'ar' ? 'إعادة المحاولة' : 'Retry' }}
      </button>
    </div>

    <!-- ==================== CUSTOM REPORT BUILDER ==================== -->
    <template v-else-if="showCustomBuilder">
      <div class="space-y-6">
        <!-- Builder Card -->
        <div class="card">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">
            <span class="flex items-center gap-2">
              <svg class="w-5 h-5 text-primary-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4" />
              </svg>
              {{ locale === 'ar' ? 'إنشاء تقرير مخصص' : 'Create Custom Report' }}
            </span>
          </h2>
          <p class="text-sm text-gray-500 mb-6">{{ locale === 'ar' ? 'حدد نوع التقرير والمعايير المطلوبة لإنشاء تقرير مخصص يناسب احتياجاتك' : 'Select report type and criteria to generate a custom report tailored to your needs' }}</p>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <!-- Report Type -->
            <div class="md:col-span-2">
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ locale === 'ar' ? 'نوع التقرير' : 'Report Type' }} *</label>
              <select v-model="customReportType" class="input-field">
                <option v-for="rt in reportTypes" :key="rt.value" :value="rt.value">
                  {{ locale === 'ar' ? rt.labelAr : rt.labelEn }}
                </option>
              </select>
            </div>

            <!-- Date Range -->
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ locale === 'ar' ? 'من تاريخ' : 'Start Date' }}</label>
              <input v-model="customStartDate" type="date" class="input-field" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ locale === 'ar' ? 'إلى تاريخ' : 'End Date' }}</label>
              <input v-model="customEndDate" type="date" class="input-field" />
            </div>

            <!-- Status Filter -->
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ locale === 'ar' ? 'الحالة' : 'Status' }}</label>
              <select v-model="customStatus" class="input-field">
                <option value="">{{ locale === 'ar' ? 'الكل' : 'All' }}</option>
                <option value="Draft">{{ getStatusLabel('Draft') }}</option>
                <option value="PendingApproval">{{ getStatusLabel('PendingApproval') }}</option>
                <option value="Approved">{{ getStatusLabel('Approved') }}</option>
                <option value="Published">{{ getStatusLabel('Published') }}</option>
                <option value="UnderEvaluation">{{ getStatusLabel('UnderEvaluation') }}</option>
                <option value="Awarded">{{ getStatusLabel('Awarded') }}</option>
                <option value="Cancelled">{{ getStatusLabel('Cancelled') }}</option>
              </select>
            </div>

            <!-- Priority Filter -->
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ locale === 'ar' ? 'الأولوية' : 'Priority' }}</label>
              <select v-model="customPriority" class="input-field">
                <option value="">{{ locale === 'ar' ? 'الكل' : 'All' }}</option>
                <option value="Low">{{ locale === 'ar' ? 'منخفضة' : 'Low' }}</option>
                <option value="Medium">{{ locale === 'ar' ? 'متوسطة' : 'Medium' }}</option>
                <option value="High">{{ locale === 'ar' ? 'عالية' : 'High' }}</option>
                <option value="Critical">{{ locale === 'ar' ? 'حرجة' : 'Critical' }}</option>
              </select>
            </div>
          </div>

          <!-- Generate Button -->
          <div class="flex items-center gap-3 mt-6 pt-4 border-t">
            <button @click="generateCustomReport" :disabled="customGenerating" class="btn-primary flex items-center gap-2">
              <svg v-if="customGenerating" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
              <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              {{ customGenerating ? (locale === 'ar' ? 'جاري الإنشاء...' : 'Generating...') : (locale === 'ar' ? 'إنشاء التقرير' : 'Generate Report') }}
            </button>
          </div>
        </div>

        <!-- Custom Report Results -->
        <div v-if="customResult" class="card">
          <div class="flex items-center justify-between mb-6">
            <h2 class="text-lg font-semibold text-gray-900">
              {{ getReportTypeLabel(customResult.reportType || customReportType) }}
            </h2>
            <div class="flex items-center gap-2">
              <button @click="exportCustomReport('csv')" class="btn-secondary text-sm flex items-center gap-1.5">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
                CSV
              </button>
              <button @click="exportCustomReport('json')" class="btn-secondary text-sm flex items-center gap-1.5">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
                JSON
              </button>
            </div>
          </div>

          <!-- Report Date Range -->
          <div v-if="customResult.startDate || customResult.endDate" class="text-xs text-gray-400 mb-4">
            {{ locale === 'ar' ? 'الفترة:' : 'Period:' }}
            {{ customResult.startDate ? formatDate(customResult.startDate) : '-' }}
            {{ locale === 'ar' ? 'إلى' : 'to' }}
            {{ customResult.endDate ? formatDate(customResult.endDate) : '-' }}
          </div>

          <!-- Summary -->
          <div v-if="customResult.summary" class="grid grid-cols-2 sm:grid-cols-4 gap-3 mb-6">
            <div v-for="(value, key) in customResult.summary" :key="key" class="p-3 bg-gray-50 rounded-lg text-center">
              <p class="text-xl font-bold text-gray-900">{{ typeof value === 'number' && value > 1000 ? formatCurrency(value) : value }}</p>
              <p class="text-xs text-gray-500 mt-1">{{ key }}</p>
            </div>
          </div>

          <!-- Data Table -->
          <div v-if="customResult.data && Array.isArray(customResult.data) && customResult.data.length > 0" class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="text-start text-gray-500 border-b">
                  <th v-for="col in Object.keys(customResult.data[0])" :key="col" class="pb-3 pe-4 font-medium whitespace-nowrap">
                    {{ col }}
                  </th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100">
                <tr v-for="(row, idx) in customResult.data" :key="idx" class="hover:bg-gray-50">
                  <td v-for="col in Object.keys(row)" :key="col" class="py-3 pe-4 text-gray-700 whitespace-nowrap">
                    {{ row[col] }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Breakdown -->
          <div v-if="customResult.breakdown && Array.isArray(customResult.breakdown) && customResult.breakdown.length > 0" class="mt-6">
            <h3 class="text-sm font-semibold text-gray-700 mb-3">{{ locale === 'ar' ? 'التوزيع' : 'Breakdown' }}</h3>
            <div class="space-y-2">
              <div v-for="item in customResult.breakdown" :key="item.label || item.status" class="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                <span class="text-sm font-medium text-gray-700">{{ item.label || item.status || item.name }}</span>
                <span class="text-sm font-bold text-gray-900">{{ item.count || item.value }}</span>
              </div>
            </div>
          </div>

          <!-- Generated At -->
          <div class="mt-4 pt-4 border-t text-xs text-gray-400 text-end">
            {{ locale === 'ar' ? 'تم الإنشاء:' : 'Generated:' }} {{ formatDate(customResult.generatedAt || new Date().toISOString()) }}
          </div>
        </div>

        <!-- Error for custom report -->
        <div v-if="reportError" class="bg-red-50 border border-red-200 rounded-xl p-6 text-center">
          <p class="text-sm text-red-700">{{ reportError }}</p>
          <button @click="reportError = null" class="mt-2 text-sm text-red-500 hover:text-red-700">{{ locale === 'ar' ? 'إغلاق' : 'Dismiss' }}</button>
        </div>
      </div>
    </template>

    <!-- ==================== TENDER STATUS REPORT ==================== -->
    <template v-else-if="activeReport === 'tenders' && dashboardStore.tenderReport">
      <div class="space-y-6">
        <div class="card">
          <div class="flex items-center justify-between mb-6">
            <h2 class="text-lg font-semibold text-gray-900">{{ locale === 'ar' ? 'تقرير حالة المنافسات' : 'Tender Status Report' }}</h2>
            <span class="text-xs text-gray-400">{{ formatDate(dashboardStore.tenderReport.generatedAt) }}</span>
          </div>
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
            <div class="p-4 bg-primary-50 rounded-lg text-center">
              <p class="text-3xl font-bold text-primary-700">{{ dashboardStore.tenderReport.totalTenders }}</p>
              <p class="text-sm text-gray-500">{{ locale === 'ar' ? 'إجمالي المنافسات' : 'Total Tenders' }}</p>
            </div>
            <div class="p-4 bg-green-50 rounded-lg text-center">
              <p class="text-2xl font-bold text-green-700">{{ formatCurrency(dashboardStore.tenderReport.totalEstimatedValue || 0) }}</p>
              <p class="text-sm text-gray-500">{{ locale === 'ar' ? 'القيمة التقديرية' : 'Total Estimated Value' }}</p>
            </div>
            <div class="p-4 bg-blue-50 rounded-lg text-center">
              <p class="text-3xl font-bold text-blue-700">{{ (dashboardStore.tenderReport.averageCompletionPercentage || 0).toFixed(0) }}%</p>
              <p class="text-sm text-gray-500">{{ locale === 'ar' ? 'متوسط الإنجاز' : 'Avg Completion' }}</p>
            </div>
          </div>
          <h3 class="text-sm font-semibold text-gray-700 mb-3">{{ locale === 'ar' ? 'توزيع حسب الحالة' : 'By Status' }}</h3>
          <div v-if="dashboardStore.tenderReport.statusBreakdown?.length" class="overflow-x-auto mb-6">
            <table class="w-full text-sm">
              <thead><tr class="text-start text-gray-500 border-b">
                <th class="pb-3 font-medium">{{ locale === 'ar' ? 'الحالة' : 'Status' }}</th>
                <th class="pb-3 font-medium">{{ locale === 'ar' ? 'العدد' : 'Count' }}</th>
                <th class="pb-3 font-medium">{{ locale === 'ar' ? 'القيمة' : 'Value' }}</th>
              </tr></thead>
              <tbody class="divide-y divide-gray-100">
                <tr v-for="item in dashboardStore.tenderReport.statusBreakdown" :key="item.status">
                  <td class="py-3"><span class="px-2.5 py-0.5 rounded-full text-xs font-medium" :class="getStatusColor(item.status)">{{ getStatusLabel(item.status) }}</span></td>
                  <td class="py-3 font-medium text-gray-900">{{ item.count }}</td>
                  <td class="py-3 text-gray-600">{{ formatCurrency(item.totalValue) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
          <div v-else class="text-center py-4 text-gray-400 text-sm">{{ locale === 'ar' ? 'لا توجد بيانات' : 'No data' }}</div>
          <h3 class="text-sm font-semibold text-gray-700 mb-3">{{ locale === 'ar' ? 'الاتجاه الشهري' : 'Monthly Trend' }}</h3>
          <div v-if="dashboardStore.tenderReport.monthlyTrend?.length" class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead><tr class="text-start text-gray-500 border-b">
                <th class="pb-3 font-medium">{{ locale === 'ar' ? 'الشهر' : 'Month' }}</th>
                <th class="pb-3 font-medium">{{ locale === 'ar' ? 'العدد' : 'Count' }}</th>
                <th class="pb-3 font-medium">{{ locale === 'ar' ? 'القيمة' : 'Value' }}</th>
              </tr></thead>
              <tbody class="divide-y divide-gray-100">
                <tr v-for="item in dashboardStore.tenderReport.monthlyTrend" :key="`${item.year}-${item.month}`">
                  <td class="py-3 text-gray-900">{{ getMonthName(item.month) }} {{ item.year }}</td>
                  <td class="py-3 font-medium text-gray-900">{{ item.count }}</td>
                  <td class="py-3 text-gray-600">{{ formatCurrency(item.totalValue) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
          <div v-else class="text-center py-4 text-gray-400 text-sm">{{ locale === 'ar' ? 'لا توجد بيانات' : 'No data' }}</div>
        </div>
      </div>
    </template>

    <!-- ==================== SLA COMPLIANCE REPORT ==================== -->
    <template v-else-if="activeReport === 'sla' && dashboardStore.slaReport">
      <div class="space-y-6">
        <div class="card">
          <div class="flex items-center justify-between mb-6">
            <h2 class="text-lg font-semibold text-gray-900">{{ locale === 'ar' ? 'تقرير امتثال SLA' : 'SLA Compliance Report' }}</h2>
            <span class="text-xs text-gray-400">{{ formatDate(dashboardStore.slaReport.generatedAt) }}</span>
          </div>
          <div class="grid grid-cols-1 sm:grid-cols-4 gap-4 mb-6">
            <div class="p-4 bg-gray-50 rounded-lg text-center">
              <p class="text-3xl font-bold" :class="dashboardStore.slaReport.complianceRate >= 80 ? 'text-green-600' : 'text-red-600'">{{ (dashboardStore.slaReport.complianceRate || 0).toFixed(1) }}%</p>
              <p class="text-sm text-gray-500">{{ locale === 'ar' ? 'نسبة الامتثال' : 'Compliance Rate' }}</p>
            </div>
            <div class="p-4 bg-green-50 rounded-lg text-center">
              <p class="text-3xl font-bold text-green-600">{{ dashboardStore.slaReport.onTrackCount }}</p>
              <p class="text-sm text-gray-500">{{ locale === 'ar' ? 'في الوقت' : 'On Track' }}</p>
            </div>
            <div class="p-4 bg-yellow-50 rounded-lg text-center">
              <p class="text-3xl font-bold text-yellow-600">{{ dashboardStore.slaReport.atRiskCount }}</p>
              <p class="text-sm text-gray-500">{{ locale === 'ar' ? 'معرضة للخطر' : 'At Risk' }}</p>
            </div>
            <div class="p-4 bg-red-50 rounded-lg text-center">
              <p class="text-3xl font-bold text-red-600">{{ dashboardStore.slaReport.overdueCount }}</p>
              <p class="text-sm text-gray-500">{{ locale === 'ar' ? 'متأخرة' : 'Overdue' }}</p>
            </div>
          </div>
          <h3 class="text-sm font-semibold text-gray-700 mb-3">{{ locale === 'ar' ? 'مقاييس المهام' : 'Task Metrics' }}</h3>
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div class="p-4 bg-gray-50 rounded-lg text-center">
              <p class="text-2xl font-bold text-gray-900">{{ dashboardStore.slaReport.totalTasks }}</p>
              <p class="text-sm text-gray-500">{{ locale === 'ar' ? 'إجمالي المهام' : 'Total Tasks' }}</p>
            </div>
            <div class="p-4 bg-orange-50 rounded-lg text-center">
              <p class="text-2xl font-bold text-orange-600">{{ dashboardStore.slaReport.escalatedTaskCount }}</p>
              <p class="text-sm text-gray-500">{{ locale === 'ar' ? 'مهام مُصعّدة' : 'Escalated Tasks' }}</p>
            </div>
            <div class="p-4 bg-blue-50 rounded-lg text-center">
              <p class="text-2xl font-bold text-blue-600">{{ (dashboardStore.slaReport.averageTaskCompletionHours || 0).toFixed(1) }}h</p>
              <p class="text-sm text-gray-500">{{ locale === 'ar' ? 'متوسط وقت الإنجاز' : 'Avg Completion Time' }}</p>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- ==================== USER ACTIVITY REPORT ==================== -->
    <template v-else-if="activeReport === 'users' && dashboardStore.userActivityReport">
      <div class="space-y-6">
        <div class="card">
          <div class="flex items-center justify-between mb-6">
            <h2 class="text-lg font-semibold text-gray-900">{{ locale === 'ar' ? 'تقرير نشاط المستخدمين' : 'User Activity Report' }}</h2>
            <span class="text-xs text-gray-400">{{ formatDate(dashboardStore.userActivityReport.generatedAt) }}</span>
          </div>
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
            <div class="p-4 bg-gray-50 rounded-lg text-center">
              <p class="text-3xl font-bold text-gray-900">{{ dashboardStore.userActivityReport.totalUsers }}</p>
              <p class="text-sm text-gray-500">{{ locale === 'ar' ? 'إجمالي المستخدمين' : 'Total Users' }}</p>
            </div>
            <div class="p-4 bg-green-50 rounded-lg text-center">
              <p class="text-3xl font-bold text-green-600">{{ dashboardStore.userActivityReport.uniqueActiveUsers || 0 }}</p>
              <p class="text-sm text-gray-500">{{ locale === 'ar' ? 'المستخدمون النشطون' : 'Active Users' }}</p>
            </div>
            <div class="p-4 bg-blue-50 rounded-lg text-center">
              <p class="text-3xl font-bold text-blue-600">{{ dashboardStore.userActivityReport.totalActions || 0 }}</p>
              <p class="text-sm text-gray-500">{{ locale === 'ar' ? 'إجمالي الإجراءات' : 'Total Actions' }}</p>
            </div>
          </div>
          <template v-if="dashboardStore.userActivityReport.actionsByCategory?.length">
            <h3 class="text-sm font-semibold text-gray-700 mb-3">{{ locale === 'ar' ? 'الإجراءات حسب التصنيف' : 'Actions by Category' }}</h3>
            <div class="overflow-x-auto mb-6">
              <table class="w-full text-sm">
                <thead><tr class="text-start text-gray-500 border-b">
                  <th class="pb-3 font-medium">{{ locale === 'ar' ? 'التصنيف' : 'Category' }}</th>
                  <th class="pb-3 font-medium">{{ locale === 'ar' ? 'العدد' : 'Count' }}</th>
                </tr></thead>
                <tbody class="divide-y divide-gray-100">
                  <tr v-for="item in dashboardStore.userActivityReport.actionsByCategory" :key="item.category">
                    <td class="py-3 font-medium text-gray-900">{{ item.category }}</td>
                    <td class="py-3 text-gray-600">{{ item.count }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </template>
          <h3 class="text-sm font-semibold text-gray-700 mb-3">{{ locale === 'ar' ? 'أكثر المستخدمين نشاطاً' : 'Top Active Users' }}</h3>
          <div v-if="dashboardStore.userActivityReport.topActiveUsers?.length" class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead><tr class="text-start text-gray-500 border-b">
                <th class="pb-3 font-medium">#</th>
                <th class="pb-3 font-medium">{{ locale === 'ar' ? 'المستخدم' : 'User' }}</th>
                <th class="pb-3 font-medium">{{ locale === 'ar' ? 'الإجراءات' : 'Actions' }}</th>
              </tr></thead>
              <tbody class="divide-y divide-gray-100">
                <tr v-for="(user, index) in dashboardStore.userActivityReport.topActiveUsers" :key="user.userId">
                  <td class="py-3 text-gray-400">{{ index + 1 }}</td>
                  <td class="py-3 font-medium text-gray-900">{{ user.userName || user.fullNameAr || user.fullNameEn || '-' }}</td>
                  <td class="py-3 text-gray-600">{{ user.actionCount }}</td>
                </tr>
              </tbody>
            </table>
          </div>
          <div v-else class="text-center py-4 text-gray-400 text-sm">{{ locale === 'ar' ? 'لا توجد بيانات' : 'No data' }}</div>
        </div>
      </div>
    </template>

    <!-- No Data -->
    <div v-else class="card text-center py-12">
      <svg class="mx-auto h-16 w-16 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
      </svg>
      <h3 class="mt-4 text-lg font-medium text-gray-900">{{ locale === 'ar' ? 'لا توجد بيانات' : 'No Report Data' }}</h3>
      <p class="mt-2 text-gray-500">{{ locale === 'ar' ? 'ستتوفر التقارير عند إضافة بيانات كافية' : 'Reports will be available when sufficient data is added' }}</p>
    </div>
  </div>
</template>
