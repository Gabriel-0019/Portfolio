import { useMutation, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { api } from './api.js'

/** Current content language ("en" | "es"), driven by the URL prefix. */
export function useLanguage() {
  return useTranslation().i18n.resolvedLanguage ?? 'en'
}

export function useProfile() {
  const lang = useLanguage()
  return useQuery({
    queryKey: ['profile', lang],
    queryFn: ({ signal }) => api.profile(lang, { signal }),
  })
}

export function useExperience() {
  const lang = useLanguage()
  return useQuery({
    queryKey: ['experience', lang],
    queryFn: ({ signal }) => api.experience(lang, { signal }),
  })
}

export function useTechnologies() {
  return useQuery({
    queryKey: ['technologies'],
    queryFn: ({ signal }) => api.technologies({ signal }),
    staleTime: 10 * 60 * 1000,
  })
}

export function useProjects(filters = {}) {
  const lang = useLanguage()
  return useQuery({
    queryKey: ['projects', lang, filters],
    queryFn: ({ signal }) => api.projects(lang, filters, { signal }),
  })
}

export function useProject(slug) {
  const lang = useLanguage()
  return useQuery({
    queryKey: ['project', slug, lang],
    queryFn: ({ signal }) => api.project(slug, lang, { signal }),
    enabled: Boolean(slug),
  })
}

export function useSendContact() {
  const lang = useLanguage()
  return useMutation({ mutationFn: (message) => api.sendContact(lang, message) })
}
