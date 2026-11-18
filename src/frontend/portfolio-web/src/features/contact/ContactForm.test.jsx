import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { jsonResponse, mockApi, renderApp } from '../../test/utils.jsx'

async function fillValidForm(user) {
  await user.type(screen.getByLabelText('Name'), 'Ana')
  await user.type(screen.getByLabelText('Email'), 'ana@example.com')
  await user.type(screen.getByLabelText('Message'), 'I would like to talk about a project.')
}

describe('ContactForm', () => {
  it('validates fields before sending', async () => {
    const fetchSpy = mockApi()
    const user = userEvent.setup()
    renderApp('/en/contact')

    await user.click(await screen.findByRole('button', { name: 'Send message' }))

    expect(await screen.findAllByText('This field is required.')).toHaveLength(3)
    expect(screen.getByLabelText('Name')).toHaveAttribute('aria-invalid', 'true')
    expect(fetchSpy.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false)
  })

  it('shows translated validation messages in Spanish', async () => {
    mockApi()
    const user = userEvent.setup()
    renderApp('/es/contact')

    await user.type(await screen.findByLabelText('Correo electrónico'), 'not-an-email')
    await user.click(screen.getByRole('button', { name: 'Enviar mensaje' }))

    expect(await screen.findByText('Ingresa un correo electrónico válido.')).toBeInTheDocument()
  })

  it('sends the message with the page language and shows a confirmation', async () => {
    let sent
    mockApi({
      'POST /api/contact': (url, init) => {
        sent = { lang: url.searchParams.get('lang'), body: JSON.parse(init.body) }
        return new Response(null, { status: 202 })
      },
    })
    const user = userEvent.setup()
    renderApp('/en/contact')

    await screen.findByRole('button', { name: 'Send message' })
    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: 'Send message' }))

    expect(await screen.findByRole('heading', { name: 'Message sent' })).toBeInTheDocument()
    expect(sent.lang).toBe('en')
    expect(sent.body).toMatchObject({
      name: 'Ana',
      email: 'ana@example.com',
      subject: null,
      message: 'I would like to talk about a project.',
      website: '',
    })
  })

  it('maps server validation errors to fields', async () => {
    mockApi({
      'POST /api/contact': () =>
        jsonResponse(
          { title: 'Validation failed', status: 400, errors: { Email: ['Email is blocked.'] } },
          400,
          'application/problem+json',
        ),
    })
    const user = userEvent.setup()
    renderApp('/en/contact')

    await screen.findByRole('button', { name: 'Send message' })
    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: 'Send message' }))

    expect(await screen.findByText('Email is blocked.')).toBeInTheDocument()
  })

  it('explains when the rate limit is reached', async () => {
    mockApi({ 'POST /api/contact': () => new Response(null, { status: 429 }) })
    const user = userEvent.setup()
    renderApp('/en/contact')

    await screen.findByRole('button', { name: 'Send message' })
    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: 'Send message' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/Too many messages sent/)
  })
})
