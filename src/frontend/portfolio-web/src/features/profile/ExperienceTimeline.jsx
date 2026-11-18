import { Award, Briefcase, GraduationCap } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import Markdown from '../../components/ui/Markdown.jsx'
import { formatPeriod } from '../../lib/format.js'
import { useLanguage } from '../../lib/queries.js'

const ICONS = { Work: Briefcase, Education: GraduationCap, Certification: Award }

export default function ExperienceTimeline({ items }) {
  const { t } = useTranslation()
  const lang = useLanguage()

  return (
    <ol className="relative border-l border-slate-200 dark:border-slate-800">
      {items.map((item, index) => {
        const Icon = ICONS[item.type] ?? Briefcase
        return (
          <li key={`${item.company}-${item.startDate}-${index}`} className="mb-10 ml-8 last:mb-0">
            <span className="absolute -left-4 flex h-8 w-8 items-center justify-center rounded-full bg-indigo-50 ring-4 ring-white dark:bg-indigo-950 dark:ring-slate-950">
              <Icon className="h-4 w-4 text-indigo-600 dark:text-indigo-400" aria-hidden="true" />
            </span>
            <div className="flex flex-col gap-1 sm:flex-row sm:items-baseline sm:justify-between sm:gap-4">
              <h3 className="text-base font-semibold text-slate-900 dark:text-white">
                {item.role}
                <span className="font-normal text-slate-500 dark:text-slate-400">
                  {' · '}
                  {item.companyUrl ? (
                    <a
                      href={item.companyUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="hover:text-indigo-600 dark:hover:text-indigo-400"
                    >
                      {item.company}
                    </a>
                  ) : (
                    item.company
                  )}
                </span>
              </h3>
              <p className="shrink-0 text-sm text-slate-500 dark:text-slate-400">
                {formatPeriod(item.startDate, item.endDate, lang, t('common.present'))}
              </p>
            </div>
            <p className="mt-1 text-xs font-medium tracking-wide text-slate-500 uppercase dark:text-slate-400">
              {t(`about.types.${item.type}`, item.type)}
              {item.location && ` · ${item.location}`}
            </p>
            {item.description && <Markdown className="prose-sm mt-3">{item.description}</Markdown>}
          </li>
        )
      })}
    </ol>
  )
}
