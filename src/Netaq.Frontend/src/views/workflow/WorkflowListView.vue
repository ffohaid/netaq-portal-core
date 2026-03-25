<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useWorkflowStore } from '../../stores/workflow'
import { getCurrentLocale } from '../../i18n'

const { t } = useI18n()
const workflowStore = useWorkflowStore()
const locale = computed(() => getCurrentLocale())

onMounted(() => {
  workflowStore.fetchTemplates()
})

function getTemplateName(template: any) {
  return locale.value === 'ar' ? template.nameAr : template.nameEn
}

function getTemplateDesc(template: any) {
  return locale.value === 'ar' ? template.descriptionAr : template.descriptionEn
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ t('workflow.title') }}</h1>
        <p class="text-gray-500 mt-1">{{ t('workflow.templates') }}</p>
      </div>
      <router-link to="/workflows/create" class="btn-primary">
        {{ t('workflow.createTemplate') }}
      </router-link>
    </div>

    <!-- Templates Grid -->
    <div v-if="workflowStore.isLoading" class="flex items-center justify-center py-12">
      <svg class="animate-spin w-8 h-8 text-primary-500" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
      </svg>
    </div>

    <div v-else-if="workflowStore.templates.length === 0" class="card text-center py-12">
      <svg class="w-16 h-16 mx-auto mb-4 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M13 10V3L4 14h7v7l9-11h-7z" />
      </svg>
      <p class="text-gray-500 text-lg mb-4">{{ t('common.noData') }}</p>
      <router-link to="/workflows/create" class="btn-primary">
        {{ t('workflow.createTemplate') }}
      </router-link>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <router-link
        v-for="template in workflowStore.templates"
        :key="template.id"
        :to="`/workflows/${template.id}`"
        class="card hover:shadow-md transition-shadow cursor-pointer"
      >
        <div class="flex items-start justify-between mb-3">
          <div class="p-2 bg-primary-50 rounded-lg">
            <svg class="w-6 h-6 text-primary-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
            </svg>
          </div>
          <span :class="template.isActive ? 'badge-success' : 'badge-danger'">
            {{ template.isActive ? t('workflow.active') : t('common.status') }}
          </span>
        </div>

        <h3 class="text-lg font-semibold text-gray-900 mb-1">{{ getTemplateName(template) }}</h3>
        <p class="text-sm text-gray-500 mb-4 line-clamp-2">{{ getTemplateDesc(template) }}</p>

        <div class="flex items-center justify-between text-xs text-gray-400">
          <span>{{ template.stepCount }} {{ t('workflow.steps') }}</span>
          <span>v{{ template.version }}</span>
          <span>{{ formatDate(template.createdAt) }}</span>
        </div>
      </router-link>
    </div>
  </div>
</template>
