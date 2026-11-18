import { Mail, MailOpen, Reply, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States.jsx'
import { buttonClasses, cardClasses } from '../../components/ui/styles.js'
import { adminApi } from '../api.js'
import { useAdminMessages, useAdminMutation } from '../queries.js'
import AdminPageHeader from '../ui/AdminPageHeader.jsx'
import { Badge, ErrorSummary } from '../ui/Fields.jsx'

const dateFormat = new Intl.DateTimeFormat('es', { dateStyle: 'medium', timeStyle: 'short' })

function replyLink(message) {
  const subject = message.subject ? `Re: ${message.subject}` : 'Re: tu mensaje'
  return `mailto:${message.email}?subject=${encodeURIComponent(subject)}`
}

export default function MessagesPage() {
  const { data: messages, isPending, isError, refetch } = useAdminMessages()
  const [unreadOnly, setUnreadOnly] = useState(false)
  const setRead = useAdminMutation(({ id, isRead }) => adminApi.setMessageRead(id, isRead))
  const remove = useAdminMutation((id) => adminApi.deleteMessage(id))

  const visible = (messages ?? []).filter((m) => !unreadOnly || !m.isRead)
  const unreadCount = (messages ?? []).filter((m) => !m.isRead).length

  function onOpen(message, open) {
    if (open && !message.isRead) setRead.mutate({ id: message.id, isRead: true })
  }

  return (
    <>
      <AdminPageHeader
        title="Mensajes"
        description={`Mensajes enviados desde el formulario de contacto. ${unreadCount} sin leer.`}
        actions={
          <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
            <input
              type="checkbox"
              checked={unreadOnly}
              onChange={(event) => setUnreadOnly(event.target.checked)}
              className="h-4 w-4 rounded border-slate-300"
            />
            Solo sin leer
          </label>
        }
      />
      {(setRead.isError || remove.isError) && (
        <div className="mb-4">
          <ErrorSummary error={setRead.error ?? remove.error} />
        </div>
      )}

      {isPending ? (
        <LoadingState />
      ) : isError ? (
        <ErrorState onRetry={refetch} />
      ) : visible.length === 0 ? (
        <EmptyState
          icon={Mail}
          title={unreadOnly ? 'No hay mensajes sin leer' : 'Todavía no hay mensajes'}
        />
      ) : (
        <ul className="flex flex-col gap-3">
          {visible.map((message) => (
            <li key={message.id}>
              <details
                className={`${cardClasses} group`}
                onToggle={(event) => onOpen(message, event.currentTarget.open)}
              >
                <summary className="flex cursor-pointer list-none flex-wrap items-center gap-3 p-4">
                  {message.isRead ? (
                    <MailOpen className="h-4 w-4 text-slate-400" aria-label="Leído" />
                  ) : (
                    <Mail className="h-4 w-4 text-indigo-600" aria-label="Sin leer" />
                  )}
                  <span
                    className={`min-w-0 flex-1 truncate ${message.isRead ? 'text-slate-600 dark:text-slate-300' : 'font-semibold text-slate-900 dark:text-white'}`}
                  >
                    {message.name}
                    <span className="font-normal text-slate-500">
                      {' '}
                      — {message.subject || message.message.slice(0, 80)}
                    </span>
                  </span>
                  <Badge>{message.language.toUpperCase()}</Badge>
                  <time dateTime={message.createdAt} className="text-xs text-slate-500">
                    {dateFormat.format(new Date(message.createdAt))}
                  </time>
                </summary>
                <div className="border-t border-slate-100 p-4 dark:border-slate-800">
                  <p className="text-sm text-slate-500">
                    <a
                      href={`mailto:${message.email}`}
                      className="text-indigo-600 hover:underline dark:text-indigo-400"
                    >
                      {message.email}
                    </a>
                  </p>
                  {message.subject && (
                    <p className="mt-2 font-medium text-slate-900 dark:text-white">
                      {message.subject}
                    </p>
                  )}
                  <p className="mt-2 whitespace-pre-wrap text-sm leading-relaxed text-slate-700 dark:text-slate-300">
                    {message.message}
                  </p>
                  <div className="mt-4 flex flex-wrap gap-2">
                    <a href={replyLink(message)} className={buttonClasses({ size: 'sm' })}>
                      <Reply className="h-4 w-4" aria-hidden="true" />
                      Responder
                    </a>
                    <button
                      type="button"
                      onClick={() => setRead.mutate({ id: message.id, isRead: !message.isRead })}
                      className={buttonClasses({ variant: 'secondary', size: 'sm' })}
                    >
                      {message.isRead ? 'Marcar como no leído' : 'Marcar como leído'}
                    </button>
                    <button
                      type="button"
                      onClick={() => {
                        if (window.confirm('¿Eliminar este mensaje? No se puede deshacer.'))
                          remove.mutate(message.id)
                      }}
                      className={buttonClasses({
                        variant: 'ghost',
                        size: 'sm',
                        className: 'text-rose-600 dark:text-rose-400',
                      })}
                    >
                      <Trash2 className="h-4 w-4" aria-hidden="true" />
                      Eliminar
                    </button>
                  </div>
                </div>
              </details>
            </li>
          ))}
        </ul>
      )}
    </>
  )
}
