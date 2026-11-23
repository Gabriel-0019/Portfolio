import { execFileSync } from 'node:child_process'

/** Drops the throw-away LocalDB database created for this run. */
export default function globalTeardown() {
  const database = process.env.E2E_DATABASE
  if (!database || !/^Portfolio_E2E_[a-z0-9]+$/.test(database)) return
  // With E2E_SQL (CI), the SQL Server container is discarded after the job: nothing to clean up.
  if (process.env.E2E_SQL) return

  try {
    execFileSync(
      'sqlcmd',
      [
        '-S',
        '(localdb)\\MSSQLLocalDB',
        '-E',
        '-b',
        '-Q',
        `IF DB_ID('${database}') IS NOT NULL BEGIN ALTER DATABASE [${database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [${database}]; END`,
      ],
      { stdio: 'pipe' },
    )
  } catch (error) {
    console.warn(`Could not drop ${database}: ${error.message}`)
  }
}
