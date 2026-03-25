<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useDashboardStore } from '../../stores/dashboard'
import { getCurrentLocale } from '../../i18n'
import { formatCurrency } from '../../utils/formatters'

const { t } = useI18n()
const dashboardStore = useDashboardStore()
const locale = computed(() => getCurrentLocale())
const activeReport = ref<'tenders' | 'sla' | 'users'>('tenders')
const reportError = ref<string | null>(null)

onMounted(async () => {
  await loadReport('tenders')
})

async function loadReport(report: 'tenders' | 'sla' | 'users') {
  activeReport.value = report
  reportError.value = null
  try {
    switch (report) {
      case 'tenders': await dashboardStore.fetchTenderReport(); break
      case 'sla': await dashboardStore.fetchSlaReport(); break
      case 'users': await dashboardStore.fetchUserActivityReport(); break
    }
    if (dashboardStore.error) {
      reportError.value = dashboardStore.error
    }
  } catch (err: any) {
    reportError.value = err.message || 'Failed to load report'
  }
}

function formatDate(dateStr: string) {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit' })
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
</script>

<template>
  <div class="p-6 space-y-6">
    <!-- Header -->
    <div>
      <h1 class="text-2xl font-bold text-gray-900">{{ locale === 'ar' ? 'التقارير' : 'Reports' }}</h1>
      <p class="text-gray-500 mt-1">{{ locale === 'ar' ? 'تقارير شاملة عن المنافسات والأداء والمستخدمين' : 'Comprehensive reports on tenders, performance, and users' }}</p>
    </div>

    <!-- Report Tabs -->
    <div class="border-b border-gray-200">
      <nav class="flex gap-6">
        <button @click="loadReport('tenders')" class="pb-3 text-sm font-medium border-b-2 transition-colors"
          :class="activeReport === 'tenders' ? 'border-primary-500 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'">
          {{ locale === 'ar' ? 'تقرير حالة المنافسات' : 'Tender Status Report' }}
        </button>
        <button @click="loadReport('sla')" class="pb-3 text-sm font-medium border-b-2 transition-colors"
          :class="activeReport === 'sla' ? 'border-primary-500 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'">
          {{ locale === 'ar' ? 'تقرير امتثال SLA' : 'SLA Compliance Report' }}
        </button>
        <button @click="loadReport('users')" class="pb-3 text-sm font-medium border-b-2 transition-colors"
          :class="activeReport === 'users' ? 'border-primary-500 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'">
          {{ locale === 'ar' ? 'تقرير نشاط المستخدمين' : 'User Activity Report' }}
        </button>
      </nav>
    </div>

    <!-- Loading -->
    <div v-if="dashboardStore.isLoading" class="flex justify-center py-12">
      <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-primary-600"></div>
    </div>

    <!-- Error -->
    <div v-else-if="reportError || dashboardStore.error" class="bg-yellow-50 border border-yellow-200 rounded-xl p-8 text-center">
      <svg class="mx-auto h-12 w-12 text-yellow-400 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z" />
      </svg>
      <h3 class="text-lg font-medium text-yellow-800">{{ locale === 'ar' ? 'لا تتوفر بيانات كافية لعرض التقرير' : 'Insufficient data to display report' }}</h3>
      <p class="text-yellow-600 mt-2 text-sm">{{ locale === 'ar' ? 'سيتم توفير التقارير تلقائياً عند إضافة بيانات كافية في المنصة' : 'Reports will be available automatically once sufficient data is added to the platform' }}</p>
      <button @click="loadReport(activeReport)" class="mt-4 px-4 py-2 bg-yellow-100 text-yellow-800 rounded-lg hover:bg-yellow-200 text-sm font-medium">
        {{ locale === 'ar' ? 'إعادة المحاولة' : 'Retry' }}
      </button>
    </div>

    <!-- Tender Status Report -->
    <template v-else-if="activeReport === 'tenders' && dashboardStore.tenderReport">
      <div class="space-y-6">
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
          <div class="flex items-center justify-between mb-6">
            <h2 class="text-lg font-semibold text-gray-900">{{ locale === 'ar' ? 'تقرير حالة المنافسات' : 'Tender Status Report' }}</h2>
            <span class="text-xs text-gray-400">{{ formatDate(dashboardStore.tenderReport.generatedAt) }}</span>
          </div>
          <!-- Summary Cards -->
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
            <div class="p-4 bg-primary-50 rounded-lg text-center">
              <p class="text-3xl font-bold text-primary-700">{{ dashboardStore.tenderReport.totalTenders }}</p>
              <p class="text-sm text-gray-500">{{ locale === 'ar' ? 'إجمالي المنافسات' : 'Total Tenders' }}</p>
            </div>
            <div class="p-4 bg-green-50 rounded-lg text-center">
              <p class="text-2xl font-bold text-green-700">{{ formatCurrency(dashboardStore.tenderReport.totalEstimatedValue || 0) }}</p>
              <p class="text-sm text-gray-500">{{ locale === 'ar' ? 'القيمة التقديرية الإجمالية' : 'Total Estimated Value' }}</p>
            </div>
            <div class="p-4 bg-blue-50 rounded-lg text-center">
              <p class="text-3xl font-bold text-blue-700">{{ (dashboardStore.tenderReport.averageCompletionPercentage || 0).toFixed(0) }}%</p>
              <p class="text-sm text-gray-500">{{ locale === 'ar' ? 'متوسط نسبة الإنجاز' : 'Avg Completion' }}</p>
            </div>
          </div>
          <!-- Status Breakdown -->
          <h3 class="text-sm font-semibold text-gray-700 mb-3">{{ locale === 'ar' ? 'توزيع المنافسات حسب الحالة' : 'Tenders by Status' }}</h3>
          <div v-if="dashboardStore.tenderReport.statusBreakdown?.length" class="overflow-x-auto mb-6">
            <table class="w-full text-sm">
              <thead>
                <tr class="text-start text-gray-500 border-b">
                  <th class="pb-3 font-medium">{{ locale === 'ar' ? 'الحالة' : 'Status' }}</th>
                  <th class="pb-3 font-medium">{{ locale === 'ar' ? 'العدد' : 'Count' }}</th>
                  <th class="pb-3 font-medium">{{ locale === 'ar' ? 'القيمة' : 'Value' }}</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100">
                <tr v-for="item in dashboardStore.tenderReport.statusBreakdown" :key="item.status">
                  <td class="py-3"><span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium" :class="getStatusColor(item.status)">{{ getStatusLabel(item.status) }}</span></td>
                  <td class="py-3 font-medium text-gray-900">{{ item.count }}</td>
                  <td class="py-3 text-gray-600">{{ formatCurrency(item.totalValue) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
          <div v-else class="text-center py-4 text-gray-400 text-sm">{{ locale === 'ar' ? 'لا توجد بيانات' : 'No data available' }}</div>
          <!-- Monthly Trend -->
          <h3 class="text-sm font-semibold text-gray-700 mb-3">{{ locale === 'ar' ? 'الاتجاه الشهري' : 'Monthly Trend' }}</h3>
          <div v-if="dashboardStore.tenderReport.monthlyTrend?.length" class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="text-start text-gray-500 border-b">
                  <th class="pb-3 font-medium">{{ locale === 'ar' ? 'الشهر' : 'Month' }}</th>
                  <th class="pb-3 font-medium">{{ locale === 'ar' ? 'العدد' : 'Count' }}</th>
                  <th class="pb-3 font-medium">{{ locale === 'ar' ? 'القيمة' : 'Value' }}</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100">
                <tr v-for="item in dashboardStore.tenderReport.monthlyTrend" :key="`${item.year}-${item.month}`">
                  <td class="py-3 text-gray-900">{{ getMonthName(item.month) }} {{ item.year }}</td>
                  <td class="py-3 font-medium text-gray-900">{{ item.count }}</td>
                  <td class="py-3 text-gray-600">{{ formatCurrency(item.totalValue) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
          <div v-else class="text-center py-4 text-gray-400 text-sm">{{ locale === 'ar' ? 'لا توجد بيانات' : 'No data available' }}</div>
        </div>
      </div>
    </template>

    <!-- SLA Compliance Report -->
    <template v-else-if="activeReport === 'sla' && dashboardStore.slaReport">
      <div class="space-y-6">
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
          <div class="flex items-center justify-between mb-6">
            <h2 class="text-lg font-semibold text-gray-900">{{ locale === 'ar' ? 'تقرير امتثال SLA' : 'SLA Compliance Report' }}</h2>
            <span class="text-xs text-gray-400">{{ formatDate(dashboardStore.slaReport.generatedAt) }}</span>
          </div>
          <!-- Compliance Rate -->
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
          <!-- Task Metrics -->
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

    <!-- User Activity Report -->
    <template v-else-if="activeReport === 'users' && dashboardStore.userActivityReport">
      <div class="space-y-6">
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
          <div class="flex items-center justify-between mb-6">
            <h2 class="text-lg font-semibold text-gray-900">{{ locale === 'ar' ? 'تقرير نشاط المستخدمين' : 'User Activity Report' }}</h2>
            <span class="text-xs text-gray-400">{{ formatDate(dashboardStore.userActivityReport.generatedAt) }}</span>
          </div>
          <!-- Summary -->
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
          <!-- Actions by Category -->
          <template v-if="dashboardStore.userActivityReport.actionsByCategory?.length">
            <h3 class="text-sm font-semibold text-gray-700 mb-3">{{ locale === 'ar' ? 'الإجراءات حسب التصنيف' : 'Actions by Category' }}</h3>
            <div class="overflow-x-auto mb-6">
              <table class="w-full text-sm">
                <thead>
                  <tr class="text-start text-gray-500 border-b">
                    <th class="pb-3 font-medium">{{ locale === 'ar' ? 'التصنيف' : 'Category' }}</th>
                    <th class="pb-3 font-medium">{{ locale === 'ar' ? 'العدد' : 'Count' }}</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-gray-100">
                  <tr v-for="item in dashboardStore.userActivityReport.actionsByCategory" :key="item.category">
                    <td class="py-3 font-medium text-gray-900">{{ item.category }}</td>
                    <td class="py-3 text-gray-600">{{ item.count }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </template>
          <!-- Top Active Users -->
          <h3 class="text-sm font-semibold text-gray-700 mb-3">{{ locale === 'ar' ? 'أكثر المستخدمين نشاطاً' : 'Top Active Users' }}</h3>
          <div v-if="dashboardStore.userActivityReport.topActiveUsers?.length" class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="text-start text-gray-500 border-b">
                  <th class="pb-3 font-medium">#</th>
                  <th class="pb-3 font-medium">{{ locale === 'ar' ? 'المستخدم' : 'User' }}</th>
                  <th class="pb-3 font-medium">{{ locale === 'ar' ? 'عدد الإجراءات' : 'Actions' }}</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100">
                <tr v-for="(user, index) in dashboardStore.userActivityReport.topActiveUsers" :key="user.userId">
                  <td class="py-3 text-gray-400">{{ index + 1 }}</td>
                  <td class="py-3 font-medium text-gray-900">{{ user.userName || user.fullNameAr || user.fullNameEn || '-' }}</td>
                  <td class="py-3 text-gray-600">{{ user.actionCount }}</td>
                </tr>
              </tbody>
            </table>
          </div>
          <div v-else class="text-center py-4 text-gray-400 text-sm">{{ locale === 'ar' ? 'لا توجد بيانات نشاط بعد' : 'No activity data yet' }}</div>
        </div>
      </div>
    </template>

    <!-- No Data -->
    <div v-else class="bg-white rounded-xl shadow-sm border border-gray-200 p-12 text-center">
      <svg class="mx-auto h-16 w-16 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
      </svg>
      <h3 class="mt-4 text-lg font-medium text-gray-900">{{ locale === 'ar' ? 'لا توجد بيانات تقارير' : 'No Report Data' }}</h3>
      <p class="mt-2 text-gray-500">{{ locale === 'ar' ? 'ستتوفر التقارير عند إضافة بيانات كافية في المنصة' : 'Reports will be available when sufficient data is added' }}</p>
    </div>
  </div>
</template>
