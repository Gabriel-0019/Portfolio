import { useTranslation } from 'react-i18next'
import { TechBadgeList } from '../../components/ui/TechBadge.jsx'

const ORDER = ['Language', 'Frontend', 'Backend', 'Database', 'DevOps', 'Tool']

export default function TechStackGroups({ technologies }) {
  const { t } = useTranslation()

  const groups = ORDER.map((category) => ({
    category,
    items: technologies.filter((tech) => tech.category === category),
  })).filter((group) => group.items.length > 0)

  return (
    <dl className="grid gap-6 sm:grid-cols-2">
      {groups.map(({ category, items }) => (
        <div key={category}>
          <dt className="text-xs font-semibold tracking-wide text-slate-500 uppercase dark:text-slate-400">
            {t(`about.techCategories.${category}`, category)}
          </dt>
          <dd className="mt-2">
            <TechBadgeList technologies={items} size="md" />
          </dd>
        </div>
      ))}
    </dl>
  )
}
