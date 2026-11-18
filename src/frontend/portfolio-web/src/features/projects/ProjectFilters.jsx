import { useTranslation } from 'react-i18next'
import { useTechnologies } from '../../lib/queries.js'

import { CATEGORIES } from './categories.js'

function pillClasses(active) {
  return [
    'rounded-full px-3 py-1.5 text-sm font-medium transition-colors',
    active
      ? 'bg-slate-900 text-white dark:bg-white dark:text-slate-900'
      : 'bg-slate-100 text-slate-700 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700',
  ].join(' ')
}

/** Category pills and technology select. Values are controlled by the parent (URL search params). */
export default function ProjectFilters({ category, tech, onChange }) {
  const { t } = useTranslation()
  const { data: technologies = [] } = useTechnologies()

  return (
    <div
      role="search"
      aria-label={t('projects.filters.label')}
      className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between"
    >
      <fieldset>
        <legend className="sr-only">{t('projects.filters.category')}</legend>
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            aria-pressed={!category}
            className={pillClasses(!category)}
            onClick={() => onChange({ category: null })}
          >
            {t('projects.filters.all')}
          </button>
          {CATEGORIES.map((value) => (
            <button
              key={value}
              type="button"
              aria-pressed={category === value}
              className={pillClasses(category === value)}
              onClick={() => onChange({ category: category === value ? null : value })}
            >
              {t(`projects.categories.${value}`)}
            </button>
          ))}
        </div>
      </fieldset>

      <label className="flex items-center gap-2 text-sm">
        <span className="font-medium text-slate-700 dark:text-slate-300">
          {t('projects.filters.technology')}
        </span>
        <select
          value={tech ?? ''}
          onChange={(event) => onChange({ tech: event.target.value || null })}
          className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        >
          <option value="">{t('projects.filters.allTechnologies')}</option>
          {technologies.map((technology) => (
            <option key={technology.name} value={technology.name}>
              {technology.name}
            </option>
          ))}
        </select>
      </label>
    </div>
  )
}
