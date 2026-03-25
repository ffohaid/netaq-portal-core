<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useInquiryStore } from '../../stores/inquiries'
import { getCurrentLocale } from '../../i18n'
import api from '../../services/api'
import type { ApiResponse, PaginatedResponse } from '../../types'

const { t } = useI18n()
const router = useRouter()
const store = useInquiryStore()
const statusFilter = ref('')
const categoryFilter = ref('')
const tenderFilter = ref('')
const searchTerm = ref('')
const currentPage = ref(1)
const pageSize = 20
const locale = computed(() => getCurrentLocale())

const showCreateModal = ref(false)
const availableTenders = ref<any[]>([])
const availableUsers = ref<any[]>([])
const isCreating = ref(false)
const createForm = ref({
  tenderId: '',
  subjectAr: '',
  subjectEn: '',
  questionAr: '',
  questionEn: '',
  priority: 'Normal',
  category: 'General',
  assignedToUserId: '',
})

async function loadTenders() {
  try {
    const response = await api.get<ApiResponse<PaginatedResponse<any>>>('/tenders?pageSize=100')
    if (response.data.isSuccess && response.data.data) {
      availableTenders.value = response.data.data.items
    }
  } catch { /* silent */ }
}

async function loadUsers() {
  try {
    const response = await api.get<ApiResponse<PaginatedResponse<any>>>('/users?pageSize=100')
    if (response.data.isSuccess && response.data.data) {
      availableUsers.value = response.data.data.items
    }
  } catch { /* silent */ }
}

async function loadInquiries() {
  await store.fetchInquiries({
    pageNumber: currentPage.value,
    pageSize,
    tenderId: tenderFilter.value || undefined,
    status: statusFilter.value || undefined,
    category: categoryFilter.value || undefined,
    search: searchTerm.value || undefined,
  })
}

function getSubject(item: any) {
  return locale.value === 'ar' ? item.subjectAr : item.subjectEn
}
function getTenderTitle(item: any) {
  return locale.value === 'ar' ? item.tenderTitleAr : item.tenderTitleEn
}
function getSubmitter(item: any) {
  return locale.value === 'ar' ? (item.submittedByUserNameAr || item.submittedByNameAr) : (item.submittedByUserNameEn || item.submittedByNameEn)
}
function getAssignee(item: any) {
  if (!item.assignedToUserNameAr && !item.assignedToUserNameEn) {
    return locale.value === 'ar' ? 'غير مسند' : 'Unassigned'
  }
  return locale.value === 'ar' ? item.assignedToUserNameAr : item.assignedToUserNameEn
}
function formatDate(dateStr: string) {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })
}

function getStatusClass(status: string) {
  const map: Record<string, string> = {
    Submitted: 'bg-blue-100 text-blue-700',
    UnderReview: 'bg-yellow-100 text-yellow-700',
    Responded: 'bg-green-100 text-green-700',
    Closed: 'bg-gray-100 text-gray-700',
    Escalated: 'bg-red-100 text-red-700',
  }
  return map[status] || 'bg-gray-100 text-gray-700'
}
function getStatusLabel(status: string) {
  if (locale.value === 'ar') {
    const m: Record<string, string> = { Submitted: 'جديد', UnderReview: 'قيد المراجعة', Responded: 'تمت الإجابة', Closed: 'مغلق', Escalated: 'مُصعّد' }
    return m[status] || status
  }
  const m: Record<string, string> = { Submitted: 'New', UnderReview: 'Under Review', Responded: 'Responded', Closed: 'Closed', Escalated: 'Escalated' }
  return m[status] || status
}
function getPriorityClass(priority: string) {
  const map: Record<string, string> = { Low: 'bg-blue-100 text-blue-700', Normal: 'bg-gray-100 text-gray-700', High: 'bg-orange-100 text-orange-700', Urgent: 'bg-red-100 text-red-700' }
  return map[priority] || 'bg-gray-100 text-gray-700'
}
function getPriorityLabel(priority: string) {
  if (locale.value === 'ar') {
    const m: Record<string, string> = { Low: 'منخفضة', Normal: 'عادية', High: 'عالية', Urgent: 'عاجلة' }
    return m[priority] || priority
  }
  return priority
}
function getCategoryLabel(category: string) {
  if (locale.value === 'ar') {
    const m: Record<string, string> = { General: 'عام', Technical: 'فني', Financial: 'مالي', Legal: 'قانوني', Administrative: 'إداري', Clarification: 'توضيح' }
    return m[category] || category
  }
  return category
}

async function handleCreate() {
  if (!createForm.value.tenderId || !createForm.value.subjectAr || !createForm.value.questionAr) return
  isCreating.value = true
  const id = await store.createInquiry({
    tenderId: createForm.value.tenderId,
    subjectAr: createForm.value.subjectAr,
    subjectEn: createForm.value.subjectEn || createForm.value.subjectAr,
    questionAr: createForm.value.questionAr,
    questionEn: createForm.value.questionEn || createForm.value.questionAr,
    priority: createForm.value.priority,
    category: createForm.value.category,
    assignedToUserId: createForm.value.assignedToUserId || undefined,
  })
  isCreating.value = false
  if (id) {
    showCreateModal.value = false
    resetForm()
    await loadInquiries()
  }
}
function resetForm() {
  createForm.value = { tenderId: '', subjectAr: '', subjectEn: '', questionAr: '', questionEn: '', priority: 'Normal', category: 'General', assignedToUserId: '' }
}

let searchTimeout: any = null
watch(searchTerm, () => { clearTimeout(searchTimeout); searchTimeout = setTimeout(() => { currentPage.value = 1; loadInquiries() }, 400) })
watch([statusFilter, categoryFilter, tenderFilter], () => { currentPage.value = 1; loadInquiries() })

onMounted(() => { loadInquiries(); loadTenders(); loadUsers() })
</script>

<template>
  <div class="p-6">
    <!-- Header -->
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ locale === 'ar' ? 'الاستفسارات' : 'Inquiries' }}</h1>
        <p class="text-gray-500 mt-1">{{ locale === 'ar' ? 'إدارة استفسارات المنافسات - استلام الأسئلة من منصة اعتماد وتوزيعها على المختصين للإجابة' : 'Manage tender inquiries - receive questions from Etimad platform and assign to specialists' }}</p>
      </div>
      <button @click="showCreateModal = true" class="px-4 py-2.5 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors text-sm font-medium flex items-center gap-2">
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" /></svg>
        {{ locale === 'ar' ? 'إضافة استفسار' : 'Add Inquiry' }}
      </button>
    </div>

    <!-- Filters -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4 mb-6">
      <div class="grid grid-cols-1 md:grid-cols-5 gap-3">
        <div class="md:col-span-2">
          <input v-model="searchTerm" type="text" :placeholder="locale === 'ar' ? 'بحث في الاستفسارات...' : 'Search inquiries...'"
            class="w-full px-4 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500 focus:border-primary-500" />
        </div>
        <select v-model="tenderFilter" class="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500">
          <option value="">{{ locale === 'ar' ? 'جميع المنافسات' : 'All Tenders' }}</option>
          <option v-for="tender in availableTenders" :key="tender.id" :value="tender.id">{{ locale === 'ar' ? tender.titleAr : tender.titleEn }}</option>
        </select>
        <select v-model="statusFilter" class="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500">
          <option value="">{{ locale === 'ar' ? 'جميع الحالات' : 'All Statuses' }}</option>
          <option value="Submitted">{{ locale === 'ar' ? 'جديد' : 'New' }}</option>
          <option value="UnderReview">{{ locale === 'ar' ? 'قيد المراجعة' : 'Under Review' }}</option>
          <option value="Responded">{{ locale === 'ar' ? 'تمت الإجابة' : 'Responded' }}</option>
          <option value="Closed">{{ locale === 'ar' ? 'مغلق' : 'Closed' }}</option>
          <option value="Escalated">{{ locale === 'ar' ? 'مُصعّد' : 'Escalated' }}</option>
        </select>
        <select v-model="categoryFilter" class="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500">
          <option value="">{{ locale === 'ar' ? 'جميع التصنيفات' : 'All Categories' }}</option>
          <option value="General">{{ locale === 'ar' ? 'عام' : 'General' }}</option>
          <option value="Technical">{{ locale === 'ar' ? 'فني' : 'Technical' }}</option>
          <option value="Financial">{{ locale === 'ar' ? 'مالي' : 'Financial' }}</option>
          <option value="Legal">{{ locale === 'ar' ? 'قانوني' : 'Legal' }}</option>
          <option value="Administrative">{{ locale === 'ar' ? 'إداري' : 'Administrative' }}</option>
          <option value="Clarification">{{ locale === 'ar' ? 'توضيح' : 'Clarification' }}</option>
        </select>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="store.isLoading" class="flex justify-center py-12">
      <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-primary-600"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="store.inquiries.length === 0" class="bg-white rounded-xl shadow-sm border border-gray-200 p-12 text-center">
      <svg class="mx-auto h-16 w-16 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
      <h3 class="mt-4 text-lg font-medium text-gray-900">{{ locale === 'ar' ? 'لا توجد استفسارات' : 'No Inquiries' }}</h3>
      <p class="mt-2 text-gray-500">{{ locale === 'ar' ? 'لم يتم إضافة أي استفسارات بعد. يمكنك إضافة استفسار جديد من منصة اعتماد.' : 'No inquiries added yet. You can add a new inquiry from Etimad platform.' }}</p>
    </div>

    <!-- Inquiries Table -->
    <div v-else class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
      <table class="w-full">
        <thead class="bg-gray-50 border-b border-gray-200">
          <tr>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ locale === 'ar' ? 'الموضوع' : 'Subject' }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ locale === 'ar' ? 'المنافسة' : 'Tender' }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ locale === 'ar' ? 'التصنيف' : 'Category' }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ locale === 'ar' ? 'المسند إليه' : 'Assigned To' }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ locale === 'ar' ? 'الأولوية' : 'Priority' }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('common.status') }}</th>
            <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ locale === 'ar' ? 'التاريخ' : 'Date' }}</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-100">
          <tr v-for="inquiry in store.inquiries" :key="inquiry.id" class="hover:bg-gray-50 transition-colors cursor-pointer" @click="router.push(`/inquiries/${inquiry.id}`)">
            <td class="px-4 py-3">
              <div class="text-sm font-medium text-gray-900">{{ getSubject(inquiry) }}</div>
              <div class="text-xs text-gray-400">{{ getSubmitter(inquiry) }}</div>
            </td>
            <td class="px-4 py-3 text-sm text-gray-600">{{ getTenderTitle(inquiry) }}</td>
            <td class="px-4 py-3"><span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-700">{{ getCategoryLabel(inquiry.category) }}</span></td>
            <td class="px-4 py-3 text-sm text-gray-600">{{ getAssignee(inquiry) }}</td>
            <td class="px-4 py-3"><span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium" :class="getPriorityClass(inquiry.priority)">{{ getPriorityLabel(inquiry.priority) }}</span></td>
            <td class="px-4 py-3"><span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium" :class="getStatusClass(inquiry.status)">{{ getStatusLabel(inquiry.status) }}</span></td>
            <td class="px-4 py-3 text-sm text-gray-600">{{ formatDate(inquiry.createdAt) }}</td>
          </tr>
        </tbody>
      </table>
      <div v-if="store.totalPages > 1" class="flex items-center justify-between px-4 py-3 border-t border-gray-200">
        <div class="text-sm text-gray-500">{{ store.totalCount }} {{ locale === 'ar' ? 'استفسار' : 'inquiries' }}</div>
        <div class="flex gap-2">
          <button @click="currentPage--; loadInquiries()" :disabled="currentPage <= 1" class="px-3 py-1.5 text-sm border border-gray-300 rounded-lg disabled:opacity-50 hover:bg-gray-50">{{ t('common.previous') }}</button>
          <span class="px-3 py-1.5 text-sm text-gray-600">{{ currentPage }} / {{ store.totalPages }}</span>
          <button @click="currentPage++; loadInquiries()" :disabled="currentPage >= store.totalPages" class="px-3 py-1.5 text-sm border border-gray-300 rounded-lg disabled:opacity-50 hover:bg-gray-50">{{ t('common.next') }}</button>
        </div>
      </div>
    </div>

    <!-- Create Inquiry Modal -->
    <div v-if="showCreateModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50" @click.self="showCreateModal = false">
      <div class="bg-white rounded-xl shadow-xl w-full max-w-2xl max-h-[90vh] overflow-y-auto m-4">
        <div class="p-6 border-b border-gray-200">
          <h2 class="text-lg font-bold text-gray-900">{{ locale === 'ar' ? 'إضافة استفسار جديد' : 'Add New Inquiry' }}</h2>
          <p class="text-sm text-gray-500 mt-1">{{ locale === 'ar' ? 'أدخل بيانات الاستفسار المستلم من منصة اعتماد' : 'Enter inquiry details received from Etimad platform' }}</p>
        </div>
        <div class="p-6 space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ locale === 'ar' ? 'المنافسة' : 'Tender' }} *</label>
            <select v-model="createForm.tenderId" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500">
              <option value="">{{ locale === 'ar' ? '-- اختر المنافسة --' : '-- Select Tender --' }}</option>
              <option v-for="tender in availableTenders" :key="tender.id" :value="tender.id">{{ locale === 'ar' ? tender.titleAr : tender.titleEn }}</option>
            </select>
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ locale === 'ar' ? 'الموضوع (عربي)' : 'Subject (Arabic)' }} *</label>
              <input v-model="createForm.subjectAr" type="text" dir="rtl" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ locale === 'ar' ? 'الموضوع (إنجليزي)' : 'Subject (English)' }}</label>
              <input v-model="createForm.subjectEn" type="text" dir="ltr" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500" />
            </div>
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ locale === 'ar' ? 'نص السؤال (عربي)' : 'Question (Arabic)' }} *</label>
              <textarea v-model="createForm.questionAr" rows="4" dir="rtl" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500"></textarea>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ locale === 'ar' ? 'نص السؤال (إنجليزي)' : 'Question (English)' }}</label>
              <textarea v-model="createForm.questionEn" rows="4" dir="ltr" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500"></textarea>
            </div>
          </div>
          <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ locale === 'ar' ? 'التصنيف' : 'Category' }}</label>
              <select v-model="createForm.category" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500">
                <option value="General">{{ locale === 'ar' ? 'عام' : 'General' }}</option>
                <option value="Technical">{{ locale === 'ar' ? 'فني' : 'Technical' }}</option>
                <option value="Financial">{{ locale === 'ar' ? 'مالي' : 'Financial' }}</option>
                <option value="Legal">{{ locale === 'ar' ? 'قانوني' : 'Legal' }}</option>
                <option value="Administrative">{{ locale === 'ar' ? 'إداري' : 'Administrative' }}</option>
                <option value="Clarification">{{ locale === 'ar' ? 'توضيح' : 'Clarification' }}</option>
              </select>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ locale === 'ar' ? 'الأولوية' : 'Priority' }}</label>
              <select v-model="createForm.priority" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500">
                <option value="Low">{{ locale === 'ar' ? 'منخفضة' : 'Low' }}</option>
                <option value="Normal">{{ locale === 'ar' ? 'عادية' : 'Normal' }}</option>
                <option value="High">{{ locale === 'ar' ? 'عالية' : 'High' }}</option>
                <option value="Urgent">{{ locale === 'ar' ? 'عاجلة' : 'Urgent' }}</option>
              </select>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ locale === 'ar' ? 'إسناد إلى' : 'Assign To' }}</label>
              <select v-model="createForm.assignedToUserId" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500">
                <option value="">{{ locale === 'ar' ? '-- اختياري --' : '-- Optional --' }}</option>
                <option v-for="user in availableUsers" :key="user.id" :value="user.id">{{ locale === 'ar' ? user.fullNameAr : user.fullNameEn }}</option>
              </select>
            </div>
          </div>
          <div v-if="store.error" class="bg-red-50 border border-red-200 rounded-lg p-3">
            <p class="text-red-700 text-sm">{{ store.error }}</p>
          </div>
        </div>
        <div class="p-6 border-t border-gray-200 flex justify-end gap-3">
          <button @click="showCreateModal = false; resetForm()" class="px-4 py-2 border border-gray-300 rounded-lg text-sm font-medium hover:bg-gray-50">{{ t('common.cancel') }}</button>
          <button @click="handleCreate" :disabled="isCreating || !createForm.tenderId || !createForm.subjectAr || !createForm.questionAr"
            class="px-4 py-2 bg-primary-600 text-white rounded-lg text-sm font-medium hover:bg-primary-700 disabled:opacity-50 flex items-center gap-2">
            <div v-if="isCreating" class="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
            {{ locale === 'ar' ? 'إضافة الاستفسار' : 'Add Inquiry' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
