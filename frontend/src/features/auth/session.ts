import { ApiError } from '@/lib/http'
import { authApi } from './api'
import type { AuthResponse } from './types'

/**
 * The access token lives only in memory (never in localStorage).
 * The refresh token is an HttpOnly cookie the browser sends to /api/auth automatically.
 */
let accessToken: string | null = null

export const tokenStore = {
  get: () => accessToken,
  set: (token: string) => {
    accessToken = token
  },
  clear: () => {
    accessToken = null
  },
}

let refreshInFlight: Promise<AuthResponse | null> | null = null

/**
 * Exchanges the refresh cookie for a new access token. Resolves null when there is no valid session.
 * Concurrent callers in this tab share one request; Web Locks serialize refreshes across tabs, because
 * the refresh token rotates and reusing an old one ends the session everywhere.
 */
export function refreshSession(): Promise<AuthResponse | null> {
  refreshInFlight ??= runRefresh().finally(() => {
    refreshInFlight = null
  })
  return refreshInFlight
}

async function runRefresh(): Promise<AuthResponse | null> {
  const refresh = async () => {
    try {
      const response = await authApi.refresh()
      tokenStore.set(response.accessToken)
      return response
    } catch (error) {
      tokenStore.clear()
      if (error instanceof ApiError && error.status === 401) {
        return null
      }
      throw error
    }
  }

  return navigator.locks ? navigator.locks.request('kitaphane-auth-refresh', refresh) : refresh()
}
