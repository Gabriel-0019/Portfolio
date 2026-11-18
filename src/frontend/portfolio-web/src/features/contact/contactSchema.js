import { z } from 'zod'

// Mirrors the server rules in Portfolio.Application/Contact/SubmitContactMessage.cs.
export const LIMITS = {
  nameMax: 100,
  emailMax: 254,
  subjectMax: 150,
  messageMin: 10,
  messageMax: 4000,
}

/** Builds the contact form schema with translated messages. */
export function createContactSchema(t) {
  const required = t('contact.errors.required')
  const max = (count) => t('contact.errors.maxLength', { count })

  return z.object({
    name: z.string().trim().min(1, required).max(LIMITS.nameMax, max(LIMITS.nameMax)),
    email: z
      .string()
      .trim()
      .min(1, required)
      .max(LIMITS.emailMax, max(LIMITS.emailMax))
      .pipe(z.email(t('contact.errors.email'))),
    subject: z.string().trim().max(LIMITS.subjectMax, max(LIMITS.subjectMax)).optional(),
    message: z
      .string()
      .trim()
      .min(1, required)
      .min(LIMITS.messageMin, t('contact.errors.minLength', { count: LIMITS.messageMin }))
      .max(LIMITS.messageMax, max(LIMITS.messageMax)),
    // Honeypot: hidden from people, often filled in by bots. The server ignores such messages.
    website: z.string().optional(),
  })
}

export const EMPTY_CONTACT = { name: '', email: '', subject: '', message: '', website: '' }
