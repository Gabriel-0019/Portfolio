import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { vi } from 'vitest'
import { routes } from '../app/routes.jsx'
import ThemeProvider from '../app/ThemeProvider.jsx'

export const profileFixture = {
  fullName: 'Test User',
  headline: 'Software Developer',
  bio: 'I build things.',
  email: 'test@example.com',
  location: 'Somewhere',
  avatarUrl: null,
  cvUrl: null,
  socialLinks: [{ platform: 'github', url: 'https://github.com/test' }],
}

export const projectFixture = {
  slug: 'portfolio',
  title: 'Developer Portfolio',
  summary: 'A bilingual portfolio.',
  category: 'Web',
  isFeatured: true,
  coverImageUrl: null,
  repoUrl: 'https://github.com/test/portfolio',
  demoUrl: null,
  startDate: '2026-11-17',
  endDate: null,
  technologies: [{ name: 'React', iconKey: 'react', category: 'Frontend' }],
}

export function jsonResponse(body, status = 200, contentType = 'application/json') {
  return new Response(body === null ? null : JSON.stringify(body), {
    status,
    headers: { 'Content-Type': contentType },
  })
}

/**
 * Mocks fetch with a route table: { 'GET /api/profile': (url, init) => Response }.
 * Unmatched requests fail the test loudly. Returns the spy to inspect calls.
 */
export function mockApi(handlers = {}) {
  const defaults = {
    'GET /api/profile': () => jsonResponse(profileFixture),
    'GET /api/technologies': () =>
      jsonResponse([{ name: 'React', iconKey: 'react', category: 'Frontend' }]),
    'GET /api/projects': () => jsonResponse([projectFixture]),
    'GET /api/experience': () => jsonResponse([]),
  }
  const table = { ...defaults, ...handlers }

  return vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init = {}) => {
    const url = new URL(String(input), 'http://localhost')
    const key = `${init.method ?? 'GET'} ${url.pathname}`
    const handler = table[key]
    if (!handler) throw new Error(`Unexpected request: ${key}`)
    return handler(url, init)
  })
}

/** Renders the real route tree at `path` with fresh query cache and theme state. */
export function renderApp(path = '/en') {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  const router = createMemoryRouter(routes, { initialEntries: [path] })

  const utils = render(
    <ThemeProvider>
      <QueryClientProvider client={queryClient}>
        <RouterProvider router={router} />
      </QueryClientProvider>
    </ThemeProvider>,
  )

  return { ...utils, router }
}
