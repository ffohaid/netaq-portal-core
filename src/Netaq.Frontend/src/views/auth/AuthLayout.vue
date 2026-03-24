<script setup lang="ts">
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
</script>

<template>
  <div class="min-h-screen bg-gradient-to-br from-govtech-dark via-primary-700 to-primary-900 flex items-center justify-center p-4">
    <div class="w-full max-w-md">
      <!-- Logo & Title -->
      <div class="text-center mb-8">
        <div class="inline-flex items-center justify-center w-16 h-16 bg-white/10 backdrop-blur-sm rounded-2xl mb-4">
          <span class="text-3xl font-bold text-white">ن</span>
        </div>
        <h1 class="text-2xl font-bold text-white">{{ t('common.appName') }}</h1>
        <p class="text-white/70 mt-2 text-sm">{{ t('auth.loginSubtitle') }}</p>
      </div>

      <!-- Auth Card -->
      <div class="bg-white rounded-2xl shadow-2xl p-8">
        <router-view />
      </div>

      <!-- Footer -->
      <div class="text-center mt-6">
        <p class="text-white/50 text-xs">
          &copy; {{ new Date().getFullYear() }} {{ t('common.appName') }}
        </p>
      </div>
    </div>
  </div>
</template>
