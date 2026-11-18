import { ThemeContext, useThemeState } from './theme.js'

export default function ThemeProvider({ children }) {
  const value = useThemeState()
  return <ThemeContext value={value}>{children}</ThemeContext>
}
