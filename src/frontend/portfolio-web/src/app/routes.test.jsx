import { screen, within } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { jsonResponse, mockApi, renderApp } from '../test/utils.jsx'

describe('routing and pages', () => {
  it('redirects "/" to the browser language and renders the home page', async () => {
    vi.spyOn(navigator, 'languages', 'get').mockReturnValue(['es-MX'])
    mockApi()

    const { router } = renderApp('/')

    expect(
      await screen.findByRole('heading', { level: 1, name: 'Hola, soy Test User.' }),
    ).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/es')
    expect(document.documentElement.lang).toBe('es')
  })

  it('adds the language prefix to paths that lack it', async () => {
    vi.spyOn(navigator, 'languages', 'get').mockReturnValue(['en-US'])
    mockApi()

    const { router } = renderApp('/projects?tech=React')

    expect(await screen.findByRole('heading', { level: 1, name: 'Projects' })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/en/projects')
    expect(router.state.location.search).toBe('?tech=React')
  })

  it('requests content in the URL language', async () => {
    const fetchSpy = mockApi()

    renderApp('/es/projects')

    expect(await screen.findByRole('link', { name: 'Developer Portfolio' })).toHaveAttribute(
      'href',
      '/es/projects/portfolio',
    )
    const projectCall = fetchSpy.mock.calls.find(([url]) => String(url).startsWith('/api/projects'))
    expect(String(projectCall[0])).toContain('lang=es')
  })

  it('links the language switcher to the same page in the other language', async () => {
    mockApi()

    renderApp('/en/projects')

    const group = await screen.findAllByRole('group', { name: 'Language' })
    expect(within(group[0]).getByRole('link', { name: /^ES\s*Español$/ })).toHaveAttribute(
      'href',
      '/es/projects',
    )
  })

  it('shows the "coming soon" state when there are no projects', async () => {
    mockApi({ 'GET /api/projects': () => jsonResponse([]) })

    renderApp('/en/projects')

    expect(await screen.findByText('Projects coming soon')).toBeInTheDocument()
  })

  it('shows "project not found" for unknown slugs', async () => {
    mockApi({
      'GET /api/projects/missing': () =>
        jsonResponse({ title: 'Not Found', status: 404 }, 404, 'application/problem+json'),
    })

    renderApp('/en/projects/missing')

    expect(await screen.findByRole('heading', { name: 'Project not found' })).toBeInTheDocument()
  })

  it('renders the case study markdown', async () => {
    mockApi({
      'GET /api/projects/portfolio': () =>
        jsonResponse({ ...{ images: [], content: '## Problem\nSomething' }, ...projectDetail() }),
    })

    renderApp('/en/projects/portfolio')

    expect(await screen.findByRole('heading', { level: 2, name: 'Problem' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Source code/ })).toHaveAttribute(
      'href',
      'https://github.com/test/portfolio',
    )
  })

  it('renders a 404 page for unknown routes', async () => {
    mockApi()

    renderApp('/en/does-not-exist')

    expect(await screen.findByRole('heading', { name: 'Page not found' })).toBeInTheDocument()
  })
})

function projectDetail() {
  return {
    slug: 'portfolio',
    title: 'Developer Portfolio',
    summary: 'A bilingual portfolio.',
    content: '## Problem\nSomething',
    category: 'Web',
    isFeatured: true,
    coverImageUrl: null,
    repoUrl: 'https://github.com/test/portfolio',
    demoUrl: null,
    startDate: '2026-11-17',
    endDate: null,
    technologies: [{ name: 'React', iconKey: 'react', category: 'Frontend' }],
    images: [],
  }
}
