import { request } from '../lib/api.js'

const json = (method) => (path, body) => request(path, { method, body })
const put = json('PUT')
const post = json('POST')
const patch = json('PATCH')
const del = (path) => request(path, { method: 'DELETE' })

export const adminApi = {
  me: (options) => request('/api/auth/me', options),
  login: (credentials) => post('/api/auth/login', credentials),
  logout: () => post('/api/auth/logout'),

  projects: (options) => request('/api/admin/projects', options),
  project: (id, options) => request(`/api/admin/projects/${id}`, options),
  createProject: (body) => post('/api/admin/projects', body),
  updateProject: (id, body) => put(`/api/admin/projects/${id}`, body),
  deleteProject: (id) => del(`/api/admin/projects/${id}`),

  technologies: (options) => request('/api/admin/technologies', options),
  createTechnology: (body) => post('/api/admin/technologies', body),
  updateTechnology: (id, body) => put(`/api/admin/technologies/${id}`, body),
  deleteTechnology: (id) => del(`/api/admin/technologies/${id}`),

  experience: (options) => request('/api/admin/experience', options),
  createExperience: (body) => post('/api/admin/experience', body),
  updateExperience: (id, body) => put(`/api/admin/experience/${id}`, body),
  deleteExperience: (id) => del(`/api/admin/experience/${id}`),

  profile: (options) => request('/api/admin/profile', options),
  saveProfile: (body) => put('/api/admin/profile', body),

  messages: (options) => request('/api/admin/messages', options),
  setMessageRead: (id, isRead) => patch(`/api/admin/messages/${id}`, { isRead }),
  deleteMessage: (id) => del(`/api/admin/messages/${id}`),

  upload: (file) => {
    const form = new FormData()
    form.append('file', file)
    return request('/api/admin/uploads', { method: 'POST', body: form })
  },
}
