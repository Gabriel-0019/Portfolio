import { Outlet, ScrollRestoration } from 'react-router'
import { useTranslation } from 'react-i18next'
import Footer from './Footer.jsx'
import Header from './Header.jsx'

export default function RootLayout() {
  const { t } = useTranslation()

  return (
    <div className="flex min-h-svh flex-col">
      <a
        href="#content"
        className="sr-only z-50 rounded-lg bg-indigo-600 px-4 py-2 text-white focus:not-sr-only focus:fixed focus:top-3 focus:left-3"
      >
        {t('nav.skipToContent')}
      </a>
      <Header />
      <main id="content" tabIndex={-1} className="flex-1 focus:outline-none">
        <Outlet />
      </main>
      <Footer />
      <ScrollRestoration />
    </div>
  )
}
