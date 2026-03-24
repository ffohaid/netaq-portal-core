<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useTenderStore } from '../../stores/tenders'
import { getCurrentLocale } from '../../i18n'
import type { TenderType, BookletCreationMethod, CreateTenderRequest, BookletTemplate } from '../../types'

const { t } = useI18n()
const router = useRouter()
const tenderStore = useTenderStore()
const locale = computed(() => getCurrentLocale())

// Wizard state
const currentStep = ref(1)
const totalSteps = 5
const isSubmitting = ref(false)

// Form data
const form = ref<CreateTenderRequest>({
  titleAr: '',
  titleEn: '',
  descriptionAr: '',
  descriptionEn: '',
  tenderType: 'GeneralSupply',
  estimatedValue: 0,
  creationMethod: 'FromTemplate',
  bookletTemplateId: undefined,
  submissionOpenDate: undefined,
  submissionCloseDate: undefined,
  projectStartDate: undefined,
  projectEndDate: undefined,
  technicalWeight: 60,
  financialWeight: 40,
})

// Template selection
const selectedTemplate = ref<BookletTemplate | null>(null)

const tenderTypes: TenderType[] = [
  'GeneralSupply', 'PharmaceuticalSupply', 'MedicalSupply', 'MilitarySupply',
  'GeneralServices', 'CateringServices', 'CityCleaning', 'BuildingMaintenance',
  'GeneralConsulting', 'EngineeringDesign', 'EngineeringSupervision',
  'GeneralConstruction', 'RoadConstruction', 'RoadMaintenance',
  'InformationTechnology',
  'FrameworkAgreementSupply', 'FrameworkAgreementServices', 'FrameworkAgreementConsulting',
  'RevenueSharing', 'PerformanceBasedContract', 'CapacityStudy',
]

const creationMethods: BookletCreationMethod[] = ['FromTemplate', 'AiExtraction', 'ManualEntry']

const steps = computed(() => [
  { id: 1, title: t('tenders.steps.basicInfo'), icon: 'M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z' },
  { id: 2, title: t('tenders.steps.selectTemplate'), icon: 'M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2' },
  { id: 3, title: t('tenders.steps.editSections'), icon: 'M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z' },
  { id: 4, title: t('tenders.steps.evaluationCriteria'), icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z' },
  { id: 5, title: t('tenders.steps.reviewSubmit'), icon: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z' },
])

// Validation
const isStep1Valid = computed(() => {
  return form.value.titleAr.trim() !== '' &&
    form.value.titleEn.trim() !== '' &&
    form.value.estimatedValue > 0
})

const isStep2Valid = computed(() => {
  if (form.value.creationMethod === 'FromTemplate') {
    return !!form.value.bookletTemplateId
  }
  return true
})

// Watch tender type to filter templates
watch(() => form.value.tenderType, async () => {
  if (form.value.creationMethod === 'FromTemplate') {
    await tenderStore.fetchTemplates({ tenderType: form.value.tenderType })
  }
})

// Watch financial/technical weight sync
watch(() => form.value.technicalWeight, (val) => {
  form.value.financialWeight = 100 - val
})

function selectTemplate(template: BookletTemplate) {
  selectedTemplate.value = template
  form.value.bookletTemplateId = template.id
}

function getTemplateName(template: BookletTemplate) {
  return locale.value === 'ar' ? template.nameAr : template.nameEn
}

function getTemplateDesc(template: BookletTemplate) {
  return locale.value === 'ar' ? template.descriptionAr : template.descriptionEn
}

async function goToStep(step: number) {
  // Auto-save when moving between steps
  if (step > currentStep.value) {
    if (currentStep.value === 1 && !isStep1Valid.value) return
    if (currentStep.value === 2 && !isStep2Valid.value) return
  }

  if (step === 2 && form.value.creationMethod === 'FromTemplate') {
    await tenderStore.fetchTemplates({ tenderType: form.value.tenderType })
  }

  currentStep.value = step
}

async function handleSubmit() {
  isSubmitting.value = true
  try {
    const result = await tenderStore.createTender(form.value)
    if (result) {
      router.push(`/tenders/${result.id}`)
    }
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="p-6 max-w-5xl mx-auto">
    <!-- Header -->
    <div class="flex items-center gap-4 mb-8">
      <button @click="router.push('/tenders')" class="text-gray-500 hover:text-gray-700">
        <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
        </svg>
      </button>
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ t('tenders.create') }}</h1>
      </div>
    </div>

    <!-- Stepper -->
    <div class="mb-8">
      <div class="flex items-center justify-between">
        <div
          v-for="step in steps"
          :key="step.id"
          class="flex items-center"
          :class="step.id < totalSteps ? 'flex-1' : ''"
        >
          <button
            @click="goToStep(step.id)"
            class="flex items-center gap-2"
            :class="step.id <= currentStep ? 'text-primary-600' : 'text-gray-400'"
          >
            <div
              class="w-10 h-10 rounded-full flex items-center justify-center text-sm font-bold border-2 transition-colors"
              :class="[
                step.id < currentStep ? 'bg-primary-600 border-primary-600 text-white' :
                step.id === currentStep ? 'border-primary-600 text-primary-600 bg-primary-50' :
                'border-gray-300 text-gray-400'
              ]"
            >
              <svg v-if="step.id < currentStep" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
              </svg>
              <span v-else>{{ step.id }}</span>
            </div>
            <span class="hidden md:block text-sm font-medium">{{ step.title }}</span>
          </button>
          <div
            v-if="step.id < totalSteps"
            class="flex-1 h-0.5 mx-4"
            :class="step.id < currentStep ? 'bg-primary-600' : 'bg-gray-200'"
          ></div>
        </div>
      </div>
    </div>

    <!-- Step 1: Basic Information -->
    <div v-if="currentStep === 1" class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
      <h2 class="text-lg font-semibold text-gray-900 mb-6">{{ t('tenders.steps.basicInfo') }}</h2>
      <div class="space-y-6">
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('tenders.titleAr') }} *</label>
            <input v-model="form.titleAr" type="text" dir="rtl" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('tenders.titleEn') }} *</label>
            <input v-model="form.titleEn" type="text" dir="ltr" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500" />
          </div>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('tenders.descriptionAr') }}</label>
            <textarea v-model="form.descriptionAr" rows="3" dir="rtl" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"></textarea>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('tenders.descriptionEn') }}</label>
            <textarea v-model="form.descriptionEn" rows="3" dir="ltr" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"></textarea>
          </div>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('tenders.tenderType') }} *</label>
            <select v-model="form.tenderType" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500">
              <option v-for="type in tenderTypes" :key="type" :value="type">{{ t(`tenders.type.${type}`) }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('tenders.estimatedValue') }} ({{ t('common.sar') }}) *</label>
            <input v-model.number="form.estimatedValue" type="number" min="0" step="1000" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500" />
          </div>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('tenders.creationMethod') }}</label>
            <select v-model="form.creationMethod" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500">
              <option v-for="method in creationMethods" :key="method" :value="method">{{ t(`tenders.method.${method}`) }}</option>
            </select>
          </div>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('tenders.technicalWeight') }} (%)</label>
            <input v-model.number="form.technicalWeight" type="number" min="0" max="100" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('tenders.financialWeight') }} (%)</label>
            <input :value="form.financialWeight" type="number" disabled class="w-full px-4 py-2.5 border border-gray-200 rounded-lg bg-gray-50 text-gray-500" />
          </div>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('tenders.submissionOpenDate') }}</label>
            <input v-model="form.submissionOpenDate" type="date" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('tenders.submissionCloseDate') }}</label>
            <input v-model="form.submissionCloseDate" type="date" class="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500" />
          </div>
        </div>
      </div>
    </div>

    <!-- Step 2: Select Template -->
    <div v-if="currentStep === 2" class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
      <h2 class="text-lg font-semibold text-gray-900 mb-6">{{ t('tenders.steps.selectTemplate') }}</h2>

      <div v-if="form.creationMethod === 'FromTemplate'">
        <div v-if="tenderStore.isLoading" class="flex justify-center py-8">
          <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
        </div>
        <div v-else-if="tenderStore.templates.length === 0" class="text-center py-8 text-gray-500">
          {{ t('common.noData') }}
        </div>
        <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div
            v-for="template in tenderStore.templates"
            :key="template.id"
            @click="selectTemplate(template)"
            class="border-2 rounded-xl p-4 cursor-pointer transition-all"
            :class="form.bookletTemplateId === template.id ? 'border-primary-500 bg-primary-50' : 'border-gray-200 hover:border-primary-300'"
          >
            <div class="flex items-start justify-between">
              <div class="flex-1">
                <h3 class="font-semibold text-gray-900">{{ getTemplateName(template) }}</h3>
                <p class="text-sm text-gray-500 mt-1">{{ getTemplateDesc(template) }}</p>
                <div class="flex items-center gap-4 mt-3 text-xs text-gray-400">
                  <span>{{ t('templates.version') }}: {{ template.version }}</span>
                  <span>{{ t('templates.sectionCount') }}: {{ template.sectionCount }}</span>
                </div>
              </div>
              <div
                v-if="form.bookletTemplateId === template.id"
                class="w-6 h-6 bg-primary-600 rounded-full flex items-center justify-center flex-shrink-0"
              >
                <svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                </svg>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div v-else-if="form.creationMethod === 'AiExtraction'" class="text-center py-8">
        <svg class="mx-auto h-16 w-16 text-primary-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
        </svg>
        <h3 class="mt-4 text-lg font-medium text-gray-900">{{ t('ai.title') }}</h3>
        <p class="mt-2 text-gray-500">{{ t('ai.generating') }}</p>
      </div>

      <div v-else class="text-center py-8">
        <svg class="mx-auto h-16 w-16 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
        </svg>
        <h3 class="mt-4 text-lg font-medium text-gray-900">{{ t('tenders.method.ManualEntry') }}</h3>
        <p class="mt-2 text-gray-500">{{ t('sections.title') }}</p>
      </div>
    </div>

    <!-- Step 3: Preview Sections (will be editable after creation) -->
    <div v-if="currentStep === 3" class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
      <h2 class="text-lg font-semibold text-gray-900 mb-6">{{ t('tenders.steps.editSections') }}</h2>
      <div class="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
        <div class="flex items-start gap-3">
          <svg class="w-5 h-5 text-blue-600 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <p class="text-sm text-blue-700">
            {{ locale === 'ar'
              ? 'سيتم إنشاء أبواب الكراسة تلقائياً بناءً على النموذج المختار. يمكنك تحرير كل باب بعد إنشاء المنافسة.'
              : 'Booklet sections will be automatically created based on the selected template. You can edit each section after creating the tender.'
            }}
          </p>
        </div>
      </div>
      <div class="space-y-3">
        <div v-for="(sectionType, index) in ['GeneralTermsAndConditions', 'TechnicalScopeAndSpecifications', 'QualificationRequirements', 'EvaluationCriteria', 'FinancialTerms', 'ContractualTerms', 'LocalContentRequirements', 'AppendicesAndForms']" :key="sectionType" class="flex items-center gap-4 p-4 bg-gray-50 rounded-lg">
          <div class="w-8 h-8 bg-primary-100 text-primary-700 rounded-lg flex items-center justify-center text-sm font-bold">
            {{ index + 1 }}
          </div>
          <div>
            <p class="font-medium text-gray-900">{{ t(`sections.type.${sectionType}`) }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Step 4: Evaluation Criteria Preview -->
    <div v-if="currentStep === 4" class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
      <h2 class="text-lg font-semibold text-gray-900 mb-6">{{ t('tenders.steps.evaluationCriteria') }}</h2>
      <div class="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
        <div class="flex items-start gap-3">
          <svg class="w-5 h-5 text-blue-600 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <p class="text-sm text-blue-700">
            {{ locale === 'ar'
              ? 'يمكنك إضافة وتعديل معايير التقييم بالتفصيل بعد إنشاء المنافسة. كما يمكنك استخدام الذكاء الاصطناعي لاقتراح معايير مناسبة.'
              : 'You can add and edit evaluation criteria in detail after creating the tender. You can also use AI to suggest appropriate criteria.'
            }}
          </p>
        </div>
      </div>
      <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div class="border border-gray-200 rounded-xl p-6">
          <h3 class="font-semibold text-gray-900 mb-2">{{ t('criteria.technical') }}</h3>
          <div class="text-3xl font-bold text-primary-600">{{ form.technicalWeight }}%</div>
          <p class="text-sm text-gray-500 mt-1">{{ t('tenders.technicalWeight') }}</p>
        </div>
        <div class="border border-gray-200 rounded-xl p-6">
          <h3 class="font-semibold text-gray-900 mb-2">{{ t('criteria.financial') }}</h3>
          <div class="text-3xl font-bold text-green-600">{{ form.financialWeight }}%</div>
          <p class="text-sm text-gray-500 mt-1">{{ t('tenders.financialWeight') }}</p>
        </div>
      </div>
    </div>

    <!-- Step 5: Review & Submit -->
    <div v-if="currentStep === 5" class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
      <h2 class="text-lg font-semibold text-gray-900 mb-6">{{ t('tenders.steps.reviewSubmit') }}</h2>
      <div class="space-y-4">
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div class="bg-gray-50 rounded-lg p-4">
            <p class="text-xs text-gray-500 mb-1">{{ t('tenders.titleAr') }}</p>
            <p class="font-medium text-gray-900">{{ form.titleAr }}</p>
          </div>
          <div class="bg-gray-50 rounded-lg p-4">
            <p class="text-xs text-gray-500 mb-1">{{ t('tenders.titleEn') }}</p>
            <p class="font-medium text-gray-900">{{ form.titleEn }}</p>
          </div>
          <div class="bg-gray-50 rounded-lg p-4">
            <p class="text-xs text-gray-500 mb-1">{{ t('tenders.tenderType') }}</p>
            <p class="font-medium text-gray-900">{{ t(`tenders.type.${form.tenderType}`) }}</p>
          </div>
          <div class="bg-gray-50 rounded-lg p-4">
            <p class="text-xs text-gray-500 mb-1">{{ t('tenders.estimatedValue') }}</p>
            <p class="font-medium text-gray-900">{{ form.estimatedValue.toLocaleString() }} {{ t('common.sar') }}</p>
          </div>
          <div class="bg-gray-50 rounded-lg p-4">
            <p class="text-xs text-gray-500 mb-1">{{ t('tenders.creationMethod') }}</p>
            <p class="font-medium text-gray-900">{{ t(`tenders.method.${form.creationMethod}`) }}</p>
          </div>
          <div class="bg-gray-50 rounded-lg p-4">
            <p class="text-xs text-gray-500 mb-1">{{ t('tenders.technicalWeight') }} / {{ t('tenders.financialWeight') }}</p>
            <p class="font-medium text-gray-900">{{ form.technicalWeight }}% / {{ form.financialWeight }}%</p>
          </div>
        </div>
        <div v-if="selectedTemplate" class="bg-primary-50 border border-primary-200 rounded-lg p-4">
          <p class="text-xs text-primary-600 mb-1">{{ t('templates.selectTemplate') }}</p>
          <p class="font-medium text-primary-900">{{ getTemplateName(selectedTemplate) }}</p>
        </div>
      </div>

      <!-- Error -->
      <div v-if="tenderStore.error" class="mt-4 bg-red-50 border border-red-200 rounded-lg p-4">
        <p class="text-sm text-red-700">{{ tenderStore.error }}</p>
      </div>
    </div>

    <!-- Navigation Buttons -->
    <div class="flex items-center justify-between mt-6">
      <button
        v-if="currentStep > 1"
        @click="currentStep--"
        class="px-6 py-2.5 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-50 transition-colors"
      >
        {{ t('common.previous') }}
      </button>
      <div v-else></div>
      <div class="flex gap-3">
        <button
          @click="router.push('/tenders')"
          class="px-6 py-2.5 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-50 transition-colors"
        >
          {{ t('common.cancel') }}
        </button>
        <button
          v-if="currentStep < totalSteps"
          @click="goToStep(currentStep + 1)"
          :disabled="(currentStep === 1 && !isStep1Valid) || (currentStep === 2 && !isStep2Valid)"
          class="px-6 py-2.5 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {{ t('common.next') }}
        </button>
        <button
          v-else
          @click="handleSubmit"
          :disabled="isSubmitting"
          class="px-6 py-2.5 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50 flex items-center gap-2"
        >
          <div v-if="isSubmitting" class="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
          {{ t('tenders.create') }}
        </button>
      </div>
    </div>
  </div>
</template>
