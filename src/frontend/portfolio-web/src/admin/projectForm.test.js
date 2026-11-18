import { describe, expect, it } from 'vitest'
import { EMPTY_PROJECT_FORM, formToRequest, projectToForm, slugify } from './projectForm.js'

const apiProject = {
  id: 7,
  slug: 'demo',
  category: 'Backend',
  status: 'Published',
  isFeatured: true,
  displayOrder: 2,
  coverImageUrl: null,
  repoUrl: 'https://github.com/x/demo',
  demoUrl: null,
  startDate: '2026-01-01',
  endDate: null,
  technologyIds: [3, 5],
  translations: [{ language: 'es', title: 'Demo', summary: 'Resumen', content: null }],
  images: [{ url: '/uploads/a.png', altEn: 'A', altEs: 'A es' }],
  isPubliclyVisible: false,
}

describe('projectForm', () => {
  it('maps an API project to form values, filling missing languages', () => {
    const form = projectToForm(apiProject)

    expect(form.translations.es).toEqual({ title: 'Demo', summary: 'Resumen', content: '' })
    expect(form.translations.en).toEqual({ title: '', summary: '', content: '' })
    expect(form.technologyIds).toEqual(['3', '5'])
    expect(form.repoUrl).toBe('https://github.com/x/demo')
    expect(form.coverImageUrl).toBe('')
  })

  it('round-trips back to a request, omitting empty languages and blank optionals', () => {
    const request = formToRequest(projectToForm(apiProject))

    expect(request).toEqual({
      slug: 'demo',
      category: 'Backend',
      status: 'Published',
      isFeatured: true,
      displayOrder: 2,
      coverImageUrl: null,
      repoUrl: 'https://github.com/x/demo',
      demoUrl: null,
      startDate: '2026-01-01',
      endDate: null,
      technologyIds: [3, 5],
      translations: [{ language: 'es', title: 'Demo', summary: 'Resumen', content: '' }],
      images: [{ url: '/uploads/a.png', altEn: 'A', altEs: 'A es' }],
    })
  })

  it('drops gallery rows without an image and normalizes the slug', () => {
    const request = formToRequest({
      ...EMPTY_PROJECT_FORM,
      slug: '  My-Slug ',
      displayOrder: '',
      images: [{ url: '  ', altEn: '', altEs: '' }],
    })

    expect(request.slug).toBe('my-slug')
    expect(request.displayOrder).toBe(0)
    expect(request.images).toEqual([])
    expect(request.translations).toEqual([])
  })

  it.each([
    ['Mi Proyecto Ágil!', 'mi-proyecto-agil'],
    ['  .NET + React  ', 'net-react'],
    ['Ñandú', 'nandu'],
  ])('slugify(%s) = %s', (input, expected) => {
    expect(slugify(input)).toBe(expected)
  })
})
