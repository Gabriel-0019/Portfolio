import { emptyToNull, LANGUAGES } from './constants.js'

const emptyTranslation = { title: '', summary: '', content: '' }

export const EMPTY_PROJECT_FORM = {
  slug: '',
  category: 'Web',
  status: 'Draft',
  isFeatured: false,
  displayOrder: 0,
  coverImageUrl: '',
  repoUrl: '',
  demoUrl: '',
  startDate: '',
  endDate: '',
  technologyIds: [],
  translations: { en: { ...emptyTranslation }, es: { ...emptyTranslation } },
  images: [],
}

/** API project (AdminProjectDto) -> form values. */
export function projectToForm(project) {
  const translations = Object.fromEntries(
    LANGUAGES.map(({ code }) => {
      const t = project.translations.find((x) => x.language === code)
      return [
        code,
        t
          ? { title: t.title, summary: t.summary, content: t.content ?? '' }
          : { ...emptyTranslation },
      ]
    }),
  )

  return {
    slug: project.slug,
    category: project.category,
    status: project.status,
    isFeatured: project.isFeatured,
    displayOrder: project.displayOrder,
    coverImageUrl: project.coverImageUrl ?? '',
    repoUrl: project.repoUrl ?? '',
    demoUrl: project.demoUrl ?? '',
    startDate: project.startDate ?? '',
    endDate: project.endDate ?? '',
    technologyIds: project.technologyIds.map(String),
    translations,
    images: project.images.map((image) => ({ ...image })),
  }
}

/**
 * Form values -> SaveProjectRequest.
 * A language is sent only when any of its fields has content, so drafts can be saved half-translated.
 */
export function formToRequest(values) {
  const translations = LANGUAGES.map(({ code }) => ({
    language: code,
    ...values.translations[code],
  }))
    .map((t) => ({
      ...t,
      title: t.title.trim(),
      summary: t.summary.trim(),
      content: t.content.trim(),
    }))
    .filter((t) => t.title || t.summary || t.content)

  return {
    slug: values.slug.trim().toLowerCase(),
    category: values.category,
    status: values.status,
    isFeatured: Boolean(values.isFeatured),
    displayOrder: Number(values.displayOrder) || 0,
    coverImageUrl: emptyToNull(values.coverImageUrl.trim()),
    repoUrl: emptyToNull(values.repoUrl.trim()),
    demoUrl: emptyToNull(values.demoUrl.trim()),
    startDate: emptyToNull(values.startDate),
    endDate: emptyToNull(values.endDate),
    technologyIds: (values.technologyIds ?? []).map(Number),
    translations,
    images: values.images
      .filter((image) => image.url.trim())
      .map((image) => ({
        url: image.url.trim(),
        altEn: image.altEn.trim(),
        altEs: image.altEs.trim(),
      })),
  }
}

/** "Mi Proyecto Ágil!" -> "mi-proyecto-agil" */
export function slugify(text) {
  return text
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 100)
}
