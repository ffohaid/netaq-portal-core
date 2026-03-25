/**
 * NETAQ Portal — Auto-Save Composable
 * Provides reactive auto-save functionality for forms and multi-step wizards.
 * Automatically saves changes when the user navigates between steps.
 */

import { ref, watch, onBeforeUnmount, type Ref } from 'vue'

interface AutoSaveOptions {
  /** Delay in milliseconds before auto-saving (default: 2000) */
  delay?: number
  /** Whether to save immediately on step change (default: true) */
  saveOnStepChange?: boolean
}

export function useAutoSave<T>(
  data: Ref<T>,
  saveFn: (data: T) => Promise<boolean>,
  options: AutoSaveOptions = {}
) {
  const { delay = 2000, saveOnStepChange = true } = options

  const isSaving = ref(false)
  const lastSavedAt = ref<Date | null>(null)
  const hasUnsavedChanges = ref(false)
  const error = ref<string | null>(null)

  let timer: ReturnType<typeof setTimeout> | null = null
  let isFirstLoad = true

  // Watch for data changes and trigger auto-save
  watch(
    data,
    () => {
      if (isFirstLoad) {
        isFirstLoad = false
        return
      }
      hasUnsavedChanges.value = true
      error.value = null
      scheduleSave()
    },
    { deep: true }
  )

  function scheduleSave() {
    if (timer) clearTimeout(timer)
    timer = setTimeout(() => {
      performSave()
    }, delay)
  }

  async function performSave(): Promise<boolean> {
    if (isSaving.value) return false
    isSaving.value = true
    error.value = null

    try {
      const success = await saveFn(data.value)
      if (success) {
        lastSavedAt.value = new Date()
        hasUnsavedChanges.value = false
      }
      return success
    } catch (err: any) {
      error.value = err.message || 'Auto-save failed'
      return false
    } finally {
      isSaving.value = false
    }
  }

  /** Force save immediately (useful for step changes) */
  async function saveNow(): Promise<boolean> {
    if (timer) {
      clearTimeout(timer)
      timer = null
    }
    if (!hasUnsavedChanges.value) return true
    return performSave()
  }

  /** Call this when changing steps in a wizard */
  async function onStepChange(): Promise<boolean> {
    if (saveOnStepChange && hasUnsavedChanges.value) {
      return saveNow()
    }
    return true
  }

  // Cleanup on unmount
  onBeforeUnmount(() => {
    if (timer) clearTimeout(timer)
    // Try to save any unsaved changes
    if (hasUnsavedChanges.value) {
      performSave()
    }
  })

  return {
    isSaving,
    lastSavedAt,
    hasUnsavedChanges,
    error,
    saveNow,
    onStepChange,
  }
}
