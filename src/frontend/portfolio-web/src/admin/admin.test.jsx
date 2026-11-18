import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { jsonResponse, mockApi, renderApp } from '../test/utils.jsx'

const unauthorized = () => new Response(null, { status: 401 })
const me = () => jsonResponse({ email: 'admin@example.test' })

const technologies = [
  {
    id: 1,
    name: 'React',
    iconKey: 'react',
    category: 'Frontend',
    displayOrder: 0,
    projectCount: 1,
  },
  {
    id: 2,
    name: 'SQL Server',
    iconKey: null,
    category: 'Database',
    displayOrder: 1,
    projectCount: 0,
  },
]

describe('admin panel', () => {
  it('redirects to the login page when not signed in', async () => {
    mockApi({ 'GET /api/auth/me': unauthorized })

    const { router } = renderApp('/admin/projects')

    expect(
      await screen.findByRole('heading', { name: 'Panel de administración' }),
    ).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/admin/login')
    expect(router.state.location.search).toBe('?returnTo=%2Fadmin%2Fprojects')
  })

  it('signs in and returns to the requested page', async () => {
    let signedIn = false
    let sentCredentials
    mockApi({
      'GET /api/auth/me': () => (signedIn ? me() : unauthorized()),
      'POST /api/auth/login': (url, init) => {
        sentCredentials = JSON.parse(init.body)
        signedIn = true
        return me()
      },
      'GET /api/admin/technologies': () => jsonResponse(technologies),
      'GET /api/admin/messages': () => jsonResponse([]),
    })
    const user = userEvent.setup()
    const { router } = renderApp('/admin/login?returnTo=%2Fadmin%2Ftechnologies')

    await user.type(await screen.findByLabelText('Correo electrónico'), 'admin@example.test')
    await user.type(screen.getByLabelText('Contraseña'), 'secret')
    await user.click(screen.getByRole('button', { name: 'Entrar' }))

    expect(
      await screen.findByRole('heading', { level: 1, name: 'Tecnologías' }),
    ).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/admin/technologies')
    expect(sentCredentials).toEqual({
      email: 'admin@example.test',
      password: 'secret',
      rememberMe: false,
    })
  })

  it('ignores external returnTo values (open redirect)', async () => {
    mockApi({
      'GET /api/auth/me': unauthorized,
      'POST /api/auth/login': me,
      'GET /api/admin/projects': () => jsonResponse([]),
      'GET /api/admin/messages': () => jsonResponse([]),
    })
    const user = userEvent.setup()
    const { router } = renderApp('/admin/login?returnTo=https%3A%2F%2Fevil.example')

    await user.type(await screen.findByLabelText('Correo electrónico'), 'a@b.test')
    await user.type(screen.getByLabelText('Contraseña'), 'x')
    await user.click(screen.getByRole('button', { name: 'Entrar' }))

    await screen.findByRole('button', { name: 'Salir' })
    expect(router.state.location.pathname).toBe('/admin/projects')
  })

  it('shows a clear message for wrong credentials', async () => {
    mockApi({
      'GET /api/auth/me': unauthorized,
      'POST /api/auth/login': () =>
        jsonResponse({ title: 'Unauthorized', status: 401 }, 401, 'application/problem+json'),
    })
    const user = userEvent.setup()
    renderApp('/admin/login')

    await user.type(await screen.findByLabelText('Correo electrónico'), 'a@b.test')
    await user.type(screen.getByLabelText('Contraseña'), 'wrong')
    await user.click(screen.getByRole('button', { name: 'Entrar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Correo o contraseña incorrectos.')
  })

  it('creates a project with both translations and selected technologies', async () => {
    let created
    mockApi({
      'GET /api/auth/me': me,
      'GET /api/admin/messages': () => jsonResponse([{ id: 1, isRead: false }]),
      'GET /api/admin/technologies': () => jsonResponse(technologies),
      'POST /api/admin/projects': (url, init) => {
        created = JSON.parse(init.body)
        return jsonResponse(
          { id: 42, ...created, technologyIds: created.technologyIds, isPubliclyVisible: true },
          201,
        )
      },
      'GET /api/admin/projects/42': () =>
        jsonResponse({ id: 42, ...created, isPubliclyVisible: true, createdAt: '', updatedAt: '' }),
    })
    const user = userEvent.setup()
    const { router } = renderApp('/admin/projects/new')

    expect(
      await screen.findByRole('heading', { level: 1, name: 'Nuevo proyecto' }),
    ).toBeInTheDocument()
    // Unread badge in the sidebar.
    expect(screen.getByLabelText('1 sin leer')).toBeInTheDocument()

    const english = screen.getByRole('group', { name: 'Inglés' })
    const spanish = screen.getByRole('group', { name: 'Español' })
    await user.type(within(english).getByLabelText('Título'), 'Task Tracker')
    await user.type(within(english).getByLabelText('Resumen'), 'Tracks tasks.')
    await user.type(within(spanish).getByLabelText('Título'), 'Gestor de tareas')
    await user.type(within(spanish).getByLabelText('Resumen'), 'Gestiona tareas.')
    await user.click(screen.getByLabelText('Slug (URL)'))
    await user.selectOptions(screen.getByLabelText('Estado'), 'Published')
    await user.click(screen.getByLabelText('React'))
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(await screen.findByText('Cambios guardados.')).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/admin/projects/42')
    expect(created).toMatchObject({
      slug: 'task-tracker',
      status: 'Published',
      technologyIds: [1],
      translations: [
        { language: 'en', title: 'Task Tracker', summary: 'Tracks tasks.', content: '' },
        { language: 'es', title: 'Gestor de tareas', summary: 'Gestiona tareas.', content: '' },
      ],
      images: [],
    })
  })

  it('lists server validation errors', async () => {
    mockApi({
      'GET /api/auth/me': me,
      'GET /api/admin/messages': () => jsonResponse([]),
      'GET /api/admin/technologies': () => jsonResponse(technologies),
      'POST /api/admin/projects': () =>
        jsonResponse(
          {
            title: 'Validation',
            status: 400,
            errors: { 'Translations[0].Title': ["'Title' must not be empty."] },
          },
          400,
          'application/problem+json',
        ),
    })
    const user = userEvent.setup()
    renderApp('/admin/projects/new')

    await user.click(await screen.findByRole('button', { name: 'Guardar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      "Translations 1 › Title: 'Title' must not be empty.",
    )
  })
})
