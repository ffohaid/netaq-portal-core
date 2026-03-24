<script setup lang="ts">
import { onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()
const tenderId = route.params.id as string

// Redirect to detail view for now - editing is done inline
onMounted(() => {
  router.replace(`/tenders/${tenderId}`)
})
</script>

<template>
  <div class="flex justify-center py-12">
    <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-primary-600"></div>
  </div>
</template>
