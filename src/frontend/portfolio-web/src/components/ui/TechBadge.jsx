export default function TechBadge({ name, size = 'sm' }) {
  const sizing = size === 'sm' ? 'px-2 py-0.5 text-xs' : 'px-3 py-1 text-sm'
  return (
    <span
      className={`inline-flex items-center rounded-md bg-slate-100 font-medium text-slate-700 ring-1 ring-slate-200 ring-inset dark:bg-slate-800 dark:text-slate-300 dark:ring-slate-700 ${sizing}`}
    >
      {name}
    </span>
  )
}

export function TechBadgeList({ technologies, size, className = '' }) {
  if (!technologies?.length) return null
  return (
    <ul className={`flex flex-wrap gap-2 ${className}`}>
      {technologies.map((tech) => (
        <li key={tech.name}>
          <TechBadge name={tech.name} size={size} />
        </li>
      ))}
    </ul>
  )
}
