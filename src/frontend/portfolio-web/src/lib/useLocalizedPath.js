import { useLanguage } from './queries.js'

/** Returns a function that prefixes app paths with the current language: "/projects" -> "/es/projects". */
export function useLocalizedPath() {
  const lang = useLanguage()
  return (path = '') => `/${lang}${path === '/' ? '' : path}`
}
