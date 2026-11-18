import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import en from './locales/en.json'
import es from './locales/es.json'

export const SUPPORTED_LANGUAGES = ['en', 'es']
export const DEFAULT_LANGUAGE = 'en'
const STORAGE_KEY = 'portfolio.lang'

export function isSupportedLanguage(value) {
  return SUPPORTED_LANGUAGES.includes(value)
}

/** The user's last explicit choice, then the browser languages, then English. */
export function detectLanguage() {
  try {
    const stored = window.localStorage.getItem(STORAGE_KEY)
    if (isSupportedLanguage(stored)) return stored
  } catch {
    // Storage can be unavailable (private mode, blocked cookies).
  }

  const browserLanguages = navigator.languages?.length ? navigator.languages : [navigator.language]
  for (const tag of browserLanguages) {
    const code = tag?.slice(0, 2).toLowerCase()
    if (isSupportedLanguage(code)) return code
  }

  return DEFAULT_LANGUAGE
}

export function rememberLanguage(language) {
  try {
    window.localStorage.setItem(STORAGE_KEY, language)
  } catch {
    // Ignore: remembering the language is a convenience only.
  }
}

/** Replaces the language prefix of a path: ("/en/projects", "es") -> "/es/projects". */
export function switchLanguageInPath(pathname, language) {
  const segments = pathname.split('/')
  if (isSupportedLanguage(segments[1])) {
    segments[1] = language
    return segments.join('/') || `/${language}`
  }
  return `/${language}${pathname === '/' ? '' : pathname}`
}

i18n.use(initReactI18next).init({
  resources: { en: { translation: en }, es: { translation: es } },
  lng: DEFAULT_LANGUAGE,
  fallbackLng: DEFAULT_LANGUAGE,
  supportedLngs: SUPPORTED_LANGUAGES,
  interpolation: { escapeValue: false },
  returnNull: false,
})

export default i18n
