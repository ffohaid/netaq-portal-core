<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useWorkflowStore } from '../../stores/workflow'
import { getCurrentLocale } from '../../i18n'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const workflowStore = useWorkflowStore()
const locale = computed(() => getCurrentLocale())

const templateId = route.params.id as string

onMounted(() => {
  workflowStore.fetchTemplateDetail(templateId)
})

function getStepName(step: any) {
  return locale.value === 'ar' ? step.nameAr : step.nameEn
}

function getStepTypeColor(type: string) {
  switch (type) {
    case 'Sequential': return 'bg-blue-100 text-blue-700'
    case 'Parallel': return 'bg-purple-100 text-purple-700'
    case 'Conditional': return 'bg-orange-100 text-orange-700'
    default: return 'bg-gray-100 text-gray-700'
  }
}
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
        <h1 class="text-2xl font-bold text-gray-900">{{ t('common.details') }}</h1>
      </div>
    </div>

    <div v-if="workflowStore.isLoading" class="flex items-center justify-center py-12">
      <svg class="animate-spin w-8 h-8 text-primary-500" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
      </svg>
    </div>

    <template v-else-if="workflowStore.currentTemplate">
      <!-- Template Info -->
      <div class="card">
        <div class="flex items-start justify-between mb-4">
          <div>
            <h2 class="text-xl font-bold text-gray-900">
              {{ locale === 'ar' ? workflowStore.currentTemplate.nameAr : workflowStore.currentTemplate.nameEn }}
            </h2>
            <p class="text-gray-500 mt-1">
              {{ locale === 'ar' ? workflowStore.currentTemplate.descriptionAr : workflowStore.currentTemplate.descriptionEn }}
            </p>
          </div>
          <span :class="workflowStore.currentTemplate.isActive ? 'badge-success' : 'badge-danger'">
            {{ workflowStore.currentTemplate.isActive ? t('workflow.active') : 'Inactive' }}
          </span>
        </div>
        <div class="flex gap-4 text-sm text-gray-400">
          <span>v{{ workflowStore.currentTemplate.version }}</span>
          <span>{{ workflowStore.currentTemplate.steps.length }} {{ t('workflow.steps') }}</span>
        </div>
      </div>

      <!-- Steps Timeline -->
      <div class="card">
        <h3 class="text-lg font-semibold text-gray-900 mb-6">{{ t('workflow.steps') }}</h3>

        <div class="relative">
          <!-- Vertical line -->
          <div class="absolute start-6 top-0 bottom-0 w-0.5 bg-gray-200"></div>

          <div
            v-for="step in workflowStore.currentTemplate.steps"
            :key="step.id"
            class="relative flex items-start gap-4 pb-8 last:pb-0"
          >
            <!-- Step number circle -->
            <div class="relative z-10 flex items-center justify-center w-12 h-12 bg-primary-500 text-white rounded-full text-sm font-bold flex-shrink-0">
              {{ step.order }}
            </div>

            <!-- Step content -->
            <div class="flex-1 bg-gray-50 rounded-lg p-4">
              <div class="flex items-center justify-between mb-2">
                <h4 class="font-medium text-gray-900">{{ getStepName(step) }}</h4>
                <span :class="['text-xs px-2 py-0.5 rounded-full font-medium', getStepTypeColor(step.stepType)]">
                  {{ t(`workflow.${step.stepType.toLowerCase()}`) }}
                </span>
              </div>
              <div class="flex gap-4 text-xs text-gray-500">
                <span>{{ t('workflow.requiredRole') }}: {{ t(`roles.${step.requiredRole}`) }}</span>
                <span>SLA: {{ step.slaDurationHours }}h</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
