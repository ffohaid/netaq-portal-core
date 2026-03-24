<script setup lang="ts">
import { onMounted } from 'vue'
import { getCurrentLocale } from './i18n'

onMounted(() => {
  const locale = getCurrentLocale()
  document.documentElement.dir = locale === 'ar' ? 'rtl' : 'ltr'
  document.documentElement.lang = locale
})
</script>

<template>
  <router-view />
</template>
