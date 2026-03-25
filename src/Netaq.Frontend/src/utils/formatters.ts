/**
 * NETAQ Portal — Formatting Utilities
 * Supports Hijri (Umm al-Qura) & Gregorian calendars,
 * consistent English numeral formatting, and SAR currency with new symbol.
 *
 * IMPORTANT: All numbers are displayed using English/Western numerals (0-9)
 * regardless of the current locale, per government platform standards.
 *
 * Uses native Intl APIs — no external dependencies required.
 */

import { getCurrentLocale } from '../i18n'

// ─── Numeral Normalization ─────────────────────────────────────────
// Map Eastern Arabic (٠-٩) and Extended Arabic-Indic (۰-۹) to Western (0-9)
const easternArabicMap: Record<string, string> = {
  '\u0660': '0', '\u0661': '1', '\u0662': '2', '\u0663': '3', '\u0664': '4',
  '\u0665': '5', '\u0666': '6', '\u0667': '7', '\u0668': '8', '\u0669': '9',
  '\u06F0': '0', '\u06F1': '1', '\u06F2': '2', '\u06F3': '3', '\u06F4': '4',
  '\u06F5': '5', '\u06F6': '6', '\u06F7': '7', '\u06F8': '8', '\u06F9': '9',
}

/**
 * Convert any Eastern Arabic / Extended Arabic-Indic numerals to Western (0-9).
 * Preserves all other characters (Arabic text, punctuation, etc.).
 */
export function toWesternNumerals(str: string): string {
  if (!str) return str
  return str.replace(/[\u0660-\u0669\u06F0-\u06F9]/g, (ch) => easternArabicMap[ch] || ch)
}

// ─── SAR Currency Symbol ───────────────────────────────────────────
// The new Saudi Riyal symbol (Unicode U+FDFC or custom representation)
const SAR_SYMBOL = 'ر.س'

// ─── Date Formatting ────────────────────────────────────────────────

/**
 * Format a date string as Gregorian date in the current locale.
 * Always uses Western numerals.
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

  const formatted = new Intl.DateTimeFormat(locale, { ...options, calendar: 'gregory' }).format(date)
  return toWesternNumerals(formatted)
}

/**
 * Format a date string as Hijri (Umm al-Qura) date.
 * Always uses Western numerals.
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

  const formatted = new Intl.DateTimeFormat(locale, options).format(date)
  return toWesternNumerals(formatted)
}

/**
 * Format a date showing both Hijri and Gregorian.
 * Example: "27 رمضان 1447 هـ — 25 Mar 2026"
 */
export function formatDualDate(dateStr: string | Date, style: 'short' | 'medium' | 'long' = 'medium'): string {
  if (!dateStr) return '—'
  const hijri = formatHijriDate(dateStr, style)
  const gregorian = formatGregorianDate(dateStr, style)
  return `${hijri} — ${gregorian}`
}

/**
 * Format a date with time. Always uses Western numerals.
 */
export function formatDateTime(dateStr: string | Date): string {
  if (!dateStr) return '—'
  const date = typeof dateStr === 'string' ? new Date(dateStr) : dateStr
  if (isNaN(date.getTime())) return '—'

  const locale = getCurrentLocale() === 'ar' ? 'ar-SA' : 'en-US'
  const formatted = new Intl.DateTimeFormat(locale, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    calendar: 'gregory',
  }).format(date)
  return toWesternNumerals(formatted)
}

/**
 * Get relative time (e.g., "منذ 3 ساعات" / "3 hours ago").
 * Always uses Western numerals.
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

  let result: string
  if (diffDay > 30) return formatGregorianDate(date)
  if (diffDay > 0) result = rtf.format(-diffDay, 'day')
  else if (diffHr > 0) result = rtf.format(-diffHr, 'hour')
  else if (diffMin > 0) result = rtf.format(-diffMin, 'minute')
  else result = rtf.format(-diffSec, 'second')

  return toWesternNumerals(result)
}

// ─── Number Formatting ──────────────────────────────────────────────

/**
 * Format a number with Western (English) digits.
 * ALWAYS uses en-US locale for numerals to ensure Western digits (0-9),
 * regardless of the current UI language.
 */
export function formatNumber(value: number | string | undefined | null, decimals = 0): string {
  if (value === undefined || value === null || value === '') return '—'
  const num = typeof value === 'string' ? parseFloat(value) : value
  if (isNaN(num)) return '—'

  return new Intl.NumberFormat('en-US', {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(num)
}

/**
 * Format a number as SAR currency with the new Saudi Riyal symbol.
 * Always displays Western numerals with the SAR symbol (ر.س).
 * Example: "3,464,636.00 ر.س" or "SAR 3,464,636.00"
 */
export function formatCurrency(value: number | string | undefined | null): string {
  if (value === undefined || value === null || value === '') return '—'
  const num = typeof value === 'string' ? parseFloat(value) : value
  if (isNaN(num)) return '—'

  const formatted = new Intl.NumberFormat('en-US', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(num)

  const isAr = getCurrentLocale() === 'ar'
  return isAr ? `${formatted} ${SAR_SYMBOL}` : `SAR ${formatted}`
}

/**
 * Format a number as percentage with Western numerals.
 * Example: "60%" not "٦٠٪"
 */
export function formatPercentage(value: number | string | undefined | null, decimals = 0): string {
  if (value === undefined || value === null || value === '') return '—'
  const num = typeof value === 'string' ? parseFloat(value) : value
  if (isNaN(num)) return '—'

  const formatted = new Intl.NumberFormat('en-US', {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(num)

  return `${formatted}%`
}

/**
 * Format file size in human-readable format.
 * Example: "2.5 MB", "340 KB"
 */
export function formatFileSize(bytes: number | undefined | null): string {
  if (bytes === undefined || bytes === null) return '—'
  if (bytes === 0) return '0 B'
  const units = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(1024))
  const size = bytes / Math.pow(1024, i)
  return `${formatNumber(size, i > 0 ? 1 : 0)} ${units[i]}`
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
