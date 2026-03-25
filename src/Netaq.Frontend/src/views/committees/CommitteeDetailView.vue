<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useCommitteeStore } from '../../stores/committees'
import { getCurrentLocale } from '../../i18n'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useCommitteeStore()
const locale = computed(() => getCurrentLocale())
const showDissolveModal = ref(false)

const committeeId = route.params.id as string

function getName(item: any) {
  return locale.value === 'ar' ? (item.nameAr || item.fullNameAr) : (item.nameEn || item.fullNameEn)
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString(locale.value === 'ar' ? 'ar-SA' : 'en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  })
}

function getMemberRoleLabel(role: string) {
  const map: Record<string, string> = {
    Chair: t('committees.chair'),
    Member: t('committees.member'),
    Secretary: t('committees.secretary'),
  }
  return map[role] || role
}

function getMemberRoleClass(role: string) {
  const map: Record<string, string> = {
    Chair: 'bg-purple-100 text-purple-700',
    Member: 'bg-blue-100 text-blue-700',
    Secretary: 'bg-teal-100 text-teal-700',
  }
  return map[role] || 'bg-gray-100 text-gray-700'
}

async function handleDissolve() {
  const success = await store.dissolveCommittee(committeeId)
  if (success) {
    showDissolveModal.value = false
  }
}

async function handleRemoveMember(memberId: string) {
  await store.removeMember(committeeId, memberId)
}

onMounted(() => store.fetchCommittee(committeeId))
</script>

<template>
  <div class="p-6">
    <!-- Loading -->
    <div v-if="store.isLoading && !store.currentCommittee" class="flex justify-center py-12">
      <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-primary-600"></div>
    </div>

    <template v-else-if="store.currentCommittee">
      <!-- Header -->
      <div class="flex items-start justify-between mb-6">
        <div class="flex items-center gap-4">
          <button @click="router.push('/committees')" class="p-2 hover:bg-gray-100 rounded-lg transition-colors">
            <svg class="w-5 h-5 text-gray-600 rtl:rotate-180" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
          </button>
          <div>
            <h1 class="text-2xl font-bold text-gray-900">{{ getName(store.currentCommittee) }}</h1>
            <div class="flex items-center gap-3 mt-2">
              <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                :class="store.currentCommittee.type === 'Permanent' ? 'bg-blue-100 text-blue-700' : 'bg-amber-100 text-amber-700'">
                {{ store.currentCommittee.type === 'Permanent' ? t('committees.permanent') : t('committees.adHoc') }}
              </span>
              <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                :class="store.currentCommittee.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-200 text-gray-600'">
                {{ store.currentCommittee.isActive ? t('committees.active') : t('committees.dissolved') }}
              </span>
            </div>
          </div>
        </div>
        <button
          v-if="store.currentCommittee.isActive"
          @click="showDissolveModal = true"
          class="px-4 py-2 text-red-600 border border-red-300 rounded-lg hover:bg-red-50 transition-colors text-sm"
        >
          {{ t('committees.dissolve') }}
        </button>
      </div>

      <!-- Info Cards -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
          <div class="text-sm text-gray-500 mb-1">{{ t('committees.formedAt') }}</div>
          <div class="text-lg font-semibold text-gray-900">
            {{ store.currentCommittee.formedAt ? formatDate(store.currentCommittee.formedAt) : '-' }}
          </div>
        </div>
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
          <div class="text-sm text-gray-500 mb-1">{{ t('committees.members') }}</div>
          <div class="text-lg font-semibold text-gray-900">{{ store.currentCommittee.memberCount }}</div>
        </div>
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
          <div class="text-sm text-gray-500 mb-1">{{ t('committees.type') }}</div>
          <div class="text-lg font-semibold text-gray-900">
            {{ store.currentCommittee.type === 'Permanent' ? t('committees.permanent') : t('committees.adHoc') }}
          </div>
        </div>
      </div>

      <!-- Purpose -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 mb-6">
        <h2 class="text-lg font-semibold text-gray-900 mb-3">{{ t('committees.purpose') }}</h2>
        <p class="text-gray-600 leading-relaxed">
          {{ locale === 'ar' ? store.currentCommittee.purposeAr : store.currentCommittee.purposeEn }}
        </p>
      </div>

      <!-- Members -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <div class="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
          <h2 class="text-lg font-semibold text-gray-900">{{ t('committees.members') }}</h2>
        </div>
        <table class="w-full">
          <thead class="bg-gray-50 border-b border-gray-200">
            <tr>
              <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('committees.memberName') }}</th>
              <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('committees.memberEmail') }}</th>
              <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('committees.memberRole') }}</th>
              <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('committees.joinedAt') }}</th>
              <th class="text-start px-4 py-3 text-sm font-semibold text-gray-600">{{ t('common.actions') }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-for="member in store.currentCommittee.members" :key="member.id" class="hover:bg-gray-50">
              <td class="px-4 py-3">
                <div class="flex items-center gap-3">
                  <div class="w-8 h-8 bg-primary-100 text-primary-700 rounded-full flex items-center justify-center text-sm font-bold">
                    {{ getName(member).charAt(0) }}
                  </div>
                  <span class="text-sm font-medium text-gray-900">{{ getName(member) }}</span>
                </div>
              </td>
              <td class="px-4 py-3 text-sm text-gray-600">{{ member.email }}</td>
              <td class="px-4 py-3">
                <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium" :class="getMemberRoleClass(member.role)">
                  {{ getMemberRoleLabel(member.role) }}
                </span>
              </td>
              <td class="px-4 py-3 text-sm text-gray-600">{{ formatDate(member.joinedAt) }}</td>
              <td class="px-4 py-3">
                <button
                  v-if="store.currentCommittee.isActive && member.role !== 'Chair'"
                  @click="handleRemoveMember(member.id)"
                  class="text-red-600 hover:text-red-700 text-sm"
                >
                  {{ t('common.remove') }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>

    <!-- Dissolve Modal -->
    <Teleport to="body">
      <div v-if="showDissolveModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
        <div class="bg-white rounded-xl shadow-xl p-6 w-full max-w-md mx-4">
          <h3 class="text-lg font-semibold text-gray-900 mb-2">{{ t('committees.dissolveConfirm') }}</h3>
          <p class="text-gray-600 mb-6">{{ t('committees.dissolveWarning') }}</p>
          <div class="flex gap-3 justify-end">
            <button @click="showDissolveModal = false" class="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 text-sm">
              {{ t('common.cancel') }}
            </button>
            <button @click="handleDissolve" class="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 text-sm">
              {{ t('committees.dissolve') }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
