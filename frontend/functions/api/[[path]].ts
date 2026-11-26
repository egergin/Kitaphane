/**
 * Cloudflare Pages Function: forwards every /api/* request to the backend on Render
 * (docs/deployment/deployment.md). The browser only talks to the Pages origin, so the API needs no
 * CORS and the SameSite=Strict refresh token cookie keeps working.
 *
 * The backend accepts requests only with PROXY_SHARED_SECRET and takes the visitor's IP from
 * X-Kitaphane-Client-IP (used for rate limiting). Both headers are always overwritten here, so a
 * visitor cannot supply them.
 */

interface Env {
  /** Backend service URL, e.g. https://kitaphane-api.onrender.com */
  BACKEND_URL?: string
  PROXY_SHARED_SECRET?: string
}

type PagesContext = { request: Request; env: Env }

const SECRET_HEADER = 'X-Kitaphane-Proxy-Secret'
const CLIENT_IP_HEADER = 'X-Kitaphane-Client-IP'

/** Connection-level headers that must not be forwarded. */
const HOP_BY_HOP_HEADERS = ['connection', 'keep-alive', 'proxy-authorization', 'te', 'trailer', 'transfer-encoding', 'upgrade', 'host']

export async function onRequest({ request, env }: PagesContext): Promise<Response> {
  if (!env.BACKEND_URL || !env.PROXY_SHARED_SECRET) {
    return Response.json({ message: 'The API proxy is not configured.', code: 'INTERNAL_ERROR' }, { status: 500 })
  }

  const incoming = new URL(request.url)
  const target = new URL(incoming.pathname + incoming.search, env.BACKEND_URL)

  const headers = new Headers(request.headers)
  for (const name of HOP_BY_HOP_HEADERS) headers.delete(name)
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
