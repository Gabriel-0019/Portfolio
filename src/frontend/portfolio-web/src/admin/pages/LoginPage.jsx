import { useQueryClient } from '@tanstack/react-query'
import { LoaderCircle, LockKeyhole } from 'lucide-react'
import { useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router'
import { buttonClasses } from '../../components/ui/styles.js'
import { ApiError } from '../../lib/api.js'
import { adminApi } from '../api.js'
import { adminKeys } from '../queries.js'
import { Field, inputClasses } from '../ui/Fields.jsx'

/** Only allow returning to admin pages (prevents open redirects through ?returnTo=). */
function safeReturnTo(value) {
  return value && value.startsWith('/admin/') && !value.startsWith('//') ? value : '/admin/projects'
}

function loginErrorMessage(error) {
  if (error instanceof ApiError) {
    if (error.status === 401) return 'Correo o contraseña incorrectos.'
    if (error.status === 423)
      return 'Demasiados intentos fallidos. La cuenta está bloqueada unos minutos.'
    if (error.status === 429) return 'Demasiados intentos. Espera unos minutos.'
  }
  return 'No se pudo iniciar sesión. Inténtalo de nuevo.'
}

export default function LoginPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [searchParams] = useSearchParams()
  const [status, setStatus] = useState({ pending: false, error: null })

  async function onSubmit(event) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    setStatus({ pending: true, error: null })

    try {
      const me = await adminApi.login({
        email: form.get('email'),
        password: form.get('password'),
        rememberMe: form.get('rememberMe') === 'on',
      })
      queryClient.setQueryData(adminKeys.me, me)
      navigate(safeReturnTo(searchParams.get('returnTo')), { replace: true })
    } catch (error) {
      setStatus({ pending: false, error: loginErrorMessage(error) })
    }
  }

  return (
    <main className="flex min-h-svh items-center justify-center bg-slate-50 px-4 dark:bg-slate-950">
      <title>Iniciar sesión · Administración</title>
      <meta name="robots" content="noindex" />
      <div className="w-full max-w-sm rounded-2xl border border-slate-200 bg-white p-6 shadow-sm sm:p-8 dark:border-slate-800 dark:bg-slate-900">
        <div className="mb-6 flex items-center gap-3">
          <span className="flex h-10 w-10 items-center justify-center rounded-full bg-indigo-50 dark:bg-indigo-950">
            <LockKeyhole
              className="h-5 w-5 text-indigo-600 dark:text-indigo-400"
              aria-hidden="true"
            />
          </span>
          <h1 className="text-lg font-semibold text-slate-900 dark:text-white">
            Panel de administración
          </h1>
        </div>

        <form onSubmit={onSubmit} className="flex flex-col gap-4">
          {status.error && (
            <p
              role="alert"
              className="rounded-lg bg-rose-50 p-3 text-sm text-rose-700 dark:bg-rose-950/40 dark:text-rose-300"
            >
              {status.error}
            </p>
          )}
          <Field id="login-email" label="Correo electrónico">
            <input
              id="login-email"
              name="email"
              type="email"
              autoComplete="username"
              required
              className={inputClasses}
            />
          </Field>
          <Field id="login-password" label="Contraseña">
            <input
              id="login-password"
              name="password"
              type="password"
              autoComplete="current-password"
              required
              className={inputClasses}
            />
          </Field>
          <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
            <input type="checkbox" name="rememberMe" className="h-4 w-4 rounded border-slate-300" />
            Mantener la sesión iniciada
          </label>
          <button type="submit" disabled={status.pending} className={buttonClasses({ size: 'lg' })}>
            {status.pending && <LoaderCircle className="h-4 w-4 animate-spin" aria-hidden="true" />}
            {status.pending ? 'Entrando…' : 'Entrar'}
          </button>
        </form>
      </div>
    </main>
  )
}
