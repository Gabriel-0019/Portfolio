export { CATEGORIES as PROJECT_CATEGORIES } from '../features/projects/categories.js'

export const PROJECT_STATUSES = [
  { value: 'Draft', label: 'Borrador' },
  { value: 'Published', label: 'Publicado' },
  { value: 'Archived', label: 'Archivado' },
]

export const CATEGORY_LABELS = {
  Web: 'Web',
  Mobile: 'Móvil',
  Backend: 'Backend',
  Desktop: 'Escritorio',
  Data: 'Datos',
  Other: 'Otro',
}

export const TECH_CATEGORIES = [
  { value: 'Language', label: 'Lenguaje' },
  { value: 'Frontend', label: 'Frontend' },
  { value: 'Backend', label: 'Backend' },
  { value: 'Database', label: 'Base de datos' },
  { value: 'DevOps', label: 'DevOps' },
  { value: 'Tool', label: 'Herramienta' },
]

export const EXPERIENCE_TYPES = [
  { value: 'Work', label: 'Trabajo' },
  { value: 'Education', label: 'Formación' },
  { value: 'Certification', label: 'Certificación' },
]

export const SOCIAL_PLATFORMS = [
  { value: 'github', label: 'GitHub' },
  { value: 'linkedin', label: 'LinkedIn' },
  { value: 'email', label: 'Email (mailto:)' },
  { value: 'website', label: 'Sitio web' },
]

export const LANGUAGES = [
  { code: 'en', label: 'Inglés' },
  { code: 'es', label: 'Español' },
]

/** "" -> null, other values unchanged. */
export const emptyToNull = (value) => (value === '' || value === undefined ? null : value)
