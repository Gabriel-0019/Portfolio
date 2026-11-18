import { useLocation } from 'react-router'
import { useTranslation } from 'react-i18next'
import { SUPPORTED_LANGUAGES, switchLanguageInPath } from '../../i18n/index.js'

/**
 * Per-page metadata. React 19 hoists <title>, <meta> and <link> into <head>.
 * Note: social crawlers don't run JavaScript, so Open Graph previews need prerendering (phase 4).
 */
export default function Seo({ title, description }) {
  const { t, i18n } = useTranslation()
  const { pathname } = useLocation()
  const siteName = t('common.siteName')
  const fullTitle = title ? `${title} · ${siteName}` : siteName
  const origin = typeof window !== 'undefined' ? window.location.origin : ''

  return (
    <>
      <title>{fullTitle}</title>
      {description && <meta name="description" content={description} />}
      <meta property="og:type" content="website" />
      <meta property="og:title" content={fullTitle} />
      {description && <meta property="og:description" content={description} />}
      <meta property="og:locale" content={i18n.resolvedLanguage === 'es' ? 'es_ES' : 'en_US'} />
      {SUPPORTED_LANGUAGES.map((lang) => (
        <link
          key={lang}
          rel="alternate"
          hrefLang={lang}
          href={`${origin}${switchLanguageInPath(pathname, lang)}`}
        />
      ))}
    </>
  )
}
