import path from 'node:path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  // Backend address used by the dev server proxy (docker: http://backend:5000).
  const apiProxyTarget = env.API_PROXY_TARGET || 'http://localhost:5000'

  return {
    plugins: [react(), tailwindcss()],
    resolve: {
      alias: {
        '@': path.resolve(import.meta.dirname, './src'),
      },
    },
    server: {
      host: true,
      port: 5173,
      strictPort: true,
      proxy: {
        // Same-origin API access so the HttpOnly refresh-token cookie works without cross-site settings.
        '/api': {
          target: apiProxyTarget,
          changeOrigin: false,
        },
      },
    },
  }
})
