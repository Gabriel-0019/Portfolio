import { existsSync, readFileSync } from 'node:fs'
import path from 'node:path'

const API_USER_SECRETS_ID = 'portfolio-api-6f2a1c4e'

/** Parses "Key=Value;Key=Value" (Npgsql connection string) into a lower-cased key map. */
export function parseConnectionString(value) {
  return Object.fromEntries(
    value
      .split(';')
      .map((part) => part.trim())
      .filter(Boolean)
      .map((part) => {
        const index = part.indexOf('=')
        return [part.slice(0, index).trim().toLowerCase(), part.slice(index + 1).trim()]
      }),
  )
}

/**
 * PostgreSQL server for the e2e run: E2E_PG (CI, a postgres container) or, locally, the API's
 * ConnectionStrings:Portfolio from user secrets (secrets.json). Only host/port/credentials are reused.
 */
export function serverConnectionString() {
  if (process.env.E2E_PG) return process.env.E2E_PG

  const appData = process.env.APPDATA ?? path.join(process.env.HOME ?? '', '.microsoft')
  const file = path.join(appData, 'Microsoft', 'UserSecrets', API_USER_SECRETS_ID, 'secrets.json')
  if (!existsSync(file)) {
    throw new Error(`No PostgreSQL for the e2e tests: set E2E_PG or create ${file}`)
  }

  const secrets = JSON.parse(readFileSync(file, 'utf8').replace(/^﻿/, ''))
  const connection = secrets['ConnectionStrings:Portfolio'] ?? secrets.ConnectionStrings?.Portfolio
  if (!connection) throw new Error(`ConnectionStrings:Portfolio is missing in ${file}`)
  return connection
}

/** Same server and credentials, different database. */
export function withDatabase(connection, database) {
  const parts = connection
    .split(';')
    .map((part) => part.trim())
    .filter(Boolean)
    .filter((part) => !/^database\s*=/i.test(part))
  return [...parts, `Database=${database}`].join(';')
}
