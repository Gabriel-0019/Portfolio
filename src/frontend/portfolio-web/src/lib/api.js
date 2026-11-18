const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''

/** Error thrown for non-2xx responses. Carries the HTTP status and ProblemDetails body. */
export class ApiError extends Error {
  constructor(message, status, problem) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

function buildUrl(path, params) {
  const url = new URL(`${API_BASE_URL}${path}`, window.location.origin)
  for (const [key, value] of Object.entries(params ?? {})) {
    if (value !== undefined && value !== null && value !== '') {
      url.searchParams.set(key, value)
    }
  }
  return API_BASE_URL ? url.toString() : `${url.pathname}${url.search}`
}

async function request(path, { method = 'GET', params, body, signal } = {}) {
  const response = await fetch(buildUrl(path, params), {
    method,
    headers: {
      Accept: 'application/json',
      ...(body !== undefined ? { 'Content-Type': 'application/json' } : {}),
    },
    body: body !== undefined ? JSON.stringify(body) : undefined,
    signal,
  })

  if (!response.ok) {
    let problem = null
    try {
      problem = await response.json()
    } catch {
      // Body was not JSON.
    }
    throw new ApiError(
      problem?.title ?? `Request failed with status ${response.status}`,
      response.status,
      problem,
    )
  }

  if (response.status === 202 || response.status === 204) return null
  const text = await response.text()
  return text ? JSON.parse(text) : null
}

export function apiGet(path, options) {
  return request(path, options)
}

export function apiPost(path, body, options) {
  return request(path, { ...options, method: 'POST', body })
}

export const api = {
  status: (options) => apiGet('/api/status', options),
  profile: (lang, options) => apiGet('/api/profile', { ...options, params: { lang } }),
  experience: (lang, options) => apiGet('/api/experience', { ...options, params: { lang } }),
  technologies: (options) => apiGet('/api/technologies', options),
  projects: (lang, filters = {}, options) =>
    apiGet('/api/projects', { ...options, params: { lang, ...filters } }),
  project: (slug, lang, options) =>
    apiGet(`/api/projects/${encodeURIComponent(slug)}`, { ...options, params: { lang } }),
  sendContact: (lang, message) => apiPost('/api/contact', message, { params: { lang } }),
}
