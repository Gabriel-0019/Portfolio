/** Parses an ISO date-only string ("2026-11-17") as a local date, avoiding UTC day shifts. */
export function parseDateOnly(value) {
  if (!value) return null
  const [year, month, day] = value.split('-').map(Number)
  if (!year || !month) return null
  return new Date(year, month - 1, day || 1)
}

/** "Nov 2026" / "nov 2026" */
export function formatMonthYear(value, lang) {
  const date = parseDateOnly(value)
  if (!date) return ''
  return new Intl.DateTimeFormat(lang, { month: 'short', year: 'numeric' }).format(date)
}

/** "Jan 2024 – Present" */
export function formatPeriod(start, end, lang, presentLabel) {
  const from = formatMonthYear(start, lang)
  const to = end ? formatMonthYear(end, lang) : presentLabel
  if (!from) return to ?? ''
  return `${from} – ${to}`
}

export function initials(fullName) {
  return (fullName ?? '')
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0].toUpperCase())
    .join('')
}
