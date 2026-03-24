<script setup lang="ts">
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-50">
    <div class="text-center">
      <h1 class="text-6xl font-bold text-gray-300 mb-4">404</h1>
      <p class="text-xl text-gray-500 mb-6">Page not found</p>
      <router-link to="/dashboard" class="btn-primary">
        {{ t('nav.dashboard') }}
      </router-link>
    </div>
  </div>
</template>
