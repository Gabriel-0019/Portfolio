import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { adminApi } from './api.js'

export const adminKeys = {
  me: ['admin', 'me'],
  projects: ['admin', 'projects'],
  project: (id) => ['admin', 'projects', String(id)],
  technologies: ['admin', 'technologies'],
  experience: ['admin', 'experience'],
  profile: ['admin', 'profile'],
  messages: ['admin', 'messages'],
}

export function useMe() {
  return useQuery({
    queryKey: adminKeys.me,
    queryFn: ({ signal }) => adminApi.me({ signal }),
    retry: false,
    staleTime: 5 * 60 * 1000,
  })
}

export const useAdminProjects = () =>
  useQuery({ queryKey: adminKeys.projects, queryFn: ({ signal }) => adminApi.projects({ signal }) })

export const useAdminProject = (id) =>
  useQuery({
    queryKey: adminKeys.project(id),
    queryFn: ({ signal }) => adminApi.project(id, { signal }),
    enabled: Boolean(id),
  })

export const useAdminTechnologies = () =>
  useQuery({
    queryKey: adminKeys.technologies,
    queryFn: ({ signal }) => adminApi.technologies({ signal }),
  })

export const useAdminExperience = () =>
  useQuery({
    queryKey: adminKeys.experience,
    queryFn: ({ signal }) => adminApi.experience({ signal }),
  })

export const useAdminProfile = () =>
  useQuery({ queryKey: adminKeys.profile, queryFn: ({ signal }) => adminApi.profile({ signal }) })

export const useAdminMessages = () =>
  useQuery({ queryKey: adminKeys.messages, queryFn: ({ signal }) => adminApi.messages({ signal }) })

/**
 * Mutation that refreshes every cached query afterwards (admin lists and the public site),
 * so edits show up everywhere without a reload.
 */
export function useAdminMutation(mutationFn, options = {}) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn,
    ...options,
    onSuccess: async (...args) => {
      await queryClient.invalidateQueries()
      await options.onSuccess?.(...args)
    },
  })
}
