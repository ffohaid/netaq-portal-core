<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useTenderStore } from '../../stores/tenders'
import { getCurrentLocale } from '../../i18n'
import type { TemplateCategory } from '../../types'

const { t } = useI18n()
const router = useRouter()
const tenderStore = useTenderStore()
const locale = computed(() => getCurrentLocale())

const categoryFilter = ref<TemplateCategory | ''>('')

const categories: TemplateCategory[] = [
  'Supply', 'Services', 'Consulting', 'Engineering',
  'InformationTechnology', 'Construction', 'SpecialModels',
]

function getTemplateName(template: { nameAr: string; nameEn: string }) {
  return locale.value === 'ar' ? template.nameAr : template.nameEn
}

function getTemplateDesc(template: { descriptionAr?: string; descriptionEn?: string }) {
  return locale.value === 'ar' ? template.descriptionAr : template.descriptionEn
}

function getCategoryIcon(category: TemplateCategory): string {
  const icons: Record<TemplateCategory, string> = {
    Supply: 'M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4',
    Services: 'M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z',
    Consulting: 'M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z',
    Engineering: 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.066 2.573c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.573 1.066c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.066-2.573c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z',
    InformationTechnology: 'M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z',
    Construction: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4',
    SpecialModels: 'M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z',
  }
  return icons[category] || icons.Supply
}

async function loadTemplates() {
  await tenderStore.fetchTemplates({
    category: categoryFilter.value || undefined,
    activeOnly: true,
  })
}

watch(categoryFilter, () => {
  loadTemplates()
})

onMounted(() => {
  loadTemplates()
})
</script>

<template>
  <div class="p-6">
    <!-- Header -->
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-gray-900">{{ t('templates.title') }}</h1>
      <p class="text-gray-500 mt-1">{{ t('templates.list') }}</p>
    </div>

    <!-- Category Filter -->
    <div class="flex flex-wrap gap-2 mb-6">
      <button
        @click="categoryFilter = ''"
        class="px-4 py-2 rounded-lg text-sm font-medium transition-colors"
        :class="categoryFilter === '' ? 'bg-primary-600 text-white' : 'bg-white border border-gray-200 text-gray-600 hover:bg-gray-50'"
      >
        {{ t('common.all') }}
      </button>
      <button
        v-for="category in categories"
        :key="category"
        @click="categoryFilter = category"
        class="px-4 py-2 rounded-lg text-sm font-medium transition-colors flex items-center gap-2"
        :class="categoryFilter === category ? 'bg-primary-600 text-white' : 'bg-white border border-gray-200 text-gray-600 hover:bg-gray-50'"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="getCategoryIcon(category)" />
        </svg>
        {{ t(`templates.categories.${category}`) }}
      </button>
    </div>

    <!-- Loading -->
    <div v-if="tenderStore.isLoading" class="flex justify-center py-12">
      <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-primary-600"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="tenderStore.templates.length === 0" class="bg-white rounded-xl shadow-sm border border-gray-200 p-12 text-center">
      <svg class="mx-auto h-16 w-16 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2" />
      </svg>
      <h3 class="mt-4 text-lg font-medium text-gray-900">{{ t('common.noData') }}</h3>
    </div>

    <!-- Templates Grid -->
    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <div
        v-for="template in tenderStore.templates"
        :key="template.id"
        @click="router.push(`/templates/${template.id}`)"
        class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 hover:shadow-md hover:border-primary-300 transition-all cursor-pointer"
      >
        <div class="flex items-start justify-between mb-4">
          <div class="w-10 h-10 bg-primary-100 rounded-lg flex items-center justify-center">
            <svg class="w-5 h-5 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="getCategoryIcon(template.category)" />
            </svg>
          </div>
          <span class="text-xs bg-gray-100 text-gray-600 px-2 py-1 rounded-full">{{ t(`templates.categories.${template.category}`) }}</span>
        </div>
        <h3 class="font-semibold text-gray-900 mb-2">{{ getTemplateName(template) }}</h3>
        <p class="text-sm text-gray-500 line-clamp-2 mb-4">{{ getTemplateDesc(template) }}</p>
        <div class="flex items-center justify-between text-xs text-gray-400">
          <span>{{ t('templates.version') }}: {{ template.version }}</span>
          <span>{{ template.sectionCount }} {{ t('templates.sectionCount') }}</span>
        </div>
      </div>
    </div>
  </div>
</template>
