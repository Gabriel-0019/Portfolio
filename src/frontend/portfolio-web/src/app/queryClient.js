import { QueryClient } from '@tanstack/react-query'
import { ApiError } from '../lib/api.js'

/**
 * Reads the data the server embedded in the HTML (see Hosting/PageMetadata.cs). Returns null in
 * development (Vite serves index.html without it) or when the block is missing or malformed.
 */
export function readInitialData(doc = document) {
  const element = doc.getElementById('initial-data')
  if (!element?.textContent) return null
  try {
    return JSON.parse(element.textContent)
  } catch {
    return null
  }
}

/** Seeds the cache with server data under the same keys the hooks in lib/queries.js use. */
export function seedQueryClient(queryClient, initial) {
  if (!initial?.data) return
  const { language: lang, data } = initial

  if (data.profile) queryClient.setQueryData(['profile', lang], data.profile)
  if (data.featuredProjects) {
    queryClient.setQueryData(['projects', lang, { featured: true }], data.featuredProjects)
  }
  if (data.projects) queryClient.setQueryData(['projects', lang, {}], data.projects)
  if (data.project) queryClient.setQueryData(['project', data.project.slug, lang], data.project)
}

export function createQueryClient(initial = null) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 60 * 1000,
        refetchOnWindowFocus: false,
        // Client errors (e.g. 404) will not change on retry.
        retry: (failureCount, error) =>
          !(error instanceof ApiError && error.status >= 400 && error.status < 500) &&
          failureCount < 2,
      },
    },
  })
  seedQueryClient(queryClient, initial)
  return queryClient
}
