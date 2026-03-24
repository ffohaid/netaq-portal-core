import { createI18n } from 'vue-i18n'
import ar from './ar'
import en from './en'

const savedLocale = localStorage.getItem('netaq_locale') || 'ar'

const i18n = createI18n({
  legacy: false,
  locale: savedLocale,
  fallbackLocale: 'ar',
  messages: { ar, en },
})

export default i18n

export function setLocale(locale: 'ar' | 'en') {
  i18n.global.locale.value = locale
  localStorage.setItem('netaq_locale', locale)
  document.documentElement.dir = locale === 'ar' ? 'rtl' : 'ltr'
  document.documentElement.lang = locale
}

export function getCurrentLocale(): 'ar' | 'en' {
  return i18n.global.locale.value as 'ar' | 'en'
}
