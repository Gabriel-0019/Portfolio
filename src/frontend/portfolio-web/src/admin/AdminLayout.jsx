import { useQueryClient } from '@tanstack/react-query'
import { Briefcase, Cpu, ExternalLink, FolderKanban, Inbox, LogOut, UserRound } from 'lucide-react'
import { Navigate, NavLink, Outlet, useLocation, useNavigate } from 'react-router'
import InitialLoading from '../components/layout/InitialLoading.jsx'
import ThemeToggle from '../components/layout/ThemeToggle.jsx'
import { ErrorState } from '../components/ui/States.jsx'
import { ApiError } from '../lib/api.js'
import { adminApi } from './api.js'
import { useAdminMessages, useMe } from './queries.js'

const NAV = [
  { to: '/admin/projects', label: 'Proyectos', icon: FolderKanban },
  { to: '/admin/technologies', label: 'Tecnologías', icon: Cpu },
  { to: '/admin/experience', label: 'Experiencia', icon: Briefcase },
  { to: '/admin/profile', label: 'Perfil', icon: UserRound },
  { to: '/admin/messages', label: 'Mensajes', icon: Inbox, badge: 'unread' },
]

function navClasses({ isActive }) {
  return [
    'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
    isActive
      ? 'bg-slate-900 text-white dark:bg-white dark:text-slate-900'
      : 'text-slate-700 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800',
  ].join(' ')
}

function Shell() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { data: me } = useMe()
  const { data: messages } = useAdminMessages()
  const unread = messages?.filter((m) => !m.isRead).length ?? 0

  async function logout() {
    await adminApi.logout().catch(() => null)
    queryClient.removeQueries({ queryKey: ['admin'] })
    navigate('/admin/login', { replace: true })
  }

  return (
    <div className="min-h-svh bg-slate-50 dark:bg-slate-950">
      <header className="sticky top-0 z-30 border-b border-slate-200 bg-white/90 backdrop-blur dark:border-slate-800 dark:bg-slate-950/90">
        <div className="flex h-14 items-center justify-between gap-4 px-4 sm:px-6">
          <p className="text-sm font-semibold text-slate-900 dark:text-white">
            Panel de administración
          </p>
          <div className="flex items-center gap-2">
            <a
              href="/"
              target="_blank"
              rel="noopener noreferrer"
              className="hidden items-center gap-1 rounded-lg px-2 py-1.5 text-sm text-slate-600 hover:bg-slate-100 sm:inline-flex dark:text-slate-400 dark:hover:bg-slate-800"
            >
              <ExternalLink className="h-4 w-4" aria-hidden="true" />
              Ver sitio
            </a>
            <ThemeToggle />
            <button
              type="button"
              onClick={logout}
              title={me?.email ? `Cerrar sesión de ${me.email}` : 'Cerrar sesión'}
              className="inline-flex items-center gap-1 rounded-lg px-2 py-1.5 text-sm text-slate-600 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800"
            >
              <LogOut className="h-4 w-4" aria-hidden="true" />
              Salir
            </button>
          </div>
        </div>
      </header>

      <div className="mx-auto flex max-w-7xl flex-col gap-6 px-4 py-6 sm:px-6 lg:flex-row">
        <nav aria-label="Administración" className="lg:w-56 lg:shrink-0">
          <ul className="flex gap-1 overflow-x-auto lg:flex-col">
            {NAV.map(({ to, label, icon: Icon, badge }) => (
              <li key={to}>
                <NavLink to={to} className={navClasses}>
                  <Icon className="h-4 w-4 shrink-0" aria-hidden="true" />
                  <span className="whitespace-nowrap">{label}</span>
                  {badge === 'unread' && unread > 0 && (
                    <span
                      className="ml-auto rounded-full bg-indigo-600 px-1.5 text-xs text-white"
                      aria-label={`${unread} sin leer`}
                    >
                      {unread}
                    </span>
                  )}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>
        <main className="min-w-0 flex-1">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

/** Requires a signed-in admin; otherwise sends the user to the login page and back afterwards. */
export default function AdminLayout() {
  const { pathname, search } = useLocation()
  const { isPending, isError, error, refetch } = useMe()

  if (isPending) return <InitialLoading />

  if (isError) {
    if (error instanceof ApiError && error.status === 401) {
      return (
        <Navigate to={`/admin/login?returnTo=${encodeURIComponent(pathname + search)}`} replace />
      )
    }
    return <ErrorState onRetry={refetch} />
  }

  return <Shell />
}
