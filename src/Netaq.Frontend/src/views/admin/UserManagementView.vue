<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import api from '../../services/api'
import { getCurrentLocale } from '../../i18n'
import type { ApiResponse, SendInvitationRequest } from '../../types'

const { t } = useI18n({ useScope: 'global' })
const locale = computed(() => getCurrentLocale())
const isRtl = computed(() => locale.value === 'ar')

interface UserItem {
  id: string; fullNameAr: string; fullNameEn: string; email: string; phone?: string
  jobTitleAr?: string; jobTitleEn?: string; departmentAr?: string; departmentEn?: string
  role: string; status: string; lastLoginAt?: string; createdAt: string
}

const users = ref<UserItem[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = 20
const isLoadingUsers = ref(false)
const searchTerm = ref('')
const roleFilter = ref('')
const statusFilter = ref('')

// Modals
const showInviteModal = ref(false)
const showEditModal = ref(false)
const showDeleteConfirm = ref(false)
const showResetConfirm = ref(false)

const inviteForm = ref<SendInvitationRequest>({ email: '', fullNameAr: '', fullNameEn: '', assignedRole: 'Coordinator' })
const editForm = ref<any>({})
const selectedUser = ref<UserItem | null>(null)

const isSubmitting = ref(false)
const successMessage = ref('')
const errorMessage = ref('')

const roles = [
  { value: 'OrganizationAdmin', label: 'roles.OrganizationAdmin' },
  { value: 'DepartmentManager', label: 'roles.DepartmentManager' },
  { value: 'Coordinator', label: 'roles.Coordinator' },
  { value: 'CommitteeChair', label: 'roles.CommitteeChair' },
  { value: 'CommitteeMember', label: 'roles.CommitteeMember' },
  { value: 'Viewer', label: 'roles.Viewer' },
]

const userStatuses = [
  { value: 'Active', labelAr: 'نشط', labelEn: 'Active' },
  { value: 'Invited', labelAr: 'مدعو', labelEn: 'Invited' },
  { value: 'Suspended', labelAr: 'معلق', labelEn: 'Suspended' },
  { value: 'Deactivated', labelAr: 'معطل', labelEn: 'Deactivated' },
]

async function loadUsers() {
  isLoadingUsers.value = true
  try {
    const params: Record<string, any> = { pageNumber: currentPage.value, pageSize }
    if (searchTerm.value) params.search = searchTerm.value
    if (roleFilter.value) params.role = roleFilter.value
    if (statusFilter.value) params.status = statusFilter.value
    const response = await api.get('/users', { params })
    if (response.data.isSuccess) {
      users.value = response.data.data.users
      totalCount.value = response.data.data.totalCount
    }
  } catch (err: any) {
    console.error('Failed to load users:', err)
  } finally {
    isLoadingUsers.value = false
  }
}

async function sendInvitation() {
  isSubmitting.value = true; errorMessage.value = ''
  try {
    const response = await api.post<ApiResponse<string>>('/invitation/send', inviteForm.value)
    if (response.data.isSuccess) {
      successMessage.value = t('invitation.invitationSent')
      showInviteModal.value = false
      inviteForm.value = { email: '', fullNameAr: '', fullNameEn: '', assignedRole: 'Coordinator' }
      await loadUsers()
    } else {
      errorMessage.value = response.data.error || 'Failed to send invitation'
    }
  } catch (err: any) {
    errorMessage.value = err.response?.data?.error || 'An error occurred'
  } finally {
    isSubmitting.value = false
  }
}

function openEditModal(user: UserItem) {
  selectedUser.value = user
  editForm.value = {
    fullNameAr: user.fullNameAr, fullNameEn: user.fullNameEn,
    phone: user.phone || '', jobTitleAr: user.jobTitleAr || '', jobTitleEn: user.jobTitleEn || '',
    departmentAr: user.departmentAr || '', departmentEn: user.departmentEn || '',
    role: user.role, status: user.status,
  }
  showEditModal.value = true
}

async function saveEditUser() {
  if (!selectedUser.value) return
  isSubmitting.value = true; errorMessage.value = ''
  try {
    const res = await api.put(`/users/${selectedUser.value.id}`, editForm.value)
    if (res.data.isSuccess) {
      successMessage.value = isRtl.value ? 'تم تحديث بيانات المستخدم بنجاح' : 'User updated successfully'
      showEditModal.value = false
      await loadUsers()
    } else {
      errorMessage.value = res.data.message || 'Failed to update user'
    }
  } catch (err: any) {
    errorMessage.value = err.response?.data?.message || 'An error occurred'
  } finally {
    isSubmitting.value = false
  }
}

function openResetConfirm(user: UserItem) {
  selectedUser.value = user
  showResetConfirm.value = true
}

async function resetUserPassword() {
  if (!selectedUser.value) return
  isSubmitting.value = true; errorMessage.value = ''
  try {
    const res = await api.post(`/users/${selectedUser.value.id}/reset-password`)
    if (res.data.isSuccess) {
      successMessage.value = isRtl.value ? 'تم إعادة تعيين كلمة المرور وإرسالها عبر البريد' : 'Password reset and sent via email'
      showResetConfirm.value = false
    } else {
      errorMessage.value = res.data.message || 'Failed to reset password'
    }
  } catch (err: any) {
    errorMessage.value = err.response?.data?.message || 'An error occurred'
  } finally {
    isSubmitting.value = false
  }
}

function openDeleteConfirm(user: UserItem) {
  selectedUser.value = user
  showDeleteConfirm.value = true
}

async function deleteUser() {
  if (!selectedUser.value) return
  isSubmitting.value = true; errorMessage.value = ''
  try {
    const res = await api.delete(`/users/${selectedUser.value.id}`)
    if (res.data.isSuccess) {
      successMessage.value = isRtl.value ? 'تم حذف المستخدم بنجاح' : 'User deleted successfully'
      showDeleteConfirm.value = false
      await loadUsers()
    } else {
      errorMessage.value = res.data.message || 'Failed to delete user'
    }
  } catch (err: any) {
    errorMessage.value = err.response?.data?.message || 'An error occurred'
  } finally {
    isSubmitting.value = false
  }
}

async function updateUserStatus(userId: string, newStatus: string) {
  try {
    await api.put(`/users/${userId}/status`, { status: newStatus })
    await loadUsers()
  } catch (err: any) {
    console.error('Failed to update status:', err)
  }
}

function getUserName(user: UserItem) { return locale.value === 'ar' ? user.fullNameAr : user.fullNameEn }
function getUserJobTitle(user: UserItem) { return locale.value === 'ar' ? (user.jobTitleAr || '-') : (user.jobTitleEn || '-') }
function formatDate(dateStr?: string) {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })
}
function getStatusClass(status: string) {
  switch (status) {
    case 'Active': return 'bg-green-100 text-green-700'
    case 'Invited': return 'bg-yellow-100 text-yellow-700'
    case 'Suspended': return 'bg-red-100 text-red-700'
    case 'Deactivated': return 'bg-gray-100 text-gray-700'
    default: return 'bg-gray-100 text-gray-700'
  }
}
function getStatusLabel(status: string) {
  const found = userStatuses.find(s => s.value === status)
  if (!found) return status
  return locale.value === 'ar' ? found.labelAr : found.labelEn
}

const totalPages = computed(() => Math.ceil(totalCount.value / pageSize))

watch([roleFilter, statusFilter], () => { currentPage.value = 1; loadUsers() })
let searchTimeout: ReturnType<typeof setTimeout> | null = null
watch(searchTerm, () => {
  if (searchTimeout) clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => { currentPage.value = 1; loadUsers() }, 500)
})
onMounted(() => { loadUsers() })
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ t('nav.userManagement') }}</h1>
        <p class="text-sm text-gray-500 mt-1">{{ totalCount }} {{ isRtl ? 'مستخدم' : 'users' }}</p>
      </div>
      <button @click="showInviteModal = true" class="btn-primary flex items-center gap-2">
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" /></svg>
        {{ t('invitation.title') }}
      </button>
    </div>

    <!-- Success/Error Messages -->
    <div v-if="successMessage" class="p-4 bg-green-50 border border-green-200 rounded-lg flex items-center justify-between">
      <p class="text-green-700 text-sm">{{ successMessage }}</p>
      <button @click="successMessage = ''" class="text-green-500 hover:text-green-700"><svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg></button>
    </div>

    <!-- Filters -->
    <div class="card">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <input v-model="searchTerm" type="text" :placeholder="t('common.search')" class="input-field" />
        <select v-model="roleFilter" class="input-field">
          <option value="">{{ isRtl ? 'جميع الأدوار' : 'All Roles' }}</option>
          <option v-for="role in roles" :key="role.value" :value="role.value">{{ t(role.label) }}</option>
        </select>
        <select v-model="statusFilter" class="input-field">
          <option value="">{{ isRtl ? 'جميع الحالات' : 'All Statuses' }}</option>
          <option v-for="s in userStatuses" :key="s.value" :value="s.value">{{ isRtl ? s.labelAr : s.labelEn }}</option>
        </select>
      </div>
    </div>

    <!-- Users Table -->
    <div class="card">
      <div v-if="isLoadingUsers" class="flex items-center justify-center py-12">
        <svg class="animate-spin w-8 h-8 text-primary-500" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" /><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" /></svg>
      </div>
      <div v-else-if="users.length === 0" class="text-center py-12">
        <svg class="w-16 h-16 mx-auto mb-4 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" /></svg>
        <p class="text-gray-500">{{ t('common.noData') }}</p>
      </div>
      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="text-start text-gray-500 border-b">
              <th class="pb-3 font-medium">{{ isRtl ? 'الاسم' : 'Name' }}</th>
              <th class="pb-3 font-medium">{{ t('auth.email') }}</th>
              <th class="pb-3 font-medium">{{ isRtl ? 'المسمى الوظيفي' : 'Job Title' }}</th>
              <th class="pb-3 font-medium">{{ t('invitation.role') }}</th>
              <th class="pb-3 font-medium">{{ t('common.status') }}</th>
              <th class="pb-3 font-medium">{{ isRtl ? 'آخر دخول' : 'Last Login' }}</th>
              <th class="pb-3 font-medium">{{ t('common.actions') }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-for="user in users" :key="user.id" class="hover:bg-gray-50">
              <td class="py-3">
                <div class="flex items-center gap-3">
                  <div class="w-8 h-8 bg-primary-100 rounded-full flex items-center justify-center text-primary-700 font-bold text-xs">{{ getUserName(user).charAt(0) }}</div>
                  <span class="font-medium text-gray-900">{{ getUserName(user) }}</span>
                </div>
              </td>
              <td class="py-3 text-gray-500" dir="ltr">{{ user.email }}</td>
              <td class="py-3 text-gray-500">{{ getUserJobTitle(user) }}</td>
              <td class="py-3"><span class="text-xs px-2 py-0.5 rounded-full bg-primary-50 text-primary-700">{{ t(`roles.${user.role}`) }}</span></td>
              <td class="py-3"><span class="text-xs px-2 py-0.5 rounded-full" :class="getStatusClass(user.status)">{{ getStatusLabel(user.status) }}</span></td>
              <td class="py-3 text-gray-500">{{ formatDate(user.lastLoginAt) }}</td>
              <td class="py-3">
                <div class="flex items-center gap-1">
                  <!-- Edit -->
                  <button @click="openEditModal(user)" class="p-1.5 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors" :title="isRtl ? 'تعديل' : 'Edit'">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
                  </button>
                  <!-- Reset Password -->
                  <button @click="openResetConfirm(user)" class="p-1.5 text-amber-600 hover:bg-amber-50 rounded-lg transition-colors" :title="isRtl ? 'إعادة تعيين كلمة المرور' : 'Reset Password'">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" /></svg>
                  </button>
                  <!-- Suspend/Activate -->
                  <button v-if="user.status === 'Active'" @click="updateUserStatus(user.id, 'Suspended')" class="p-1.5 text-orange-600 hover:bg-orange-50 rounded-lg transition-colors" :title="isRtl ? 'تعليق' : 'Suspend'">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" /></svg>
                  </button>
                  <button v-if="user.status === 'Suspended'" @click="updateUserStatus(user.id, 'Active')" class="p-1.5 text-green-600 hover:bg-green-50 rounded-lg transition-colors" :title="isRtl ? 'تفعيل' : 'Activate'">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                  </button>
                  <!-- Delete -->
                  <button @click="openDeleteConfirm(user)" class="p-1.5 text-red-600 hover:bg-red-50 rounded-lg transition-colors" :title="isRtl ? 'حذف' : 'Delete'">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <!-- Pagination -->
      <div v-if="totalPages > 1" class="flex items-center justify-between mt-4 pt-4 border-t">
        <p class="text-sm text-gray-500">{{ isRtl ? 'صفحة' : 'Page' }} {{ currentPage }} {{ isRtl ? 'من' : 'of' }} {{ totalPages }}</p>
        <div class="flex items-center gap-2">
          <button @click="currentPage--; loadUsers()" :disabled="currentPage <= 1" class="px-3 py-1 text-sm border rounded-lg disabled:opacity-50">{{ t('common.previous') }}</button>
          <button @click="currentPage++; loadUsers()" :disabled="currentPage >= totalPages" class="px-3 py-1 text-sm border rounded-lg disabled:opacity-50">{{ t('common.next') }}</button>
        </div>
      </div>
    </div>

    <!-- Invite Modal -->
    <Teleport to="body">
      <div v-if="showInviteModal" class="fixed inset-0 z-50 flex items-center justify-center">
        <div class="fixed inset-0 bg-black/50" @click="showInviteModal = false"></div>
        <div class="relative bg-white rounded-2xl shadow-xl w-full max-w-md p-6 z-10">
          <h3 class="text-lg font-semibold text-gray-900 mb-4">{{ t('invitation.title') }}</h3>
          <form @submit.prevent="sendInvitation" class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('auth.email') }}</label>
              <input v-model="inviteForm.email" type="email" required class="input-field" dir="ltr" />
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('auth.fullNameAr') }}</label>
                <input v-model="inviteForm.fullNameAr" type="text" class="input-field" dir="rtl" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('auth.fullNameEn') }}</label>
                <input v-model="inviteForm.fullNameEn" type="text" class="input-field" dir="ltr" />
              </div>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('invitation.role') }}</label>
              <select v-model="inviteForm.assignedRole" class="input-field">
                <option v-for="role in roles" :key="role.value" :value="role.value">{{ t(role.label) }}</option>
              </select>
            </div>
            <div v-if="errorMessage" class="p-3 bg-red-50 border border-red-200 rounded-lg"><p class="text-sm text-red-700">{{ errorMessage }}</p></div>
            <div class="flex items-center justify-end gap-3 pt-2">
              <button type="button" @click="showInviteModal = false" class="btn-secondary">{{ t('common.cancel') }}</button>
              <button type="submit" :disabled="isSubmitting" class="btn-primary">{{ isSubmitting ? t('common.loading') : t('invitation.sendInvitation') }}</button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- Edit User Modal -->
    <Teleport to="body">
      <div v-if="showEditModal" class="fixed inset-0 z-50 flex items-center justify-center">
        <div class="fixed inset-0 bg-black/50" @click="showEditModal = false"></div>
        <div class="relative bg-white rounded-2xl shadow-xl w-full max-w-lg p-6 z-10 max-h-[90vh] overflow-y-auto">
          <h3 class="text-lg font-semibold text-gray-900 mb-4">{{ isRtl ? 'تعديل بيانات المستخدم' : 'Edit User' }}</h3>
          <form @submit.prevent="saveEditUser" class="space-y-4">
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ isRtl ? 'الاسم (عربي)' : 'Name (Arabic)' }}</label>
                <input v-model="editForm.fullNameAr" type="text" class="input-field" dir="rtl" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ isRtl ? 'الاسم (إنجليزي)' : 'Name (English)' }}</label>
                <input v-model="editForm.fullNameEn" type="text" class="input-field" dir="ltr" />
              </div>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ isRtl ? 'رقم الهاتف' : 'Phone' }}</label>
              <input v-model="editForm.phone" type="tel" class="input-field" dir="ltr" />
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ isRtl ? 'المسمى الوظيفي (عربي)' : 'Job Title (Ar)' }}</label>
                <input v-model="editForm.jobTitleAr" type="text" class="input-field" dir="rtl" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ isRtl ? 'المسمى الوظيفي (إنجليزي)' : 'Job Title (En)' }}</label>
                <input v-model="editForm.jobTitleEn" type="text" class="input-field" dir="ltr" />
              </div>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ isRtl ? 'الإدارة (عربي)' : 'Department (Ar)' }}</label>
                <input v-model="editForm.departmentAr" type="text" class="input-field" dir="rtl" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ isRtl ? 'الإدارة (إنجليزي)' : 'Department (En)' }}</label>
                <input v-model="editForm.departmentEn" type="text" class="input-field" dir="ltr" />
              </div>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('invitation.role') }}</label>
                <select v-model="editForm.role" class="input-field">
                  <option v-for="role in roles" :key="role.value" :value="role.value">{{ t(role.label) }}</option>
                </select>
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('common.status') }}</label>
                <select v-model="editForm.status" class="input-field">
                  <option v-for="s in userStatuses" :key="s.value" :value="s.value">{{ isRtl ? s.labelAr : s.labelEn }}</option>
                </select>
              </div>
            </div>
            <div v-if="errorMessage" class="p-3 bg-red-50 border border-red-200 rounded-lg"><p class="text-sm text-red-700">{{ errorMessage }}</p></div>
            <div class="flex items-center justify-end gap-3 pt-2">
              <button type="button" @click="showEditModal = false" class="btn-secondary">{{ t('common.cancel') }}</button>
              <button type="submit" :disabled="isSubmitting" class="btn-primary">{{ isSubmitting ? t('common.loading') : t('common.save') }}</button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- Reset Password Confirmation -->
    <Teleport to="body">
      <div v-if="showResetConfirm" class="fixed inset-0 z-50 flex items-center justify-center">
        <div class="fixed inset-0 bg-black/50" @click="showResetConfirm = false"></div>
        <div class="relative bg-white rounded-2xl shadow-xl w-full max-w-sm p-6 z-10 text-center">
          <div class="w-14 h-14 bg-amber-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <svg class="w-7 h-7 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" /></svg>
          </div>
          <h3 class="text-lg font-semibold text-gray-900 mb-2">{{ isRtl ? 'إعادة تعيين كلمة المرور' : 'Reset Password' }}</h3>
          <p class="text-sm text-gray-500 mb-6">
            {{ isRtl ? `سيتم إعادة تعيين كلمة مرور "${selectedUser?.fullNameAr}" وإرسال كلمة مرور مؤقتة عبر البريد الإلكتروني` : `Password for "${selectedUser?.fullNameEn}" will be reset and a temporary password sent via email` }}
          </p>
          <div class="flex items-center justify-center gap-3">
            <button @click="showResetConfirm = false" class="btn-secondary">{{ t('common.cancel') }}</button>
            <button @click="resetUserPassword" :disabled="isSubmitting" class="px-4 py-2 bg-amber-600 text-white rounded-lg hover:bg-amber-700 disabled:opacity-50">
              {{ isSubmitting ? t('common.loading') : (isRtl ? 'إعادة التعيين' : 'Reset') }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Delete Confirmation -->
    <Teleport to="body">
      <div v-if="showDeleteConfirm" class="fixed inset-0 z-50 flex items-center justify-center">
        <div class="fixed inset-0 bg-black/50" @click="showDeleteConfirm = false"></div>
        <div class="relative bg-white rounded-2xl shadow-xl w-full max-w-sm p-6 z-10 text-center">
          <div class="w-14 h-14 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <svg class="w-7 h-7 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
          </div>
          <h3 class="text-lg font-semibold text-gray-900 mb-2">{{ isRtl ? 'حذف المستخدم' : 'Delete User' }}</h3>
          <p class="text-sm text-gray-500 mb-6">
            {{ isRtl ? `هل أنت متأكد من حذف "${selectedUser?.fullNameAr}"؟ لا يمكن التراجع عن هذا الإجراء.` : `Are you sure you want to delete "${selectedUser?.fullNameEn}"? This action cannot be undone.` }}
          </p>
          <div class="flex items-center justify-center gap-3">
            <button @click="showDeleteConfirm = false" class="btn-secondary">{{ t('common.cancel') }}</button>
            <button @click="deleteUser" :disabled="isSubmitting" class="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50">
              {{ isSubmitting ? t('common.loading') : (isRtl ? 'حذف' : 'Delete') }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
