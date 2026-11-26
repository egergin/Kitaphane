import { useQueryClient } from '@tanstack/react-query'
import { type ReactNode, useCallback, useEffect, useMemo, useState } from 'react'
import { setAccessTokenProvider, setUnauthorizedHandler } from '@/lib/http'
import { authApi } from './api'
import { AuthContext, type AuthContextValue, type AuthStatus } from './AuthContext'
import { refreshSession, tokenStore } from './session'
import type { AuthResponse, User } from './types'

setAccessTokenProvider(tokenStore.get)

type AuthState = { status: AuthStatus; user: User | null }

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [state, setState] = useState<AuthState>({ status: 'loading', user: null })

  const setAnonymous = useCallback(() => {
    tokenStore.clear()
    queryClient.clear()
    setState({ status: 'anonymous', user: null })
  }, [queryClient])

  // Restore the session from the refresh cookie on page load.
  useEffect(() => {
    let active = true
    refreshSession()
      .then((response) => {
        if (!active) return
        setState(response ? { status: 'authenticated', user: response.user } : { status: 'anonymous', user: null })
      })
      .catch(() => {
        if (active) setState({ status: 'anonymous', user: null })
      })
    return () => {
      active = false
    }
  }, [])

  // An expired access token is refreshed silently; if that fails the user is signed out.
  useEffect(() => {
    setUnauthorizedHandler(async () => {
      const response = await refreshSession().catch(() => null)
      if (response) {
        setState({ status: 'authenticated', user: response.user })
        return true
      }
      setAnonymous()
      return false
    })
    return () => setUnauthorizedHandler(null)
  }, [setAnonymous])

  const signIn = useCallback((response: AuthResponse) => {
    tokenStore.set(response.accessToken)
    setState({ status: 'authenticated', user: response.user })
  }, [])

  const signOut = useCallback(async () => {
    try {
      await authApi.logout()
    } finally {
      setAnonymous()
    }
  }, [setAnonymous])

  const updateUser = useCallback((user: User) => {
    setState((current) => (current.status === 'authenticated' ? { ...current, user } : current))
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({ status: state.status, user: state.user, signIn, signOut, updateUser }),
    [state, signIn, signOut, updateUser],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
