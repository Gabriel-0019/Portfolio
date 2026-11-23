import { QueryClientProvider } from '@tanstack/react-query'
import { useState } from 'react'
import { createBrowserRouter, RouterProvider } from 'react-router'
import { createQueryClient } from './queryClient.js'
import { routes } from './routes.jsx'
import ThemeProvider from './ThemeProvider.jsx'

export default function App({ initialData = null }) {
  const [queryClient] = useState(() => createQueryClient(initialData))
  const [router] = useState(() => createBrowserRouter(routes))

  return (
    <ThemeProvider>
      <QueryClientProvider client={queryClient}>
        <RouterProvider router={router} />
      </QueryClientProvider>
    </ThemeProvider>
  )
}
