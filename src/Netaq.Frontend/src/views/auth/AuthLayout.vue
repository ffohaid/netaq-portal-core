<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { computed } from 'vue'

const { t, locale } = useI18n({ useScope: 'global' })
const isRtl = computed(() => locale.value === 'ar')
</script>

<template>
  <div class="min-h-screen flex" :dir="isRtl ? 'rtl' : 'ltr'">
    <!-- Left Panel - Branding (hidden on mobile) -->
    <div class="hidden lg:flex lg:w-1/2 relative overflow-hidden bg-gradient-to-br from-slate-900 via-blue-900 to-indigo-900">
      <!-- Animated Background Pattern -->
      <div class="absolute inset-0 opacity-10">
        <div class="absolute top-0 start-0 w-96 h-96 bg-blue-400 rounded-full mix-blend-multiply filter blur-3xl animate-pulse"></div>
        <div class="absolute bottom-0 end-0 w-96 h-96 bg-indigo-400 rounded-full mix-blend-multiply filter blur-3xl animate-pulse" style="animation-delay: 2s;"></div>
        <div class="absolute top-1/2 start-1/2 w-96 h-96 bg-purple-400 rounded-full mix-blend-multiply filter blur-3xl animate-pulse" style="animation-delay: 4s;"></div>
      </div>
      
      <!-- Grid Pattern -->
      <div class="absolute inset-0 opacity-5" style="background-image: radial-gradient(circle, rgba(255,255,255,0.3) 1px, transparent 1px); background-size: 30px 30px;"></div>
      
      <!-- Content -->
      <div class="relative z-10 flex flex-col justify-center items-center w-full px-12">
        <!-- Logo -->
        <div class="mb-8">
          <div class="inline-flex items-center justify-center w-24 h-24 bg-white/10 backdrop-blur-md rounded-3xl border border-white/20 shadow-2xl">
            <span class="text-5xl font-bold text-white" style="font-family: 'Noto Kufi Arabic', sans-serif;">ن</span>
          </div>
        </div>
        
        <!-- Title -->
        <h1 class="text-4xl font-bold text-white mb-4 text-center" style="font-family: 'Noto Kufi Arabic', sans-serif;">
          منصة نِطاق
        </h1>
        <p class="text-xl text-blue-200 mb-2 text-center">NETAQ Portal</p>
        <p class="text-blue-300/70 text-center max-w-md leading-relaxed mt-4">
          {{ isRtl ? 'منصة حكومية متكاملة لإدارة المنافسات والمشتريات الحكومية وفق أعلى المعايير' : 'An integrated government platform for managing tenders and procurement to the highest standards' }}
        </p>
        
        <!-- Feature Icons -->
        <div class="mt-12 grid grid-cols-3 gap-6">
          <div class="flex flex-col items-center text-center">
            <div class="w-12 h-12 bg-white/10 backdrop-blur-sm rounded-xl flex items-center justify-center mb-3 border border-white/10">
              <svg class="w-6 h-6 text-blue-300" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/></svg>
            </div>
            <span class="text-white/60 text-xs">{{ isRtl ? 'أمان متقدم' : 'Advanced Security' }}</span>
          </div>
          <div class="flex flex-col items-center text-center">
            <div class="w-12 h-12 bg-white/10 backdrop-blur-sm rounded-xl flex items-center justify-center mb-3 border border-white/10">
              <svg class="w-6 h-6 text-blue-300" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M13 10V3L4 14h7v7l9-11h-7z"/></svg>
            </div>
            <span class="text-white/60 text-xs">{{ isRtl ? 'ذكاء اصطناعي' : 'AI Powered' }}</span>
          </div>
          <div class="flex flex-col items-center text-center">
            <div class="w-12 h-12 bg-white/10 backdrop-blur-sm rounded-xl flex items-center justify-center mb-3 border border-white/10">
              <svg class="w-6 h-6 text-blue-300" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/></svg>
            </div>
            <span class="text-white/60 text-xs">{{ isRtl ? 'تقارير شاملة' : 'Full Reports' }}</span>
          </div>
        </div>
      </div>
      
      <!-- Bottom Copyright -->
      <div class="absolute bottom-6 start-0 end-0 text-center">
        <p class="text-white/30 text-xs">&copy; {{ new Date().getFullYear() }} NETAQ Portal v5.7</p>
      </div>
    </div>
    
    <!-- Right Panel - Auth Form -->
    <div class="w-full lg:w-1/2 flex items-center justify-center bg-gray-50 p-6 sm:p-8">
      <div class="w-full max-w-md">
        <!-- Mobile Logo (shown only on small screens) -->
        <div class="lg:hidden text-center mb-8">
          <div class="inline-flex items-center justify-center w-16 h-16 bg-gradient-to-br from-blue-600 to-indigo-700 rounded-2xl mb-4 shadow-lg">
            <span class="text-3xl font-bold text-white" style="font-family: 'Noto Kufi Arabic', sans-serif;">ن</span>
          </div>
          <h1 class="text-2xl font-bold text-gray-900" style="font-family: 'Noto Kufi Arabic', sans-serif;">منصة نِطاق</h1>
          <p class="text-gray-500 mt-1 text-sm">NETAQ Portal</p>
        </div>
        
        <!-- Auth Card -->
        <div class="bg-white rounded-2xl shadow-xl border border-gray-100 p-8">
          <router-view />
        </div>
        
        <!-- Footer -->
        <div class="text-center mt-6">
          <p class="text-gray-400 text-xs">
            &copy; {{ new Date().getFullYear() }} {{ t('common.appName') }} — {{ isRtl ? 'جميع الحقوق محفوظة' : 'All rights reserved' }}
          </p>
        </div>
      </div>
    </div>
  </div>
</template>
