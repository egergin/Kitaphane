/**
 * Cloudflare Worker for the Kitaphane frontend (docs/deployment/deployment.md).
 *
 * - /api/* is forwarded to the backend on Render. The browser only talks to this Worker's origin, so
 *   the API needs no CORS and the SameSite=Strict refresh token cookie keeps working.
 * - Everything else is served from the built React app (static assets, see wrangler.jsonc); unknown
 *   paths fall back to index.html (single-page application).
 *
 * The backend accepts requests only with PROXY_SHARED_SECRET and takes the visitor's IP from
 * X-Kitaphane-Client-IP (used for rate limiting). Both headers are always overwritten here, so a
 * visitor cannot supply them.
 */

interface Env {
  /** Backend service URL, e.g. https://kitaphane-api.onrender.com (wrangler.jsonc "vars"). */
  BACKEND_URL?: string
  /** Secret shared with the backend (Workers secret, set in the Cloudflare dashboard). */
  PROXY_SHARED_SECRET?: string
  /** Static assets of the React build (wrangler.jsonc "assets"). */
  ASSETS: { fetch(request: Request): Promise<Response> }
}

const SECRET_HEADER = 'X-Kitaphane-Proxy-Secret'
const CLIENT_IP_HEADER = 'X-Kitaphane-Client-IP'

/** Connection-level headers that must not be forwarded. */
const HOP_BY_HOP_HEADERS = ['connection', 'keep-alive', 'proxy-authorization', 'te', 'trailer', 'transfer-encoding', 'upgrade', 'host']

export async function proxyToBackend(request: Request, env: Env): Promise<Response> {
  if (!env.BACKEND_URL || !env.PROXY_SHARED_SECRET) {
    // Only the names of the missing settings are reported, never their values.
    const missing = (['BACKEND_URL', 'PROXY_SHARED_SECRET'] as const).filter((name) => !env[name])
    return Response.json(
      { message: `The API proxy is not configured (missing: ${missing.join(', ')}).`, code: 'INTERNAL_ERROR' },
      { status: 500 },
    )
  }

  const incoming = new URL(request.url)
  const target = new URL(incoming.pathname + incoming.search, env.BACKEND_URL)

  const headers = new Headers(request.headers)
  for (const name of HOP_BY_HOP_HEADERS) headers.delete(name)
  // The backend host (Render) is itself behind Cloudflare, which rejects requests that carry
  // Cloudflare's own headers such as CF-Connecting-IP (403). Client-supplied forwarding headers are
  // dropped too; the visitor IP travels only in CLIENT_IP_HEADER.
  for (const name of [...headers.keys()]) {
    if (name.startsWith('cf-') || name.startsWith('x-forwarded-') || name === 'x-real-ip') headers.delete(name)
  }
  headers.set(SECRET_HEADER, env.PROXY_SHARED_SECRET)
  headers.delete(CLIENT_IP_HEADER)
  const clientIp = request.headers.get('CF-Connecting-IP')
  if (clientIp) headers.set(CLIENT_IP_HEADER, clientIp)

  const hasBody = request.method !== 'GET' && request.method !== 'HEAD'
  try {
    const response = await fetch(target, {
      method: request.method,
      headers,
      body: hasBody ? request.body : undefined,
      redirect: 'manual',
    })
    // Status, headers (including Set-Cookie for the refresh token) and body are passed through as is.
    return new Response(response.body, response)
  } catch {
    return Response.json({ message: 'The server cannot be reached.', code: 'INTERNAL_ERROR' }, { status: 502 })
  }
}

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const { pathname } = new URL(request.url)
    if (pathname === '/api' || pathname.startsWith('/api/')) {
      return proxyToBackend(request, env)
    }
    return env.ASSETS.fetch(request)
  },
}
