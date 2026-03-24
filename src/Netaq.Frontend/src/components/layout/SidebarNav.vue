<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '../../stores/auth'

defineProps<{
  collapsed: boolean
}>()

defineEmits<{
  toggle: []
}>()

const { t } = useI18n()
const route = useRoute()
const authStore = useAuthStore()

const settingsExpanded = ref(false)

const isAdmin = computed(() => {
  return authStore.user?.role === 'SystemAdmin' || authStore.user?.role === 'OrganizationAdmin'
})

const isSystemAdmin = computed(() => {
  return authStore.user?.role === 'SystemAdmin'
})

const navItems = computed(() => [
  {
    name: t('nav.dashboard'),
    path: '/dashboard',
    icon: 'M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6',
    show: true,
  },
  {
    name: t('nav.tasks'),
    path: '/tasks',
    icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4',
    show: true,
  },
  {
    name: t('nav.tenders'),
    path: '/tenders',
    icon: 'M19 20H5a2 2 0 01-2-2V6a2 2 0 012-2h10a2 2 0 012 2v1m2 13a2 2 0 01-2-2V7m2 13a2 2 0 002-2V9a2 2 0 00-2-2h-2m-4-3H9M7 16h6M7 8h6v4H7V8z',
    show: true,
  },
  {
    name: t('nav.templates'),
    path: '/templates',
    icon: 'M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2',
    show: true,
  },
  {
    name: t('nav.workflows'),
    path: '/workflows',
    icon: 'M13 10V3L4 14h7v7l9-11h-7z',
    show: true,
  },
  {
    name: t('nav.reports'),
    path: '/reports',
    icon: 'M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
    show: isAdmin.value,
  },
  {
    name: t('nav.audit'),
    path: '/audit',
    icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z',
    show: isAdmin.value,
  },
  {
    name: t('nav.userManagement'),
    path: '/admin/users',
    icon: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z',
    show: isAdmin.value,
  },
])

const settingsItems = computed(() => [
  {
    name: t('nav.orgSettings'),
    path: '/settings/organization',
    show: isAdmin.value,
  },
  {
    name: t('nav.aiConfig'),
    path: '/settings/ai',
    show: isSystemAdmin.value,
  },
  {
    name: t('nav.knowledgeBase'),
    path: '/settings/knowledge-base',
    show: isSystemAdmin.value,
  },
  {
    name: t('nav.systemSettings'),
    path: '/settings/system',
    show: isSystemAdmin.value,
  },
])

function isActive(path: string): boolean {
  return route.path.startsWith(path)
}

function isSettingsActive(): boolean {
  return route.path.startsWith('/settings')
}
</script>

<template>
  <aside
    class="bg-govtech-dark text-white transition-all duration-300 flex flex-col"
    :class="collapsed ? 'w-16' : 'w-64'"
  >
    <!-- Logo -->
    <div class="h-16 flex items-center justify-center border-b border-white/10 px-4">
      <template v-if="!collapsed">
        <div class="flex items-center gap-3">
          <img
            v-if="authStore.user?.organizationLogoUrl"
            :src="authStore.user.organizationLogoUrl"
            alt="Logo"
            class="w-8 h-8 rounded"
          />
          <div v-else class="w-8 h-8 bg-primary-500 rounded flex items-center justify-center text-sm font-bold">
            ن
          </div>
          <span class="font-bold text-lg">{{ t('common.appName') }}</span>
        </div>
      </template>
      <template v-else>
        <div class="w-8 h-8 bg-primary-500 rounded flex items-center justify-center text-sm font-bold">
          ن
        </div>
      </template>
    </div>

    <!-- Navigation -->
    <nav class="flex-1 py-4 space-y-1 px-2 overflow-y-auto">
      <router-link
        v-for="item in navItems.filter(i => i.show)"
        :key="item.path"
        :to="item.path"
        class="flex items-center gap-3 px-3 py-2.5 rounded-lg transition-colors"
        :class="[
          isActive(item.path)
            ? 'bg-white/15 text-white'
            : 'text-white/70 hover:bg-white/10 hover:text-white'
        ]"
      >
        <svg class="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="item.icon" />
        </svg>
        <span v-if="!collapsed" class="text-sm font-medium">{{ item.name }}</span>
      </router-link>

      <!-- Settings Section (Collapsible) -->
      <div v-if="isAdmin && settingsItems.some(i => i.show)">
        <button
          @click="settingsExpanded = !settingsExpanded"
          class="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg transition-colors"
          :class="[
            isSettingsActive()
              ? 'bg-white/15 text-white'
              : 'text-white/70 hover:bg-white/10 hover:text-white'
          ]"
        >
          <svg class="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.066 2.573c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.573 1.066c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.066-2.573c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
          </svg>
          <span v-if="!collapsed" class="text-sm font-medium flex-1 text-start">{{ t('nav.settings') }}</span>
          <svg v-if="!collapsed" class="w-4 h-4 transition-transform" :class="settingsExpanded ? 'rotate-90' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </button>

        <div v-if="(settingsExpanded || isSettingsActive()) && !collapsed" class="ms-4 mt-1 space-y-1">
          <router-link
            v-for="item in settingsItems.filter(i => i.show)"
            :key="item.path"
            :to="item.path"
            class="flex items-center gap-2 px-3 py-2 rounded-lg text-sm transition-colors"
            :class="[
              isActive(item.path)
                ? 'bg-white/15 text-white'
                : 'text-white/60 hover:bg-white/10 hover:text-white'
            ]"
          >
            <span class="w-1.5 h-1.5 rounded-full" :class="isActive(item.path) ? 'bg-primary-400' : 'bg-white/30'"></span>
            <span>{{ item.name }}</span>
          </router-link>
        </div>
      </div>
    </nav>

    <!-- Collapse Toggle -->
    <div class="p-2 border-t border-white/10">
      <button
        @click="$emit('toggle')"
        class="w-full flex items-center justify-center p-2 rounded-lg text-white/70 hover:bg-white/10 hover:text-white transition-colors"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            :d="collapsed ? 'M13 5l7 7-7 7' : 'M11 19l-7-7 7-7'"
          />
        </svg>
      </button>
    </div>
  </aside>
</template>
