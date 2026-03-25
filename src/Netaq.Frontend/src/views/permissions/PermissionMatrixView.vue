<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { usePermissionStore, type PermissionMatrix, type UpdatePermissionEntry } from '../../stores/permissions'

const { t } = useI18n()
const store = usePermissionStore()
const hasChanges = ref(false)
const pendingChanges = ref<Map<string, any>>(new Map())
const saveSuccess = ref(false)

// Match backend TenderPhase enum names (serialized as strings via System.Text.Json)
const phases = [
  'Drafting', 'Review', 'Approval', 'Published',
  'EvaluationTechnical', 'EvaluationFinancial', 'Awarding', 'Closed'
]

// Match backend OrganizationRole enum names
const roles = [
  'SystemAdmin', 'OrganizationAdmin', 'DepartmentManager',
  'Coordinator', 'CommitteeChair', 'CommitteeMember',
  'LegalAdvisor', 'Viewer'
]

const permissionFields = [
  'canView', 'canCreate', 'canEdit', 'canDelete',
  'canApprove', 'canReject', 'canDelegate', 'canExport'
] as const

function getPhaseLabel(key: string) {
  const labels: Record<string, string> = {
    Drafting: t('permissions.phases.Drafting'),
    Review: t('permissions.phases.Review'),
    Approval: t('permissions.phases.Approval'),
    Published: t('permissions.phases.Publishing'),
    EvaluationTechnical: t('permissions.phases.TechnicalEvaluation'),
    EvaluationFinancial: t('permissions.phases.FinancialEvaluation'),
    Awarding: t('permissions.phases.Awarding'),
    Closed: t('permissions.phases.Contracting'),
  }
  return labels[key] || key
}

function getRoleLabel(role: string) {
  return t(`permissions.roles.${role}`)
}

function getPermLabel(perm: string) {
  return t(`permissions.fields.${perm}`)
}

function getMatrixEntry(phase: string, role: string): PermissionMatrix | undefined {
  return store.matrix.find(m => m.tenderPhase === phase && m.userRole === role)
}

function togglePermission(entry: PermissionMatrix, field: typeof permissionFields[number]) {
  const newValue = !(entry as any)[field]
  const key = entry.id

  if (!pendingChanges.value.has(key)) {
    pendingChanges.value.set(key, {
      userRole: entry.userRole,
      tenderPhase: entry.tenderPhase,
      canView: entry.canView,
      canCreate: entry.canCreate,
      canEdit: entry.canEdit,
      canDelete: entry.canDelete,
      canApprove: entry.canApprove,
      canReject: entry.canReject,
      canDelegate: entry.canDelegate,
      canExport: entry.canExport,
    })
  }

  const pending = pendingChanges.value.get(key)!
  pending[field] = newValue
  ;(entry as any)[field] = newValue
  hasChanges.value = true
  saveSuccess.value = false
}

async function saveAll() {
  if (pendingChanges.value.size === 0) return
  const entries: UpdatePermissionEntry[] = Array.from(pendingChanges.value.values()).map(p => ({
    userRole: p.userRole,
    tenderPhase: p.tenderPhase,
    canView: p.canView,
    canCreate: p.canCreate,
    canEdit: p.canEdit,
    canDelete: p.canDelete,
    canApprove: p.canApprove,
    canReject: p.canReject,
    canDelegate: p.canDelegate,
    canExport: p.canExport,
  }))
  const success = await store.bulkUpdate(entries)
  if (success) {
    pendingChanges.value.clear()
    hasChanges.value = false
    saveSuccess.value = true
    setTimeout(() => { saveSuccess.value = false }, 3000)
  }
}

const selectedPhase = ref(phases[0])

const filteredMatrix = computed(() => {
  return roles.map(role => {
    const entry = getMatrixEntry(selectedPhase.value, role)
    return { role, entry }
  })
})

onMounted(() => store.fetchMatrix())
</script>

<template>
  <div class="p-6">
    <!-- Header -->
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ t('permissions.title') }}</h1>
        <p class="text-gray-500 mt-1">{{ t('permissions.subtitle') }}</p>
      </div>
      <div class="flex items-center gap-3">
        <span v-if="saveSuccess" class="text-green-600 text-sm flex items-center gap-1">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
          </svg>
          {{ t('common.saved') }}
        </span>
        <button
          @click="saveAll"
          :disabled="!hasChanges || store.isSaving"
          class="bg-primary-600 text-white px-4 py-2.5 rounded-lg hover:bg-primary-700 transition-colors flex items-center gap-2 disabled:opacity-50"
        >
          <div v-if="store.isSaving" class="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
          <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7H5a2 2 0 00-2 2v9a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-3m-1 4l-3 3m0 0l-3-3m3 3V4" />
          </svg>
          {{ t('permissions.saveChanges') }}
        </button>
      </div>
    </div>

    <!-- Phase Tabs -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-200 mb-6">
      <div class="flex overflow-x-auto border-b border-gray-200">
        <button
          v-for="phase in phases"
          :key="phase"
          @click="selectedPhase = phase"
          class="px-4 py-3 text-sm font-medium whitespace-nowrap border-b-2 transition-colors"
          :class="selectedPhase === phase
            ? 'border-primary-600 text-primary-600'
            : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'"
        >
          {{ getPhaseLabel(phase) }}
        </button>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="store.isLoading" class="flex justify-center py-12">
      <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-primary-600"></div>
    </div>

    <!-- Error -->
    <div v-else-if="store.error" class="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
      <p class="text-red-700 text-sm">{{ store.error }}</p>
    </div>

    <!-- Matrix Table -->
    <div v-else class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full">
          <thead class="bg-gray-50 border-b border-gray-200">
            <tr>
              <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600 sticky start-0 bg-gray-50 z-10 min-w-[180px]">
                {{ t('permissions.role') }}
              </th>
              <th v-for="perm in permissionFields" :key="perm" class="text-center px-3 py-3 text-sm font-semibold text-gray-600 min-w-[90px]">
                {{ getPermLabel(perm) }}
              </th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-for="{ role, entry } in filteredMatrix" :key="role" class="hover:bg-gray-50">
              <td class="px-4 py-3 text-sm font-medium text-gray-900 sticky start-0 bg-white z-10">
                {{ getRoleLabel(role) }}
              </td>
              <td v-for="perm in permissionFields" :key="perm" class="px-3 py-3 text-center">
                <button
                  v-if="entry"
                  @click="togglePermission(entry, perm)"
                  class="w-8 h-8 rounded-lg flex items-center justify-center transition-all mx-auto"
                  :class="(entry as any)[perm]
                    ? 'bg-green-100 text-green-600 hover:bg-green-200'
                    : 'bg-gray-100 text-gray-400 hover:bg-gray-200'"
                >
                  <svg v-if="(entry as any)[perm]" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                  <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
                <span v-else class="text-gray-300 text-xs">—</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Legend -->
    <div class="mt-4 flex items-center gap-6 text-sm text-gray-500">
      <div class="flex items-center gap-2">
        <div class="w-6 h-6 rounded bg-green-100 flex items-center justify-center">
          <svg class="w-4 h-4 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
          </svg>
        </div>
        {{ t('permissions.allowed') }}
      </div>
      <div class="flex items-center gap-2">
        <div class="w-6 h-6 rounded bg-gray-100 flex items-center justify-center">
          <svg class="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </div>
        {{ t('permissions.denied') }}
      </div>
    </div>
  </div>
</template>
