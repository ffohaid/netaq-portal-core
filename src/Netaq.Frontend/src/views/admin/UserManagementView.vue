<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import api from '../../services/api'
import type { ApiResponse, SendInvitationRequest } from '../../types'

const { t } = useI18n()

const showInviteModal = ref(false)
const inviteForm = ref<SendInvitationRequest>({
  email: '',
  fullNameAr: '',
  fullNameEn: '',
  assignedRole: 'Coordinator',
})
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

async function sendInvitation() {
  isSubmitting.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const response = await api.post<ApiResponse<string>>('/invitation/send', inviteForm.value)
    if (response.data.isSuccess) {
      successMessage.value = t('invitation.invitationSent')
      showInviteModal.value = false
      inviteForm.value = { email: '', fullNameAr: '', fullNameEn: '', assignedRole: 'Coordinator' }
    } else {
      errorMessage.value = response.data.error || 'Failed to send invitation'
    }
  } catch (err: any) {
    errorMessage.value = err.response?.data?.error || 'An error occurred'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ t('nav.userManagement') }}</h1>
      </div>
      <button @click="showInviteModal = true" class="btn-primary">
        {{ t('invitation.title') }}
      </button>
    </div>

    <!-- Success Message -->
    <div v-if="successMessage" class="p-4 bg-green-50 border border-green-200 rounded-lg">
      <p class="text-green-700">{{ successMessage }}</p>
    </div>

    <!-- Placeholder for user list (fetched from API) -->
    <div class="card text-center py-12">
      <svg class="w-16 h-16 mx-auto mb-4 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
          d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
      </svg>
      <p class="text-gray-500">{{ t('invitation.title') }}</p>
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

            <div v-if="errorMessage" class="p-3 bg-danger-50 border border-danger-500/20 rounded-lg">
              <p class="text-sm text-danger-700">{{ errorMessage }}</p>
            </div>

            <div class="flex items-center justify-end gap-3 pt-2">
              <button type="button" @click="showInviteModal = false" class="btn-secondary">{{ t('common.cancel') }}</button>
              <button type="submit" :disabled="isSubmitting" class="btn-primary">
                {{ isSubmitting ? t('common.loading') : t('invitation.sendInvitation') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>
  </div>
</template>
