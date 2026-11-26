/**
 * Central HTTP client for the Kitaphane API.
 * - Prefixes VITE_API_BASE_URL
 * - Sends JSON and the access token (when available)
 * - Converts the API error format ({ message, code, errors? }) into ApiError
 */

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? '/api').replace(/\/$/, '')

export type ApiErrorBody = {
  message: string
  code: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  readonly code: string
  readonly errors?: Record<string, string[]>

  constructor(status: number, body: ApiErrorBody) {
    super(body.message)
    this.name = 'ApiError'
    this.status = status
    this.code = body.code
    this.errors = body.errors
  }
}

/** Raised when the request never reached the API (offline, server down, CORS). */
export const NETWORK_ERROR_CODE = 'NETWORK_ERROR'

type AccessTokenProvider = () => string | null
/** Tries to obtain a new access token; resolves true when the request can be retried. */
type UnauthorizedHandler = () => Promise<boolean>
type LanguageProvider = () => string

let getAccessToken: AccessTokenProvider = () => null
let onUnauthorized: UnauthorizedHandler | null = null
let getLanguage: LanguageProvider = () => 'tr'

/** Registered by the auth feature so requests carry the in-memory access token. */
export function setAccessTokenProvider(provider: AccessTokenProvider) {
  getAccessToken = provider
}

/** Registered by the auth feature to refresh the session when the access token has expired. */
export function setUnauthorizedHandler(handler: UnauthorizedHandler | null) {
  onUnauthorized = handler
}

/** The API uses Accept-Language to pick the email language (tr | en). */
export function setLanguageProvider(provider: LanguageProvider) {
  getLanguage = provider
}

/** Auth flow endpoints answer 401 for wrong credentials/tokens; they must never trigger a silent refresh. */
const NO_REFRESH_PATHS = new Set([
  '/auth/login',
  '/auth/register',
  '/auth/verify-email',
  '/auth/resend-verification',
  '/auth/refresh',
  '/auth/logout',
  '/auth/forgot-password',
  '/auth/verify-reset-code',
  '/auth/reset-password',
])

type RequestOptions = Omit<RequestInit, 'body'> & {
  body?: unknown
}

export async function request<T>(path: string, options: RequestOptions = {}, isRetry = false): Promise<T> {
  const { body, headers, ...init } = options
  const requestHeaders = new Headers(headers)
  requestHeaders.set('Accept-Language', getLanguage())

  if (body !== undefined) {
    requestHeaders.set('Content-Type', 'application/json')
  }

  const token = getAccessToken()
  if (token) {
    requestHeaders.set('Authorization', `Bearer ${token}`)
  }

  let response: Response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      ...init,
      headers: requestHeaders,
      body: body === undefined ? undefined : JSON.stringify(body),
      credentials: 'include',
    })
  } catch {
    throw new ApiError(0, { message: 'Network error.', code: NETWORK_ERROR_CODE })
  }

  if (response.status === 401 && !isRetry && onUnauthorized && !NO_REFRESH_PATHS.has(path)) {
    if (await onUnauthorized()) {
      return request<T>(path, options, true)
    }
  }

  if (!response.ok) {
    throw new ApiError(response.status, await readErrorBody(response))
  }

  if (response.status === 204) {
    return undefined as T
  }

  const contentType = response.headers.get('Content-Type') ?? ''
  return (contentType.includes('application/json') ? response.json() : response.text()) as Promise<T>
}

async function readErrorBody(response: Response): Promise<ApiErrorBody> {
  try {
    const body = (await response.json()) as Partial<ApiErrorBody>
    if (typeof body.code === 'string' && typeof body.message === 'string') {
      return body as ApiErrorBody
    }
  } catch {
    // Non-JSON error body (e.g. proxy error page) - fall through to generic error.
  }
  return { message: response.statusText || 'Request failed.', code: 'INTERNAL_ERROR' }
}

export const http = {
  get: <T>(path: string, options?: RequestOptions) => request<T>(path, { ...options, method: 'GET' }),
  post: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>(path, { ...options, method: 'POST', body }),
  put: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>(path, { ...options, method: 'PUT', body }),
  delete: <T>(path: string, options?: RequestOptions) => request<T>(path, { ...options, method: 'DELETE' }),
}
