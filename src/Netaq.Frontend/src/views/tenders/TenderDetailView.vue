<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useTenderStore } from '../../stores/tenders'
import { getCurrentLocale } from '../../i18n'
import type { TenderSection, TenderCriteria, AiComplianceCheck, AiSuggestion, TenderStatus } from '../../types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const tenderStore = useTenderStore()
const locale = computed(() => getCurrentLocale())

const tenderId = route.params.id as string
const activeTab = ref<'sections' | 'criteria' | 'ai' | 'export'>('sections')
const activeSection = ref<TenderSection | null>(null)
const isAutoSaving = ref(false)
const autoSaveTimer = ref<ReturnType<typeof setTimeout> | null>(null)

// AI State
const aiLoading = ref(false)
const complianceResult = ref<AiComplianceCheck | null>(null)
const boilerplateResult = ref<AiSuggestion | null>(null)
const showAiPanel = ref(false)
const aiError = ref<string | null>(null)
const aiActiveAction = ref<'compliance' | 'generate' | 'criteria' | null>(null)

// Export State
const isExporting = ref(false)

function getTitle(item: { titleAr: string; titleEn: string } | { nameAr: string; nameEn: string }) {
  if ('titleAr' in item) return locale.value === 'ar' ? item.titleAr : item.titleEn
  return locale.value === 'ar' ? item.nameAr : item.nameEn
}

function getStatusClass(status: TenderStatus) {
  const classes: Record<string, string> = {
    Draft: 'bg-gray-100 text-gray-700',
    PendingApproval: 'bg-yellow-100 text-yellow-700',
    Approved: 'bg-green-100 text-green-700',
    EvaluationInProgress: 'bg-blue-100 text-blue-700',
    EvaluationCompleted: 'bg-emerald-100 text-emerald-700',
    Archived: 'bg-purple-100 text-purple-700',
    Cancelled: 'bg-red-100 text-red-700',
  }
  return classes[status] || 'bg-gray-100 text-gray-700'
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('en-US', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value)
}

function selectSection(section: TenderSection) {
  activeSection.value = { ...section }
}

// Auto-save with debounce
function onSectionContentChange() {
  if (autoSaveTimer.value) clearTimeout(autoSaveTimer.value)
  autoSaveTimer.value = setTimeout(async () => {
    if (!activeSection.value) return
    isAutoSaving.value = true
    await tenderStore.updateSection(tenderId, {
      sectionId: activeSection.value.id,
      contentHtml: activeSection.value.contentHtml || '',
      completionPercentage: activeSection.value.completionPercentage,
    })
    isAutoSaving.value = false
  }, 2000) // 2-second debounce for auto-save
}

// AI Functions
async function generateBoilerplate() {
  if (!activeSection.value) return
  aiLoading.value = true
  aiError.value = null
  showAiPanel.value = true
  try {
    boilerplateResult.value = await tenderStore.aiGenerateBoilerplate(
      tenderId,
      activeSection.value.sectionType
    )
    if (!boilerplateResult.value) {
      aiError.value = locale.value === 'ar'
        ? 'لم يتم الحصول على نتيجة من المساعد الذكي. تأكد من إعدادات الذكاء الاصطناعي في لوحة الإعدادات.'
        : 'No result from AI assistant. Please check AI configuration in Settings.'
    }
  } catch (e: any) {
    aiError.value = locale.value === 'ar'
      ? 'حدث خطأ أثناء توليد المحتوى. يرجى المحاولة مرة أخرى.'
      : 'Error generating content. Please try again.'
  }
  aiLoading.value = false
}

async function checkCompliance() {
  aiLoading.value = true
  aiError.value = null
  aiActiveAction.value = 'compliance'
  complianceResult.value = null
  try {
    complianceResult.value = await tenderStore.aiCheckCompliance(tenderId)
    if (!complianceResult.value) {
      aiError.value = locale.value === 'ar'
        ? 'لم يتم الحصول على نتيجة فحص الامتثال. تأكد من إعدادات الذكاء الاصطناعي.'
        : 'No compliance result. Please check AI configuration.'
    }
  } catch (e: any) {
    aiError.value = locale.value === 'ar'
      ? 'حدث خطأ أثناء فحص الامتثال. يرجى المحاولة مرة أخرى.'
      : 'Error checking compliance. Please try again.'
  }
  aiLoading.value = false
}

function resetAiState() {
  aiActiveAction.value = null
  complianceResult.value = null
  aiError.value = null
  aiLoading.value = false
}

function applyAiContent() {
  if (boilerplateResult.value && activeSection.value) {
    activeSection.value.contentHtml = boilerplateResult.value.content
    onSectionContentChange()
    boilerplateResult.value = null
    showAiPanel.value = false
  }
}

// Export Functions
async function handleExportPdf() {
  isExporting.value = true
  await tenderStore.exportPdf(tenderId)
  isExporting.value = false
}

async function handleExportDocx() {
  isExporting.value = true
  await tenderStore.exportDocx(tenderId)
  isExporting.value = false
}

// Submit for approval
const submitLoading = ref(false)
const submitError = ref<string | null>(null)
const submitSuccess = ref(false)

async function handleSubmitForApproval() {
  const confirmMsg = locale.value === 'ar'
    ? 'هل أنت متأكد من إرسال الكراسة للاعتماد؟ لن تتمكن من التعديل بعد الإرسال.'
    : 'Are you sure you want to submit for approval? You will not be able to edit after submission.'
  if (!confirm(confirmMsg)) return

  submitLoading.value = true
  submitError.value = null
  submitSuccess.value = false
  const success = await tenderStore.submitForApproval(tenderId)
  if (success) {
    submitSuccess.value = true
    await tenderStore.fetchTenderById(tenderId)
    setTimeout(() => { submitSuccess.value = false }, 5000)
  } else {
    submitError.value = tenderStore.error || (locale.value === 'ar'
      ? 'فشل إرسال الكراسة للاعتماد. تأكد من اكتمال جميع الأبواب ومعايير التقييم.'
      : 'Failed to submit. Ensure all sections and criteria are complete.')
  }
  submitLoading.value = false
}

// Criteria helpers
function getTechnicalCriteria(): TenderCriteria[] {
  return tenderStore.currentTender?.criteria.filter(c => c.criteriaType === 'Technical' && !c.parentId) || []
}

function getFinancialCriteria(): TenderCriteria[] {
  return tenderStore.currentTender?.criteria.filter(c => c.criteriaType === 'Financial' && !c.parentId) || []
}

onMounted(async () => {
  await tenderStore.fetchTenderById(tenderId)
  if (tenderStore.currentTender?.sections.length) {
    activeSection.value = { ...tenderStore.currentTender.sections[0] }
  }
})

onUnmounted(() => {
  if (autoSaveTimer.value) clearTimeout(autoSaveTimer.value)
})
</script>

<template>
  <div class="p-6">
    <!-- Loading -->
    <div v-if="tenderStore.isLoading && !tenderStore.currentTender" class="flex justify-center py-12">
      <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-primary-600"></div>
    </div>

    <template v-else-if="tenderStore.currentTender">
      <!-- Header -->
      <div class="flex items-start justify-between mb-6">
        <div class="flex items-center gap-4">
          <button @click="router.push('/tenders')" class="text-gray-500 hover:text-gray-700">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
          </button>
          <div>
            <h1 class="text-2xl font-bold text-gray-900">{{ getTitle(tenderStore.currentTender) }}</h1>
            <div class="flex items-center gap-3 mt-1">
              <span class="text-sm text-gray-500 font-mono">{{ tenderStore.currentTender.referenceNumber }}</span>
              <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium" :class="getStatusClass(tenderStore.currentTender.status)">
                {{ t(`tenders.status.${tenderStore.currentTender.status}`) }}
              </span>
            </div>
          </div>
        </div>
        <div class="flex items-center gap-3">
          <!-- Auto-save indicator -->
          <span v-if="isAutoSaving" class="text-xs text-gray-400 flex items-center gap-1">
            <div class="animate-spin rounded-full h-3 w-3 border-b-2 border-gray-400"></div>
            {{ t('common.autoSaved') }}
          </span>
          <!-- Completion -->
          <div class="flex items-center gap-2">
            <div class="w-24 bg-gray-200 rounded-full h-2">
              <div class="bg-primary-600 h-2 rounded-full transition-all" :style="{ width: `${tenderStore.currentTender.completionPercentage}%` }"></div>
            </div>
            <span class="text-sm text-gray-600">{{ tenderStore.currentTender.completionPercentage }}%</span>
          </div>
          <!-- Actions -->
          <div v-if="tenderStore.currentTender.status === 'Draft'" class="flex items-center gap-3">
            <button
              @click="handleSubmitForApproval"
              :disabled="submitLoading"
              class="bg-primary-600 text-white px-5 py-2.5 rounded-lg hover:bg-primary-700 transition-colors text-sm font-medium disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
            >
              <svg v-if="submitLoading" class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
              </svg>
              {{ submitLoading ? (locale === 'ar' ? 'جاري الإرسال...' : 'Submitting...') : t('tenders.submitForApproval') }}
            </button>
          </div>
          <div v-else-if="tenderStore.currentTender.status === 'PendingApproval'" class="flex items-center gap-2 bg-yellow-50 border border-yellow-200 px-4 py-2 rounded-lg">
            <svg class="w-5 h-5 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
            <span class="text-sm font-medium text-yellow-700">{{ locale === 'ar' ? 'بانتظار الاعتماد' : 'Pending Approval' }}</span>
          </div>
          <div v-else-if="tenderStore.currentTender.status === 'Approved'" class="flex items-center gap-2 bg-green-50 border border-green-200 px-4 py-2 rounded-lg">
            <svg class="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
            <span class="text-sm font-medium text-green-700">{{ locale === 'ar' ? 'معتمدة' : 'Approved' }}</span>
          </div>
        </div>
        <!-- Submit Status Messages -->
        <div v-if="submitError" class="mt-3 bg-red-50 border border-red-200 rounded-lg p-3 flex items-center gap-2">
          <svg class="w-5 h-5 text-red-500 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
          <p class="text-sm text-red-700">{{ submitError }}</p>
          <button @click="submitError = null" class="ms-auto text-red-400 hover:text-red-600">&times;</button>
        </div>
        <div v-if="submitSuccess" class="mt-3 bg-green-50 border border-green-200 rounded-lg p-3 flex items-center gap-2">
          <svg class="w-5 h-5 text-green-500 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
          <p class="text-sm text-green-700">{{ locale === 'ar' ? 'تم إرسال الكراسة للاعتماد بنجاح. تم إنشاء مهمة اعتماد للمعتمد.' : 'Tender submitted for approval successfully. An approval task has been created.' }}</p>
        </div>
      </div>

      <!-- Info Cards -->
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <div class="bg-white rounded-xl border border-gray-200 p-4">
          <p class="text-xs text-gray-500">{{ t('tenders.tenderType') }}</p>
          <p class="font-semibold text-gray-900 mt-1">{{ t(`tenders.type.${tenderStore.currentTender.tenderType}`) }}</p>
        </div>
        <div class="bg-white rounded-xl border border-gray-200 p-4">
          <p class="text-xs text-gray-500">{{ t('tenders.estimatedValue') }}</p>
          <p class="font-semibold text-gray-900 mt-1">{{ formatCurrency(tenderStore.currentTender.estimatedValue) }} ر.س</p>
        </div>
        <div class="bg-white rounded-xl border border-gray-200 p-4">
          <p class="text-xs text-gray-500">{{ t('tenders.technicalWeight') }}</p>
          <p class="font-semibold text-primary-600 mt-1">{{ tenderStore.currentTender.technicalWeight }}%</p>
        </div>
        <div class="bg-white rounded-xl border border-gray-200 p-4">
          <p class="text-xs text-gray-500">{{ t('tenders.financialWeight') }}</p>
          <p class="font-semibold text-green-600 mt-1">{{ tenderStore.currentTender.financialWeight }}%</p>
        </div>
      </div>

      <!-- Tabs -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200">
        <div class="border-b border-gray-200">
          <nav class="flex -mb-px">
            <button
              v-for="tab in [
                { key: 'sections', label: t('sections.title'), icon: 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z' },
                { key: 'criteria', label: t('criteria.title'), icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z' },
                { key: 'ai', label: t('ai.title'), icon: 'M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z' },
                { key: 'export', label: t('exportDoc.title'), icon: 'M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z' },
              ]"
              :key="tab.key"
              @click="activeTab = tab.key as any"
              class="flex items-center gap-2 px-6 py-3 text-sm font-medium border-b-2 transition-colors"
              :class="activeTab === tab.key ? 'border-primary-600 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700'"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="tab.icon" />
              </svg>
              {{ tab.label }}
            </button>
          </nav>
        </div>

        <!-- Sections Tab -->
        <div v-if="activeTab === 'sections'" class="flex" style="min-height: 600px;">
          <!-- Empty State when no sections -->
          <div v-if="!tenderStore.currentTender.sections?.length" class="flex-1 flex flex-col items-center justify-center py-16 text-gray-400">
            <svg class="w-16 h-16 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
            <p class="text-lg font-medium">{{ locale === 'ar' ? 'لا توجد أبواب بعد' : 'No sections yet' }}</p>
            <p class="text-sm mt-2">{{ locale === 'ar' ? 'سيتم إنشاء الأبواب عند اختيار نموذج أو إدخال يدوي' : 'Sections will be created when a template is selected or manual entry is used' }}</p>
            <router-link
              v-if="tenderStore.currentTender.status === 'Draft'"
              :to="`/tenders/${tenderId}/edit`"
              class="mt-4 bg-primary-600 text-white px-6 py-2 rounded-lg hover:bg-primary-700 transition-colors text-sm"
            >
              {{ locale === 'ar' ? 'تعديل المنافسة' : 'Edit Tender' }}
            </router-link>
          </div>
          <!-- Section List -->
          <div v-else class="w-72 border-e border-gray-200 bg-gray-50">
            <div class="p-4">
              <h3 class="text-sm font-semibold text-gray-700">{{ t('sections.title') }}</h3>
            </div>
            <div class="space-y-1 px-2 pb-4">
              <button
                v-for="section in tenderStore.currentTender.sections"
                :key="section.id"
                @click="selectSection(section)"
                class="w-full text-start px-3 py-2.5 rounded-lg transition-colors"
                :class="activeSection?.id === section.id ? 'bg-primary-100 text-primary-700' : 'hover:bg-gray-100 text-gray-700'"
              >
                <div class="flex items-center justify-between">
                  <span class="text-sm font-medium truncate">{{ getTitle(section) }}</span>
                  <span class="text-xs text-gray-400">{{ section.completionPercentage }}%</span>
                </div>
                <div class="mt-1 bg-gray-200 rounded-full h-1">
                  <div class="bg-primary-500 h-1 rounded-full" :style="{ width: `${section.completionPercentage}%` }"></div>
                </div>
              </button>
            </div>
          </div>

          <!-- Section Editor -->
          <div class="flex-1 p-6">
            <template v-if="activeSection">
              <div class="flex items-center justify-between mb-4">
                <h3 class="text-lg font-semibold text-gray-900">{{ getTitle(activeSection) }}</h3>
                <div class="flex items-center gap-3">
                  <span v-if="activeSection.isAiReviewed" class="text-xs text-green-600 flex items-center gap-1">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                    {{ t('sections.aiReviewed') }}
                  </span>
                  <button
                    v-if="tenderStore.currentTender.status === 'Draft'"
                    @click="generateBoilerplate"
                    class="text-sm text-primary-600 hover:text-primary-700 flex items-center gap-1"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                    </svg>
                    {{ t('ai.generateContent') }}
                  </button>
                </div>
              </div>

              <!-- Rich Text Editor (simplified) -->
              <div
                v-if="tenderStore.currentTender.status === 'Draft'"
                class="border border-gray-300 rounded-lg overflow-hidden"
              >
                <div class="bg-gray-50 border-b border-gray-300 px-3 py-2 flex items-center gap-2">
                  <span class="text-xs text-gray-500">{{ t('sections.editSection') }}</span>
                </div>
                <div
                  contenteditable="true"
                  @input="(e: Event) => { activeSection!.contentHtml = (e.target as HTMLElement).innerHTML; onSectionContentChange() }"
                  v-html="activeSection.contentHtml"
                  dir="rtl"
                  class="w-full px-4 py-3 text-sm focus:outline-none min-h-[500px] prose max-w-none"
                  :data-placeholder="locale === 'ar' ? 'ابدأ بكتابة محتوى الباب هنا...' : 'Start writing section content here...'"
                ></div>
              </div>
              <div v-else class="prose max-w-none" dir="rtl" v-html="activeSection.contentHtml || '<p class=\'text-gray-400\'>No content</p>'"></div>

              <!-- Section Completion (auto-calculated, read-only) -->
              <div class="mt-4 flex items-center gap-4">
                <label class="text-sm text-gray-600">{{ t('sections.completion') }}:</label>
                <div class="flex-1 bg-gray-200 rounded-full h-2">
                  <div class="bg-primary-500 h-2 rounded-full transition-all" :style="{ width: `${activeSection.completionPercentage}%` }"></div>
                </div>
                <span class="text-sm font-medium text-gray-700 w-12 text-end">{{ activeSection.completionPercentage }}%</span>
              </div>

              <!-- Auto-save info -->
              <div v-if="activeSection.lastAutoSavedAt" class="mt-2 text-xs text-gray-400">
                {{ t('sections.lastSaved') }}: {{ new Date(activeSection.lastAutoSavedAt).toLocaleString('en-US') }}
              </div>
            </template>
          </div>

          <!-- AI Side Panel (for boilerplate generation in sections tab) -->
          <div v-if="showAiPanel" class="w-80 border-s border-gray-200 bg-gray-50 p-4 overflow-y-auto">
            <div class="flex items-center justify-between mb-4">
              <h3 class="text-sm font-semibold text-gray-700">{{ t('ai.title') }}</h3>
              <button @click="showAiPanel = false; boilerplateResult = null" class="text-gray-400 hover:text-gray-600">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            <div v-if="aiLoading" class="flex flex-col items-center py-8">
              <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600 mb-3"></div>
              <p class="text-sm text-gray-500">{{ t('ai.generating') }}</p>
            </div>

            <!-- AI Error -->
            <div v-else-if="aiError" class="bg-red-50 border border-red-200 rounded-lg p-4">
              <div class="flex items-center gap-2 mb-2">
                <svg class="w-5 h-5 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span class="text-sm font-medium text-red-700">{{ locale === 'ar' ? 'خطأ في المساعد الذكي' : 'AI Assistant Error' }}</span>
              </div>
              <p class="text-sm text-red-600">{{ aiError }}</p>
              <button @click="aiError = null; showAiPanel = false" class="mt-3 text-sm text-red-700 underline hover:text-red-800">
                {{ locale === 'ar' ? 'إغلاق' : 'Close' }}
              </button>
            </div>

            <!-- Boilerplate Result -->
            <div v-else-if="boilerplateResult" class="space-y-4">
              <div class="bg-white rounded-lg border border-gray-200 p-3">
                <div class="flex items-center gap-2 mb-2">
                  <span class="text-xs text-gray-500">{{ t('ai.provider') }}: {{ boilerplateResult.provider }}</span>
                  <span class="text-xs text-gray-500">{{ t('ai.confidence') }}: {{ (boilerplateResult.confidenceScore * 100).toFixed(0) }}%</span>
                </div>
                <div class="text-sm text-gray-700 max-h-96 overflow-y-auto" v-html="boilerplateResult.content"></div>
              </div>
              <div class="flex gap-2">
                <button @click="applyAiContent" class="flex-1 bg-primary-600 text-white px-3 py-2 rounded-lg text-sm hover:bg-primary-700">
                  {{ t('ai.applyContent') }}
                </button>
                <button @click="boilerplateResult = null; showAiPanel = false" class="flex-1 border border-gray-300 px-3 py-2 rounded-lg text-sm hover:bg-gray-50">
                  {{ t('ai.discardContent') }}
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Criteria Tab -->
        <div v-if="activeTab === 'criteria'" class="p-6">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-8">
            <!-- Technical Criteria -->
            <div>
              <div class="flex items-center justify-between mb-4">
                <h3 class="text-lg font-semibold text-gray-900">{{ t('criteria.technical') }}</h3>
                <span class="text-sm text-gray-500">{{ t('tenders.technicalWeight') }}: {{ tenderStore.currentTender.technicalWeight }}%</span>
              </div>
              <div v-if="getTechnicalCriteria().length === 0" class="text-center py-8 text-gray-400">
                {{ t('common.noData') }}
              </div>
              <div v-else class="space-y-3">
                <div v-for="criterion in getTechnicalCriteria()" :key="criterion.id" class="bg-gray-50 rounded-lg p-4 border border-gray-200">
                  <div class="flex items-center justify-between mb-2">
                    <span class="font-medium text-gray-900">{{ getTitle(criterion) }}</span>
                    <span class="text-sm font-semibold text-primary-600">{{ criterion.weight }}%</span>
                  </div>
                  <p v-if="criterion.descriptionAr || criterion.descriptionEn" class="text-sm text-gray-500">
                    {{ locale === 'ar' ? criterion.descriptionAr : criterion.descriptionEn }}
                  </p>
                  <div v-if="criterion.children?.length" class="mt-3 space-y-2 ps-4 border-s-2 border-gray-200">
                    <div v-for="child in criterion.children" :key="child.id" class="flex items-center justify-between">
                      <span class="text-sm text-gray-700">{{ getTitle(child) }}</span>
                      <span class="text-xs text-gray-500">{{ child.weight }}%</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <!-- Financial Criteria -->
            <div>
              <div class="flex items-center justify-between mb-4">
                <h3 class="text-lg font-semibold text-gray-900">{{ t('criteria.financial') }}</h3>
                <span class="text-sm text-gray-500">{{ t('tenders.financialWeight') }}: {{ tenderStore.currentTender.financialWeight }}%</span>
              </div>
              <div v-if="getFinancialCriteria().length === 0" class="text-center py-8 text-gray-400">
                {{ t('common.noData') }}
              </div>
              <div v-else class="space-y-3">
                <div v-for="criterion in getFinancialCriteria()" :key="criterion.id" class="bg-gray-50 rounded-lg p-4 border border-gray-200">
                  <div class="flex items-center justify-between mb-2">
                    <span class="font-medium text-gray-900">{{ getTitle(criterion) }}</span>
                    <span class="text-sm font-semibold text-green-600">{{ criterion.weight }}%</span>
                  </div>
                  <p v-if="criterion.descriptionAr || criterion.descriptionEn" class="text-sm text-gray-500">
                    {{ locale === 'ar' ? criterion.descriptionAr : criterion.descriptionEn }}
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- AI Tab -->
        <div v-if="activeTab === 'ai'" class="p-6">
          <!-- AI Action Cards (shown when no action is active) -->
          <div v-if="!aiActiveAction && !aiLoading" class="grid grid-cols-1 md:grid-cols-3 gap-6">
            <button @click="checkCompliance" class="border-2 border-dashed border-gray-300 rounded-xl p-8 text-center hover:border-primary-400 hover:bg-primary-50 transition-colors">
              <svg class="mx-auto h-12 w-12 text-primary-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
              </svg>
              <h3 class="mt-4 font-semibold text-gray-900">{{ t('ai.complianceCheck') }}</h3>
              <p class="mt-2 text-sm text-gray-500">
                {{ locale === 'ar' ? 'فحص مطابقة الكراسة لنظام المنافسات والمشتريات الحكومية' : 'Check booklet compliance with Government Procurement Law' }}
              </p>
            </button>
            <button @click="activeTab = 'sections'; showAiPanel = true" class="border-2 border-dashed border-gray-300 rounded-xl p-8 text-center hover:border-primary-400 hover:bg-primary-50 transition-colors">
              <svg class="mx-auto h-12 w-12 text-primary-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
              </svg>
              <h3 class="mt-4 font-semibold text-gray-900">{{ t('ai.generateContent') }}</h3>
              <p class="mt-2 text-sm text-gray-500">
                {{ locale === 'ar' ? 'توليد محتوى احترافي لأبواب الكراسة بالذكاء الاصطناعي' : 'Generate professional section content using AI' }}
              </p>
            </button>
            <button class="border-2 border-dashed border-gray-300 rounded-xl p-8 text-center hover:border-primary-400 hover:bg-primary-50 transition-colors">
              <svg class="mx-auto h-12 w-12 text-primary-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
              </svg>
              <h3 class="mt-4 font-semibold text-gray-900">{{ t('ai.suggestCriteria') }}</h3>
              <p class="mt-2 text-sm text-gray-500">
                {{ locale === 'ar' ? 'اقتراح معايير تقييم مناسبة بناءً على نوع المنافسة' : 'Suggest evaluation criteria based on tender type' }}
              </p>
            </button>
          </div>

          <!-- AI Loading State -->
          <div v-if="aiLoading" class="flex flex-col items-center justify-center py-16">
            <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600 mb-4"></div>
            <p class="text-lg font-medium text-gray-700">{{ locale === 'ar' ? 'جاري تحليل الكراسة بالذكاء الاصطناعي...' : 'Analyzing booklet with AI...' }}</p>
            <p class="text-sm text-gray-500 mt-2">{{ locale === 'ar' ? 'قد يستغرق هذا بضع ثوانٍ' : 'This may take a few seconds' }}</p>
          </div>

          <!-- AI Error State -->
          <div v-if="!aiLoading && aiError && aiActiveAction" class="max-w-2xl mx-auto">
            <div class="bg-red-50 border border-red-200 rounded-xl p-6">
              <div class="flex items-center gap-3 mb-3">
                <svg class="w-8 h-8 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <h3 class="text-lg font-semibold text-red-700">{{ locale === 'ar' ? 'خطأ في المساعد الذكي' : 'AI Assistant Error' }}</h3>
              </div>
              <p class="text-sm text-red-600 mb-4">{{ aiError }}</p>
              <button @click="resetAiState" class="bg-red-100 text-red-700 px-4 py-2 rounded-lg text-sm hover:bg-red-200 transition-colors">
                {{ locale === 'ar' ? 'العودة' : 'Go Back' }}
              </button>
            </div>
          </div>

          <!-- Compliance Result (shown inline in AI tab) -->
          <div v-if="!aiLoading && !aiError && complianceResult && aiActiveAction === 'compliance'" class="max-w-4xl mx-auto space-y-6">
            <!-- Back button -->
            <button @click="resetAiState" class="flex items-center gap-2 text-sm text-gray-500 hover:text-gray-700 transition-colors">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
              </svg>
              {{ locale === 'ar' ? 'العودة للمساعد الذكي' : 'Back to AI Assistant' }}
            </button>

            <!-- Compliance Status Header -->
            <div class="rounded-xl p-6" :class="complianceResult.isCompliant ? 'bg-green-50 border-2 border-green-200' : 'bg-red-50 border-2 border-red-200'">
              <div class="flex items-center gap-4">
                <div class="flex-shrink-0">
                  <svg class="w-12 h-12" :class="complianceResult.isCompliant ? 'text-green-500' : 'text-red-500'" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="complianceResult.isCompliant ? 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z' : 'M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z'" />
                  </svg>
                </div>
                <div>
                  <h2 class="text-xl font-bold" :class="complianceResult.isCompliant ? 'text-green-800' : 'text-red-800'">
                    {{ complianceResult.isCompliant ? t('ai.compliant') : t('ai.nonCompliant') }}
                  </h2>
                  <p class="text-sm mt-1" :class="complianceResult.isCompliant ? 'text-green-600' : 'text-red-600'">
                    {{ locale === 'ar' ? 'فحص الامتثال القانوني لنظام المنافسات والمشتريات الحكومية' : 'Legal Compliance Check for Government Procurement Law' }}
                  </p>
                </div>
                <div class="ms-auto">
                  <span class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium" :class="complianceResult.isCompliant ? 'bg-green-200 text-green-800' : 'bg-red-200 text-red-800'">
                    {{ complianceResult.issues?.length || 0 }} {{ locale === 'ar' ? 'ملاحظات' : 'Issues' }}
                  </span>
                </div>
              </div>
            </div>

            <!-- Summary -->
            <div class="bg-white rounded-xl border border-gray-200 p-6">
              <h3 class="text-lg font-semibold text-gray-900 mb-3">{{ locale === 'ar' ? 'ملخص التقييم' : 'Assessment Summary' }}</h3>
              <p class="text-sm text-gray-700 leading-relaxed whitespace-pre-line">{{ complianceResult.summary }}</p>
              <div class="flex items-center gap-4 mt-4 pt-4 border-t border-gray-100">
                <span class="text-xs text-gray-400">{{ t('ai.provider') }}: {{ complianceResult.provider }}</span>
                <span class="text-xs text-gray-400">{{ t('ai.model') || 'Model' }}: {{ complianceResult.model }}</span>
              </div>
            </div>

            <!-- Issues List -->
            <div v-if="complianceResult.issues?.length" class="space-y-4">
              <h3 class="text-lg font-semibold text-gray-900">{{ locale === 'ar' ? 'الملاحظات والتوصيات' : 'Issues & Recommendations' }}</h3>
              <div v-for="(issue, idx) in complianceResult.issues" :key="idx" class="bg-white rounded-xl border border-gray-200 p-5 hover:shadow-sm transition-shadow">
                <div class="flex items-start justify-between mb-3">
                  <div class="flex items-center gap-3">
                    <span class="flex items-center justify-center w-8 h-8 rounded-full bg-gray-100 text-sm font-bold text-gray-600">{{ idx + 1 }}</span>
                    <span class="font-medium text-gray-900">{{ issue.sectionTitle }}</span>
                  </div>
                  <span class="text-xs px-3 py-1 rounded-full font-medium"
                    :class="{
                      'bg-red-100 text-red-700': issue.severity === 'High',
                      'bg-yellow-100 text-yellow-700': issue.severity === 'Medium',
                      'bg-blue-100 text-blue-700': issue.severity === 'Low',
                    }">
                    {{ issue.severity === 'High' ? (locale === 'ar' ? 'عالية' : 'High') : issue.severity === 'Medium' ? (locale === 'ar' ? 'متوسطة' : 'Medium') : (locale === 'ar' ? 'منخفضة' : 'Low') }}
                  </span>
                </div>
                <div class="ps-11 space-y-3">
                  <div>
                    <p class="text-xs font-medium text-gray-500 mb-1">{{ locale === 'ar' ? 'المشكلة' : 'Issue' }}</p>
                    <p class="text-sm text-gray-700">{{ issue.issue }}</p>
                  </div>
                  <div class="bg-primary-50 rounded-lg p-3">
                    <p class="text-xs font-medium text-primary-700 mb-1">{{ locale === 'ar' ? 'التوصية' : 'Recommendation' }}</p>
                    <p class="text-sm text-primary-600">{{ issue.suggestion }}</p>
                  </div>
                </div>
              </div>
            </div>

            <!-- Re-check button -->
            <div class="flex justify-center pt-4">
              <button @click="checkCompliance" class="bg-primary-600 text-white px-6 py-3 rounded-lg hover:bg-primary-700 transition-colors text-sm font-medium flex items-center gap-2">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                </svg>
                {{ locale === 'ar' ? 'إعادة الفحص' : 'Re-check Compliance' }}
              </button>
            </div>
          </div>
        </div>

        <!-- Export Tab -->
        <div v-if="activeTab === 'export'" class="p-6">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6 max-w-2xl mx-auto">
            <button
              @click="handleExportPdf"
              :disabled="isExporting"
              class="border-2 border-gray-200 rounded-xl p-8 text-center hover:border-red-400 hover:bg-red-50 transition-colors disabled:opacity-50"
            >
              <svg class="mx-auto h-16 w-16 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
              </svg>
              <h3 class="mt-4 font-semibold text-gray-900">{{ t('exportDoc.pdf') }}</h3>
              <p class="mt-1 text-sm text-gray-500">PDF</p>
            </button>
            <button
              @click="handleExportDocx"
              :disabled="isExporting"
              class="border-2 border-gray-200 rounded-xl p-8 text-center hover:border-blue-400 hover:bg-blue-50 transition-colors disabled:opacity-50"
            >
              <svg class="mx-auto h-16 w-16 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
              </svg>
              <h3 class="mt-4 font-semibold text-gray-900">{{ t('exportDoc.docx') }}</h3>
              <p class="mt-1 text-sm text-gray-500">Word (DOCX)</p>
            </button>
          </div>
          <div v-if="isExporting" class="flex justify-center mt-6">
            <div class="flex items-center gap-2 text-primary-600">
              <div class="animate-spin rounded-full h-5 w-5 border-b-2 border-primary-600"></div>
              <span class="text-sm">{{ t('exportDoc.exporting') }}</span>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
