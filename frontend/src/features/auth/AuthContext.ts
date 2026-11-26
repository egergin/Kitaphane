import { createContext, useContext } from 'react'
import type { AuthResponse, User } from './types'

export type AuthStatus = 'loading' | 'authenticated' | 'anonymous'

export type AuthContextValue = {
  status: AuthStatus
  user: User | null
  /** Stores the session returned by login / verify-email. */
  signIn: (response: AuthResponse) => void
  signOut: () => Promise<void>
  /** Replaces the signed-in user's data after a profile update. */
  updateUser: (user: User) => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used inside <AuthProvider>.')
  }
  return context
}
