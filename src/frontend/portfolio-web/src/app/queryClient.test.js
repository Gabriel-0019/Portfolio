import { describe, expect, it } from 'vitest'
import { createQueryClient, readInitialData } from './queryClient.js'

describe('initial data from the server', () => {
  it('seeds the cache under the keys the hooks use', () => {
    const profile = { fullName: 'Ada' }
    const featured = [{ slug: 'a' }]
    const project = { slug: 'a', title: 'A' }

    const client = createQueryClient({
      language: 'es',
      data: { profile, featuredProjects: featured, project, projects: featured },
    })

    expect(client.getQueryData(['profile', 'es'])).toBe(profile)
    expect(client.getQueryData(['projects', 'es', { featured: true }])).toBe(featured)
    expect(client.getQueryData(['projects', 'es', {}])).toBe(featured)
    expect(client.getQueryData(['project', 'a', 'es'])).toBe(project)
    expect(client.getQueryData(['profile', 'en'])).toBeUndefined()
  })

  it('reads the JSON island and tolerates its absence or bad content', () => {
    const doc = document.implementation.createHTMLDocument()
    expect(readInitialData(doc)).toBeNull()

    doc.head.innerHTML =
      '<script id="initial-data" type="application/json">{"language":"en","data":{}}</script>'
    expect(readInitialData(doc)).toEqual({ language: 'en', data: {} })

    doc.getElementById('initial-data').textContent = '{not json'
    expect(readInitialData(doc)).toBeNull()
  })
})
