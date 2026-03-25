/**
 * NETAQ Portal — Formatting Utilities
 * Supports Hijri (Umm al-Qura) & Gregorian calendars,
 * consistent Arabic/English number formatting, and SAR currency.
 *
 * Uses native Intl APIs — no external dependencies required.
 */

import { getCurrentLocale } from '../i18n'

// ─── Date Formatting ────────────────────────────────────────────────

/**
 * Format a date string as Gregorian date in the current locale.
 */
export function formatGregorianDate(dateStr: string | Date, style: 'short' | 'medium' | 'long' = 'medium'): string {
  if (!dateStr) return '—'
  const date = typeof dateStr === 'string' ? new Date(dateStr) : dateStr
  if (isNaN(date.getTime())) return '—'

  const locale = getCurrentLocale() === 'ar' ? 'ar-SA' : 'en-US'
  const options: Intl.DateTimeFormatOptions =
    style === 'short'
      ? { year: 'numeric', month: '2-digit', day: '2-digit' }
      : style === 'long'
        ? { year: 'numeric', month: 'long', day: 'numeric', weekday: 'long' }
        : { year: 'numeric', month: 'short', day: 'numeric' }

  return new Intl.DateTimeFormat(locale, { ...options, calendar: 'gregory' }).format(date)
}

/**
 * Format a date string as Hijri (Umm al-Qura) date.
 */
export function formatHijriDate(dateStr: string | Date, style: 'short' | 'medium' | 'long' = 'medium'): string {
  if (!dateStr) return '—'
  const date = typeof dateStr === 'string' ? new Date(dateStr) : dateStr
  if (isNaN(date.getTime())) return '—'

  const locale = getCurrentLocale() === 'ar' ? 'ar-SA-u-ca-islamic-umalqura' : 'en-SA-u-ca-islamic-umalqura'
  const options: Intl.DateTimeFormatOptions =
    style === 'short'
      ? { year: 'numeric', month: '2-digit', day: '2-digit' }
      : style === 'long'
        ? { year: 'numeric', month: 'long', day: 'numeric', weekday: 'long' }
        : { year: 'numeric', month: 'short', day: 'numeric' }

  return new Intl.DateTimeFormat(locale, options).format(date)
}

/**
 * Format a date showing both Hijri and Gregorian.
 * Example: "٢٧ رمضان ١٤٤٧ هـ — 25 Mar 2026"
 */
export function formatDualDate(dateStr: string | Date, style: 'short' | 'medium' | 'long' = 'medium'): string {
  if (!dateStr) return '—'
  const hijri = formatHijriDate(dateStr, style)
  const gregorian = formatGregorianDate(dateStr, style)
  return `${hijri} — ${gregorian}`
}

/**
 * Format a date with time.
 */
export function formatDateTime(dateStr: string | Date): string {
  if (!dateStr) return '—'
  const date = typeof dateStr === 'string' ? new Date(dateStr) : dateStr
  if (isNaN(date.getTime())) return '—'

  const locale = getCurrentLocale() === 'ar' ? 'ar-SA' : 'en-US'
  return new Intl.DateTimeFormat(locale, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    calendar: 'gregory',
  }).format(date)
}

/**
 * Get relative time (e.g., "منذ 3 ساعات" / "3 hours ago").
 */
export function formatRelativeTime(dateStr: string | Date): string {
  if (!dateStr) return '—'
  const date = typeof dateStr === 'string' ? new Date(dateStr) : dateStr
  if (isNaN(date.getTime())) return '—'

  const locale = getCurrentLocale() === 'ar' ? 'ar-SA' : 'en-US'
  const now = Date.now()
  const diffMs = now - date.getTime()
  const diffSec = Math.floor(diffMs / 1000)
  const diffMin = Math.floor(diffSec / 60)
  const diffHr = Math.floor(diffMin / 60)
  const diffDay = Math.floor(diffHr / 24)

  const rtf = new Intl.RelativeTimeFormat(locale, { numeric: 'auto' })

  if (diffDay > 30) return formatGregorianDate(date)
  if (diffDay > 0) return rtf.format(-diffDay, 'day')
  if (diffHr > 0) return rtf.format(-diffHr, 'hour')
  if (diffMin > 0) return rtf.format(-diffMin, 'minute')
  return rtf.format(-diffSec, 'second')
}

// ─── Number Formatting ──────────────────────────────────────────────

/**
 * Format a number with locale-consistent digits.
 * Uses Eastern Arabic numerals (٠١٢٣) for Arabic locale.
 */
export function formatNumber(value: number | string | undefined | null, decimals = 0): string {
  if (value === undefined || value === null || value === '') return '—'
  const num = typeof value === 'string' ? parseFloat(value) : value
  if (isNaN(num)) return '—'

  const locale = getCurrentLocale() === 'ar' ? 'ar-SA' : 'en-US'
  return new Intl.NumberFormat(locale, {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(num)
}

/**
 * Format a number as SAR currency.
 */
export function formatCurrency(value: number | string | undefined | null): string {
  if (value === undefined || value === null || value === '') return '—'
  const num = typeof value === 'string' ? parseFloat(value) : value
  if (isNaN(num)) return '—'

  const locale = getCurrentLocale() === 'ar' ? 'ar-SA' : 'en-US'
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency: 'SAR',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(num)
}

/**
 * Format a number as percentage.
 */
export function formatPercentage(value: number | string | undefined | null, decimals = 0): string {
  if (value === undefined || value === null || value === '') return '—'
  const num = typeof value === 'string' ? parseFloat(value) : value
  if (isNaN(num)) return '—'

  const locale = getCurrentLocale() === 'ar' ? 'ar-SA' : 'en-US'
  return new Intl.NumberFormat(locale, {
    style: 'percent',
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(num / 100)
}

// ─── Auto-Save Helper ───────────────────────────────────────────────

/**
 * Creates a debounced auto-save function.
 * Usage: const autoSave = createAutoSave(saveFn, 2000)
 */
export function createAutoSave<T extends (...args: any[]) => Promise<any>>(
  saveFn: T,
  delayMs = 2000
): {
  trigger: (...args: Parameters<T>) => void
  cancel: () => void
  isPending: () => boolean
} {
  let timer: ReturnType<typeof setTimeout> | null = null
  let pending = false

  return {
    trigger(...args: Parameters<T>) {
      pending = true
      if (timer) clearTimeout(timer)
      timer = setTimeout(async () => {
        try {
          await saveFn(...args)
        } finally {
          pending = false
          timer = null
        }
      }, delayMs)
    },
    cancel() {
      if (timer) {
        clearTimeout(timer)
        timer = null
        pending = false
      }
    },
    isPending() {
      return pending
    },
  }
}
