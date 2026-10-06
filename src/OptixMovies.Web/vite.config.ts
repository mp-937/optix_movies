import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // "localhost" would bind IPv6 only on Windows, so browsers trying IPv4 first wait two seconds for each new
    // connection, and cancelled requests close connections. An explicit IPv4 address avoids the guesswork.
    host: '127.0.0.1',
    // Forward API calls to OptixMovies.Api, so the browser only ever talks to one origin and needs no CORS.
    proxy: { '/api': 'http://localhost:5246' },
  },
})
