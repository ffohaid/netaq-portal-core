<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useWorkflowStore } from '../../stores/workflow'

const { t } = useI18n()
const router = useRouter()
const workflowStore = useWorkflowStore()

const nameAr = ref('')
const nameEn = ref('')
const descriptionAr = ref('')
const descriptionEn = ref('')

interface StepForm {
  nameAr: string
  nameEn: string
  order: number
  stepType: string
  requiredRole: string
  slaDurationHours: number
}

const steps = ref<StepForm[]>([
  { nameAr: '', nameEn: '', order: 1, stepType: 'Sequential', requiredRole: 'DepartmentManager', slaDurationHours: 24 },
])

function addStep() {
  steps.value.push({
    nameAr: '',
    nameEn: '',
    order: steps.value.length + 1,
    stepType: 'Sequential',
    requiredRole: 'DepartmentManager',
    slaDurationHours: 24,
  })
}

function removeStep(index: number) {
  if (steps.value.length > 1) {
    steps.value.splice(index, 1)
    steps.value.forEach((s, i) => s.order = i + 1)
  }
}

async function handleSubmit() {
  const result = await workflowStore.createTemplate({
    nameAr: nameAr.value,
    nameEn: nameEn.value,
    descriptionAr: descriptionAr.value,
    descriptionEn: descriptionEn.value,
    steps: steps.value,
  })

  if (result) {
    router.push(`/workflows/${result}`)
  }
}

const roles = [
  { value: 'SystemAdmin', label: 'roles.SystemAdmin' },
  { value: 'OrganizationAdmin', label: 'roles.OrganizationAdmin' },
  { value: 'DepartmentManager', label: 'roles.DepartmentManager' },
  { value: 'Coordinator', label: 'roles.Coordinator' },
  { value: 'CommitteeChair', label: 'roles.CommitteeChair' },
  { value: 'CommitteeMember', label: 'roles.CommitteeMember' },
]

const stepTypes = [
  { value: 'Sequential', label: 'workflow.sequential' },
  { value: 'Parallel', label: 'workflow.parallel' },
  { value: 'Conditional', label: 'workflow.conditional' },
]
</script>

<template>
  <div class="max-w-4xl mx-auto space-y-6">
    <!-- Header -->
    <div class="flex items-center gap-4">
      <button @click="router.back()" class="p-2 hover:bg-gray-100 rounded-lg">
        <svg class="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
        </svg>
      </button>
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ t('workflow.createTemplate') }}</h1>
      </div>
    </div>

    <form @submit.prevent="handleSubmit" class="space-y-6">
      <!-- Template Info -->
      <div class="card space-y-4">
        <h2 class="text-lg font-semibold text-gray-900">{{ t('workflow.templateName') }}</h2>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('workflow.templateName') }} (AR)</label>
            <input v-model="nameAr" type="text" required class="input-field" dir="rtl" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('workflow.templateName') }} (EN)</label>
            <input v-model="nameEn" type="text" required class="input-field" dir="ltr" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Description (AR)</label>
            <textarea v-model="descriptionAr" rows="3" class="input-field" dir="rtl"></textarea>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Description (EN)</label>
            <textarea v-model="descriptionEn" rows="3" class="input-field" dir="ltr"></textarea>
          </div>
        </div>
      </div>

      <!-- Steps -->
      <div class="card space-y-4">
        <div class="flex items-center justify-between">
          <h2 class="text-lg font-semibold text-gray-900">{{ t('workflow.steps') }}</h2>
          <button type="button" @click="addStep" class="btn-secondary text-sm">
            + {{ t('workflow.addStep') }}
          </button>
        </div>

        <div
          v-for="(step, index) in steps"
          :key="index"
          class="border border-gray-200 rounded-lg p-4 space-y-3"
        >
          <div class="flex items-center justify-between">
            <span class="text-sm font-medium text-gray-500">{{ t('workflow.steps') }} {{ step.order }}</span>
            <button
              v-if="steps.length > 1"
              type="button"
              @click="removeStep(index)"
              class="text-danger-500 hover:text-danger-600 text-sm"
            >
              {{ t('common.delete') }}
            </button>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-medium text-gray-600 mb-1">{{ t('workflow.stepName') }} (AR)</label>
              <input v-model="step.nameAr" type="text" required class="input-field text-sm" dir="rtl" />
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-600 mb-1">{{ t('workflow.stepName') }} (EN)</label>
              <input v-model="step.nameEn" type="text" required class="input-field text-sm" dir="ltr" />
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-600 mb-1">{{ t('workflow.stepType') }}</label>
              <select v-model="step.stepType" class="input-field text-sm">
                <option v-for="st in stepTypes" :key="st.value" :value="st.value">{{ t(st.label) }}</option>
              </select>
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-600 mb-1">{{ t('workflow.requiredRole') }}</label>
              <select v-model="step.requiredRole" class="input-field text-sm">
                <option v-for="role in roles" :key="role.value" :value="role.value">{{ t(role.label) }}</option>
              </select>
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-600 mb-1">{{ t('workflow.slaDuration') }}</label>
              <input v-model.number="step.slaDurationHours" type="number" min="1" required class="input-field text-sm" />
            </div>
          </div>
        </div>
      </div>

      <!-- Error -->
      <div v-if="workflowStore.error" class="p-3 bg-danger-50 border border-danger-500/20 rounded-lg">
        <p class="text-sm text-danger-700">{{ workflowStore.error }}</p>
      </div>

      <!-- Actions -->
      <div class="flex items-center justify-end gap-3">
        <button type="button" @click="router.back()" class="btn-secondary">{{ t('common.cancel') }}</button>
        <button type="submit" :disabled="workflowStore.isLoading" class="btn-primary">
          {{ workflowStore.isLoading ? t('common.loading') : t('common.create') }}
        </button>
      </div>
    </form>
  </div>
</template>
