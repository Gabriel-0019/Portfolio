import { execFileSync } from 'node:child_process'
import { existsSync } from 'node:fs'
import { parseConnectionString, serverConnectionString } from './database.js'

/** psql from PATH, or the default Windows install location of PostgreSQL 17. */
function findPsql() {
  const windowsDefault = 'C:\\Program Files\\PostgreSQL\\17\\bin\\psql.exe'
  return process.env.PSQL ?? (existsSync(windowsDefault) ? windowsDefault : 'psql')
}

/** Drops the throw-away PostgreSQL database created for this run. */
export default function globalTeardown() {
  const database = process.env.E2E_DATABASE
  if (!database || !/^portfolio_e2e_[a-z0-9]+$/.test(database)) return
  // With E2E_PG (CI), the postgres container is discarded after the job: nothing to clean up.
  if (process.env.E2E_PG) return

  const server = parseConnectionString(serverConnectionString())
  try {
    execFileSync(
      findPsql(),
      [
        '-h',
        server.host ?? 'localhost',
        '-p',
        server.port ?? '5432',
        '-U',
        server.username ?? 'postgres',
        '-d',
        'postgres',
        '-v',
        'ON_ERROR_STOP=1',
        '-c',
        `DROP DATABASE IF EXISTS "${database}" WITH (FORCE)`,
      ],
      // The password goes only to this child process, never to the command line or logs.
      { stdio: 'pipe', env: { ...process.env, PGPASSWORD: server.password ?? '' } },
    )
  } catch (error) {
    console.warn(`Could not drop ${database}: ${error.stderr?.toString() || error.message}`)
  }
}
