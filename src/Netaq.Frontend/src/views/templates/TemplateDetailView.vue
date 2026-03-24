<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useTenderStore } from '../../stores/tenders'
import { getCurrentLocale } from '../../i18n'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const tenderStore = useTenderStore()
const locale = computed(() => getCurrentLocale())

const templateId = route.params.id as string

function getTitle(item: { titleAr: string; titleEn: string }) {
  return locale.value === 'ar' ? item.titleAr : item.titleEn
}

function getGuidance(item: { guidanceNotesAr?: string; guidanceNotesEn?: string }) {
  return locale.value === 'ar' ? item.guidanceNotesAr : item.guidanceNotesEn
}

onMounted(async () => {
  await tenderStore.fetchTemplateById(templateId)
})
</script>

<template>
  <div class="p-6 max-w-4xl mx-auto">
    <!-- Loading -->
    <div v-if="tenderStore.isLoading" class="flex justify-center py-12">
      <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-primary-600"></div>
    </div>

    <template v-else-if="tenderStore.currentTemplate">
      <!-- Header -->
      <div class="flex items-start justify-between mb-6">
        <div class="flex items-center gap-4">
          <button @click="router.push('/templates')" class="text-gray-500 hover:text-gray-700">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
          </button>
          <div>
            <h1 class="text-2xl font-bold text-gray-900">
              {{ locale === 'ar' ? tenderStore.currentTemplate.nameAr : tenderStore.currentTemplate.nameEn }}
            </h1>
            <div class="flex items-center gap-3 mt-1">
              <span class="text-sm bg-primary-100 text-primary-700 px-2 py-0.5 rounded-full">
                {{ t(`templates.categories.${tenderStore.currentTemplate.category}`) }}
              </span>
              <span class="text-sm text-gray-500">{{ t('templates.version') }}: {{ tenderStore.currentTemplate.version }}</span>
            </div>
          </div>
        </div>
        <button
          @click="router.push({ path: '/tenders/create', query: { templateId: templateId } })"
          class="bg-primary-600 text-white px-4 py-2.5 rounded-lg hover:bg-primary-700 transition-colors flex items-center gap-2"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          {{ t('templates.useTemplate') }}
        </button>
      </div>

      <!-- Description -->
      <div v-if="tenderStore.currentTemplate.descriptionAr || tenderStore.currentTemplate.descriptionEn" class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 mb-6">
        <p class="text-gray-700">
          {{ locale === 'ar' ? tenderStore.currentTemplate.descriptionAr : tenderStore.currentTemplate.descriptionEn }}
        </p>
      </div>

      <!-- Sections -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <div class="px-6 py-4 border-b border-gray-200">
          <h2 class="text-lg font-semibold text-gray-900">{{ t('sections.title') }}</h2>
        </div>
        <div class="divide-y divide-gray-100">
          <div
            v-for="(section, index) in tenderStore.currentTemplate.sections"
            :key="section.id"
            class="p-6"
          >
            <div class="flex items-start gap-4">
              <div class="w-8 h-8 bg-primary-100 text-primary-700 rounded-lg flex items-center justify-center text-sm font-bold flex-shrink-0">
                {{ index + 1 }}
              </div>
              <div class="flex-1">
                <h3 class="font-semibold text-gray-900">{{ getTitle(section) }}</h3>
                <span class="text-xs bg-gray-100 text-gray-500 px-2 py-0.5 rounded-full mt-1 inline-block">
                  {{ t(`sections.type.${section.sectionType}`) }}
                </span>
                <!-- Guidance Notes -->
                <div v-if="getGuidance(section)" class="mt-3 bg-blue-50 border border-blue-200 rounded-lg p-3">
                  <div class="flex items-start gap-2">
                    <svg class="w-4 h-4 text-blue-500 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                    <p class="text-sm text-blue-700">{{ getGuidance(section) }}</p>
                  </div>
                </div>
                <!-- Default Content Preview -->
                <div v-if="section.defaultContentHtml" class="mt-3 text-sm text-gray-600 border-s-2 border-gray-200 ps-4 line-clamp-3" v-html="section.defaultContentHtml"></div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
