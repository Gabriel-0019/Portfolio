import { zodResolver } from '@hookform/resolvers/zod'
import { CircleAlert, CircleCheck, LoaderCircle, Send } from 'lucide-react'
import { useId, useMemo } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { buttonClasses } from '../../components/ui/styles.js'
import { ApiError } from '../../lib/api.js'
import { useSendContact } from '../../lib/queries.js'
import { createContactSchema, EMPTY_CONTACT, LIMITS } from './contactSchema.js'

const inputClasses =
  'block w-full rounded-lg border bg-white px-3 py-2 text-sm text-slate-900 shadow-sm placeholder:text-slate-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/30 dark:bg-slate-900 dark:text-slate-100'

function Field({ id, label, error, children }) {
  return (
    <div>
      <label
        htmlFor={id}
        className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300"
      >
        {label}
      </label>
      {children}
      {error && (
        <p id={`${id}-error`} className="mt-1.5 text-sm text-rose-600 dark:text-rose-400">
          {error}
        </p>
      )}
    </div>
  )
}

function fieldProps(id, error) {
  return {
    id,
    'aria-invalid': error ? 'true' : undefined,
    'aria-describedby': error ? `${id}-error` : undefined,
    className: `${inputClasses} ${error ? 'border-rose-500' : 'border-slate-300 dark:border-slate-700'}`,
  }
}

export default function ContactForm() {
  const { t } = useTranslation()
  const baseId = useId()
  const sendContact = useSendContact()

  // `t` changes identity when the language changes, so validation messages stay translated.
  const schema = useMemo(() => createContactSchema(t), [t])

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm({ resolver: zodResolver(schema), defaultValues: EMPTY_CONTACT })

  async function onSubmit(values) {
    try {
      await sendContact.mutateAsync({ ...values, subject: values.subject || null })
      reset(EMPTY_CONTACT)
    } catch (error) {
      if (error instanceof ApiError && error.status === 400 && error.problem?.errors) {
        for (const [field, messages] of Object.entries(error.problem.errors)) {
          const name = field.charAt(0).toLowerCase() + field.slice(1)
          setError(name in EMPTY_CONTACT ? name : 'root.server', { message: messages[0] })
        }
        return
      }
      const key = error instanceof ApiError && error.status === 429 ? 'rateLimited' : 'generic'
      setError('root.server', { message: t(`contact.errors.${key}`) })
    }
  }

  if (sendContact.isSuccess) {
    return (
      <div
        role="status"
        className="flex flex-col items-start gap-3 rounded-2xl bg-emerald-50 p-6 dark:bg-emerald-950/40"
      >
        <CircleCheck
          className="h-6 w-6 text-emerald-600 dark:text-emerald-400"
          aria-hidden="true"
        />
        <h2 className="text-lg font-semibold text-emerald-900 dark:text-emerald-200">
          {t('contact.successTitle')}
        </h2>
        <p className="text-emerald-800 dark:text-emerald-300">{t('contact.successBody')}</p>
        <button
          type="button"
          className={buttonClasses({ variant: 'secondary', className: 'mt-2' })}
          onClick={() => sendContact.reset()}
        >
          {t('contact.sendAnother')}
        </button>
      </div>
    )
  }

  const ids = {
    name: `${baseId}-name`,
    email: `${baseId}-email`,
    subject: `${baseId}-subject`,
    message: `${baseId}-message`,
    website: `${baseId}-website`,
  }

  return (
    <form noValidate onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-5">
      {errors.root?.server && (
        <div
          role="alert"
          className="flex items-start gap-2 rounded-lg bg-rose-50 p-3 text-sm text-rose-700 dark:bg-rose-950/40 dark:text-rose-300"
        >
          <CircleAlert className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
          {errors.root.server.message}
        </div>
      )}

      <div className="grid gap-5 sm:grid-cols-2">
        <Field id={ids.name} label={t('contact.fields.name')} error={errors.name?.message}>
          <input
            type="text"
            autoComplete="name"
            maxLength={LIMITS.nameMax}
            {...fieldProps(ids.name, errors.name)}
            {...register('name')}
          />
        </Field>
        <Field id={ids.email} label={t('contact.fields.email')} error={errors.email?.message}>
          <input
            type="email"
            autoComplete="email"
            maxLength={LIMITS.emailMax}
            {...fieldProps(ids.email, errors.email)}
            {...register('email')}
          />
        </Field>
      </div>

      <Field
        id={ids.subject}
        label={t('contact.fields.subjectOptional')}
        error={errors.subject?.message}
      >
        <input
          type="text"
          maxLength={LIMITS.subjectMax}
          {...fieldProps(ids.subject, errors.subject)}
          {...register('subject')}
        />
      </Field>

      <Field id={ids.message} label={t('contact.fields.message')} error={errors.message?.message}>
        <textarea
          rows={6}
          maxLength={LIMITS.messageMax}
          {...fieldProps(ids.message, errors.message)}
          {...register('message')}
        />
      </Field>

      {/* Honeypot: visually hidden and skipped by keyboard and screen readers. */}
      <div aria-hidden="true" className="absolute -left-[9999px] h-px w-px overflow-hidden">
        <label htmlFor={ids.website}>{t('contact.fields.website')}</label>
        <input
          id={ids.website}
          type="text"
          tabIndex={-1}
          autoComplete="off"
          {...register('website')}
        />
      </div>

      <div>
        <button type="submit" disabled={isSubmitting} className={buttonClasses({ size: 'lg' })}>
          {isSubmitting ? (
            <LoaderCircle
              className="h-4 w-4 animate-spin motion-reduce:animate-none"
              aria-hidden="true"
            />
          ) : (
            <Send className="h-4 w-4" aria-hidden="true" />
          )}
          {isSubmitting ? t('contact.sending') : t('contact.submit')}
        </button>
      </div>
    </form>
  )
}
