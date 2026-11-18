import { LoaderCircle, Plus, Save, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { Controller, useFieldArray, useForm, useWatch } from 'react-hook-form'
import { ErrorState, LoadingState } from '../../components/ui/States.jsx'
import { buttonClasses } from '../../components/ui/styles.js'
import { ApiError } from '../../lib/api.js'
import { adminApi } from '../api.js'
import { emptyToNull, LANGUAGES, SOCIAL_PLATFORMS } from '../constants.js'
import { useAdminMutation, useAdminProfile } from '../queries.js'
import AdminPageHeader from '../ui/AdminPageHeader.jsx'
import { ErrorSummary, Field, inputClasses, Section, SuccessNotice } from '../ui/Fields.jsx'
import MarkdownField from '../ui/MarkdownField.jsx'
import UploadField from '../ui/UploadField.jsx'

function toForm(profile) {
  return {
    fullName: profile?.fullName ?? '',
    email: profile?.email ?? '',
    location: profile?.location ?? '',
    avatarUrl: profile?.avatarUrl ?? '',
    cvUrlEn: profile?.cvUrlEn ?? '',
    cvUrlEs: profile?.cvUrlEs ?? '',
    translations: Object.fromEntries(
      LANGUAGES.map(({ code }) => {
        const t = profile?.translations.find((x) => x.language === code)
        return [code, { headline: t?.headline ?? '', bio: t?.bio ?? '' }]
      }),
    ),
    socialLinks: profile?.socialLinks.map((link) => ({ ...link })) ?? [],
  }
}

function toRequest(values) {
  return {
    fullName: values.fullName.trim(),
    email: emptyToNull(values.email.trim()),
    location: emptyToNull(values.location.trim()),
    avatarUrl: emptyToNull(values.avatarUrl.trim()),
    cvUrlEn: emptyToNull(values.cvUrlEn.trim()),
    cvUrlEs: emptyToNull(values.cvUrlEs.trim()),
    translations: LANGUAGES.map(({ code }) => ({
      language: code,
      headline: values.translations[code].headline.trim(),
      bio: values.translations[code].bio.trim(),
    })),
    socialLinks: values.socialLinks
      .filter((link) => link.url.trim())
      .map((link) => ({ platform: link.platform, url: link.url.trim() })),
  }
}

function ProfileForm({ profile }) {
  const [saved, setSaved] = useState(false)
  const { register, control, handleSubmit, reset } = useForm({
    defaultValues: toForm(profile),
  })
  const links = useFieldArray({ control, name: 'socialLinks' })
  const translations = useWatch({ control, name: 'translations' })
  const save = useAdminMutation((body) => adminApi.saveProfile(body), {
    onSuccess: (updated) => {
      reset(toForm(updated))
      setSaved(true)
    },
  })

  function onSubmit(values) {
    setSaved(false)
    save.mutate(toRequest(values))
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-6">
      <AdminPageHeader
        title="Perfil"
        description="Tu nombre, presentación, foto, CV y redes."
        actions={
          <button type="submit" disabled={save.isPending} className={buttonClasses()}>
            {save.isPending ? (
              <LoaderCircle className="h-4 w-4 animate-spin" aria-hidden="true" />
            ) : (
              <Save className="h-4 w-4" aria-hidden="true" />
            )}
            Guardar
          </button>
        }
      />
      {save.isError && <ErrorSummary error={save.error} />}
      {saved && <SuccessNotice>Perfil actualizado.</SuccessNotice>}

      <Section title="Datos personales">
        <div className="grid gap-4 sm:grid-cols-3">
          <Field id="p-name" label="Nombre completo">
            <input
              id="p-name"
              required
              maxLength={120}
              className={inputClasses}
              {...register('fullName')}
            />
          </Field>
          <Field id="p-email" label="Correo público" hint="Opcional.">
            <input id="p-email" type="email" className={inputClasses} {...register('email')} />
          </Field>
          <Field id="p-location" label="Ubicación" hint="Opcional, p. ej. Ciudad, País.">
            <input
              id="p-location"
              maxLength={120}
              className={inputClasses}
              {...register('location')}
            />
          </Field>
          <div className="sm:col-span-3">
            <Controller
              control={control}
              name="avatarUrl"
              render={({ field }) => (
                <UploadField
                  label="Foto"
                  value={field.value}
                  onChange={field.onChange}
                  hint="Cuadrada, al menos 400×400."
                />
              )}
            />
          </div>
        </div>
      </Section>

      <Section title="Presentación">
        <div className="grid gap-6 xl:grid-cols-2">
          {LANGUAGES.map(({ code, label }) => (
            <fieldset key={code} className="flex flex-col gap-4">
              <legend className="mb-1 text-sm font-semibold text-indigo-600 dark:text-indigo-400">
                {label}
              </legend>
              <Field
                id={`p-headline-${code}`}
                label="Titular"
                hint="P. ej. Desarrollador de software · .NET y React"
              >
                <input
                  id={`p-headline-${code}`}
                  required
                  maxLength={150}
                  className={inputClasses}
                  {...register(`translations.${code}.headline`)}
                />
              </Field>
              <MarkdownField
                label="Biografía"
                rows={8}
                value={translations[code]?.bio}
                {...register(`translations.${code}.bio`)}
              />
            </fieldset>
          ))}
        </div>
      </Section>

      <Section title="Currículum (PDF)">
        <div className="grid gap-4 lg:grid-cols-2">
          <Controller
            control={control}
            name="cvUrlEn"
            render={({ field }) => (
              <UploadField
                kind="pdf"
                label="CV en inglés"
                value={field.value}
                onChange={field.onChange}
              />
            )}
          />
          <Controller
            control={control}
            name="cvUrlEs"
            render={({ field }) => (
              <UploadField
                kind="pdf"
                label="CV en español"
                value={field.value}
                onChange={field.onChange}
              />
            )}
          />
        </div>
      </Section>

      <Section
        title="Redes y enlaces"
        actions={
          <button
            type="button"
            onClick={() => links.append({ platform: 'github', url: '' })}
            className={buttonClasses({ variant: 'secondary', size: 'sm' })}
          >
            <Plus className="h-4 w-4" aria-hidden="true" />
            Agregar enlace
          </button>
        }
      >
        {links.fields.length === 0 ? (
          <p className="text-sm text-slate-500">Sin enlaces.</p>
        ) : (
          <ul className="flex flex-col gap-3">
            {links.fields.map((item, index) => (
              <li
                key={item.id}
                className="grid gap-3 sm:grid-cols-[12rem_minmax(0,1fr)_auto] sm:items-end"
              >
                <Field id={`link-platform-${item.id}`} label="Plataforma">
                  <select
                    id={`link-platform-${item.id}`}
                    className={inputClasses}
                    {...register(`socialLinks.${index}.platform`)}
                  >
                    {SOCIAL_PLATFORMS.map(({ value, label }) => (
                      <option key={value} value={value}>
                        {label}
                      </option>
                    ))}
                  </select>
                </Field>
                <Field id={`link-url-${item.id}`} label="URL">
                  <input
                    id={`link-url-${item.id}`}
                    placeholder="https://… o mailto:tu@correo.com"
                    className={inputClasses}
                    {...register(`socialLinks.${index}.url`)}
                  />
                </Field>
                <button
                  type="button"
                  onClick={() => links.remove(index)}
                  className={buttonClasses({ variant: 'ghost', className: 'text-rose-600' })}
                  aria-label={`Quitar enlace ${index + 1}`}
                >
                  <Trash2 className="h-4 w-4" aria-hidden="true" />
                </button>
              </li>
            ))}
          </ul>
        )}
      </Section>
    </form>
  )
}

export default function ProfilePage() {
  const { data, isPending, isError, error, refetch } = useAdminProfile()
  const missing = isError && error instanceof ApiError && error.status === 404

  if (isPending) return <LoadingState />
  if (isError && !missing) return <ErrorState onRetry={refetch} />
  return <ProfileForm profile={missing ? null : data} />
}
