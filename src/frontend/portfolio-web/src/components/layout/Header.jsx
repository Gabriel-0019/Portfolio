import { Menu, X } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, NavLink, useLocation } from 'react-router'
import { useProfile } from '../../lib/queries.js'
import { useLocalizedPath } from '../../lib/useLocalizedPath.js'
import Container from '../ui/Container.jsx'
import LanguageSwitcher from './LanguageSwitcher.jsx'
import ThemeToggle from './ThemeToggle.jsx'

const NAV_ITEMS = [
  { path: '/', key: 'nav.home', end: true },
  { path: '/projects', key: 'nav.projects' },
  { path: '/about', key: 'nav.about' },
  { path: '/contact', key: 'nav.contact' },
]

function navLinkClasses({ isActive }) {
  return [
    'rounded-lg px-3 py-2 text-sm font-medium transition-colors',
    isActive
      ? 'text-indigo-600 dark:text-indigo-400'
      : 'text-slate-700 hover:bg-slate-100 hover:text-slate-900 dark:text-slate-300 dark:hover:bg-slate-800 dark:hover:text-white',
  ].join(' ')
}

export default function Header() {
  const { t } = useTranslation()
  const to = useLocalizedPath()
  const { pathname } = useLocation()
  const { data: profile } = useProfile()
  // The menu remembers the path it was opened on, so navigating anywhere closes it.
  const [openedOn, setOpenedOn] = useState(null)
  const menuOpen = openedOn === pathname
  const setMenuOpen = (update) =>
    setOpenedOn((current) => {
      const next = typeof update === 'function' ? update(current === pathname) : update
      return next ? pathname : null
    })

  const links = NAV_ITEMS.map((item) => (
    <NavLink key={item.path} to={to(item.path)} end={item.end} className={navLinkClasses}>
      {t(item.key)}
    </NavLink>
  ))

  return (
    <header className="sticky top-0 z-40 border-b border-slate-200 bg-white/85 backdrop-blur dark:border-slate-800 dark:bg-slate-950/85">
      <Container className="flex h-16 items-center justify-between gap-4">
        <Link
          to={to('/')}
          className="text-base font-semibold tracking-tight text-slate-900 dark:text-white"
        >
          {profile?.fullName ?? t('common.siteName')}
        </Link>

        <nav aria-label={t('nav.mainNavigation')} className="hidden items-center gap-1 md:flex">
          {links}
        </nav>

        <div className="hidden items-center gap-2 md:flex">
          <LanguageSwitcher />
          <ThemeToggle />
        </div>

        <button
          type="button"
          className="rounded-lg p-2 text-slate-700 hover:bg-slate-100 md:hidden dark:text-slate-300 dark:hover:bg-slate-800"
          aria-expanded={menuOpen}
          aria-controls="mobile-menu"
          aria-label={menuOpen ? t('nav.closeMenu') : t('nav.openMenu')}
          onClick={() => setMenuOpen((open) => !open)}
        >
          {menuOpen ? (
            <X className="h-5 w-5" aria-hidden="true" />
          ) : (
            <Menu className="h-5 w-5" aria-hidden="true" />
          )}
        </button>
      </Container>

      {menuOpen && (
        <div id="mobile-menu" className="border-t border-slate-200 md:hidden dark:border-slate-800">
          <Container className="flex flex-col gap-4 py-4">
            <nav aria-label={t('nav.mainNavigation')} className="flex flex-col gap-1">
              {links}
            </nav>
            <div className="flex items-center justify-between gap-2">
              <LanguageSwitcher />
              <ThemeToggle />
            </div>
          </Container>
        </div>
      )}
    </header>
  )
}
