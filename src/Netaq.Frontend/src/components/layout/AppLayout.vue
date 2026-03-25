<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '../../stores/auth'
import { useSignalR } from '../../composables/useSignalR'
import { setLocale, getCurrentLocale } from '../../i18n'
import SidebarNav from './SidebarNav.vue'

const { t } = useI18n()
const router = useRouter()
const authStore = useAuthStore()
const { connect, on } = useSignalR()

const sidebarOpen = ref(true)
const notifications = ref<any[]>([])
const showNotifications = ref(false)
const currentLocale = ref(getCurrentLocale())
const showUserMenu = ref(false)

const isRtl = computed(() => currentLocale.value === 'ar')

function toggleLocale() {
  const newLocale = currentLocale.value === 'ar' ? 'en' : 'ar'
  setLocale(newLocale)
  currentLocale.value = newLocale
}

async function handleLogout() {
  await authStore.logout()
  router.push({ name: 'Login' })
}

onMounted(async () => {
  if (!authStore.user) {
    await authStore.fetchProfile()
  }
  
  // Connect SignalR
  connect()
  
  // Listen for real-time notifications
  on('TaskAssigned', (data) => {
    notifications.value.unshift({
      id: Date.now(),
      type: 'task',
      data,
      timestamp: new Date(),
      read: false,
    })
  })
  
  on('SlaStatusChanged', (data) => {
    notifications.value.unshift({
      id: Date.now(),
      type: 'sla',
      data,
      timestamp: new Date(),
      read: false,
    })
  })
  
  on('TaskEscalated', (data) => {
    notifications.value.unshift({
      id: Date.now(),
      type: 'escalation',
      data,
      timestamp: new Date(),
      read: false,
    })
  })
})

const unreadCount = computed(() => notifications.value.filter(n => !n.read).length)
</script>

<template>
  <div class="min-h-screen bg-gray-50 flex">
    <!-- Sidebar -->
    <SidebarNav :collapsed="!sidebarOpen" @toggle="sidebarOpen = !sidebarOpen" />

    <!-- Main Content -->
    <div class="flex-1 flex flex-col min-h-screen overflow-hidden">
      <!-- Top Header -->
      <header class="bg-white border-b border-gray-200 h-16 flex items-center justify-between px-6 sticky top-0 z-30">
        <!-- Right side in RTL / Left side in LTR -->
        <div class="flex items-center gap-4">
          <button @click="sidebarOpen = !sidebarOpen" class="text-gray-500 hover:text-gray-700 lg:hidden">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>
          <h1 class="text-lg font-semibold text-gray-800">{{ t('common.appName') }}</h1>
        </div>

        <!-- Left side in RTL / Right side in LTR -->
        <div class="flex items-center gap-4">
          <!-- Language Toggle -->
          <button
            @click="toggleLocale"
            class="px-3 py-1.5 text-sm font-medium rounded-lg border border-gray-300 hover:bg-gray-50 transition-colors"
          >
            {{ currentLocale === 'ar' ? 'EN' : 'عربي' }}
          </button>

          <!-- Notifications -->
          <div class="relative">
            <button
              @click="showNotifications = !showNotifications"
              class="relative p-2 text-gray-500 hover:text-gray-700 rounded-lg hover:bg-gray-100"
            >
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
              </svg>
              <span
                v-if="unreadCount > 0"
                class="absolute -top-1 -end-1 bg-danger-500 text-white text-xs rounded-full w-5 h-5 flex items-center justify-center"
              >
                {{ unreadCount > 9 ? '9+' : unreadCount }}
              </span>
            </button>
          </div>

          <!-- User Menu -->
          <div class="relative flex items-center gap-3">
            <button @click="showUserMenu = !showUserMenu" class="flex items-center gap-3 hover:bg-gray-50 rounded-lg px-3 py-1.5 transition-colors">
              <div class="text-end">
                <p class="text-sm font-medium text-gray-800">{{ authStore.userDisplayName }}</p>
                <p class="text-xs text-gray-500">{{ authStore.organizationName }}</p>
              </div>
              <div class="w-9 h-9 bg-gradient-to-br from-blue-500 to-indigo-600 rounded-full flex items-center justify-center text-white font-bold text-sm">
                {{ (authStore.userDisplayName || 'U').charAt(0) }}
              </div>
            </button>
            <!-- Dropdown -->
            <div v-if="showUserMenu" class="absolute top-full end-0 mt-2 w-56 bg-white rounded-xl shadow-lg border border-gray-200 py-2 z-50">
              <router-link :to="{ name: 'Profile' }" @click="showUserMenu = false" class="flex items-center gap-3 px-4 py-2.5 text-sm text-gray-700 hover:bg-gray-50 transition-colors">
                <svg class="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/></svg>
                {{ t('auth.profile') }}
              </router-link>
              <div class="border-t border-gray-100 my-1"></div>
              <button @click="handleLogout" class="flex items-center gap-3 px-4 py-2.5 text-sm text-red-600 hover:bg-red-50 w-full transition-colors">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" /></svg>
                {{ t('auth.logout') }}
              </button>
            </div>
          </div>
        </div>
      </header>

      <!-- Page Content -->
      <main class="flex-1 p-6">
        <router-view />
      </main>
    </div>
  </div>
</template>
