import { redirect } from 'react-router'
import InitialLoading from '../components/layout/InitialLoading.jsx'
import RootLayout from '../components/layout/RootLayout.jsx'
import RouteError from '../components/layout/RouteError.jsx'
import i18n, { detectLanguage, isSupportedLanguage, switchLanguageInPath } from '../i18n/index.js'

/**
 * Every page lives under a language prefix (/en/..., /es/...).
 * Paths without a valid prefix are redirected to the detected language.
 */
async function languageLoader({ params, request }) {
  if (!isSupportedLanguage(params.lang)) {
    const url = new URL(request.url)
    throw redirect(switchLanguageInPath(url.pathname, detectLanguage()) + url.search)
  }

  if (i18n.resolvedLanguage !== params.lang) {
    await i18n.changeLanguage(params.lang)
  }
  document.documentElement.lang = params.lang
  return null
}

/** Pages are code-split: each one is downloaded the first time it is visited. */
const page = (load) => () => load().then((module) => ({ Component: module.default }))

export const routes = [
  {
    path: '/',
    loader: () => redirect(`/${detectLanguage()}`),
    HydrateFallback: InitialLoading,
  },
  {
    path: '/:lang',
    loader: languageLoader,
    element: <RootLayout />,
    errorElement: <RouteError />,
    HydrateFallback: InitialLoading,
    children: [
      { index: true, lazy: page(() => import('../pages/HomePage.jsx')) },
      { path: 'projects', lazy: page(() => import('../pages/ProjectsPage.jsx')) },
      { path: 'projects/:slug', lazy: page(() => import('../pages/ProjectDetailPage.jsx')) },
      { path: 'about', lazy: page(() => import('../pages/AboutPage.jsx')) },
      { path: 'contact', lazy: page(() => import('../pages/ContactPage.jsx')) },
      { path: '*', lazy: page(() => import('../pages/NotFoundPage.jsx')) },
    ],
  },
]
