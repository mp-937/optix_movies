import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Forward API calls to OptixMovies.Api, so the browser only ever talks to one origin and needs no CORS.
    proxy: { '/api': 'http://localhost:5246' },
  },
})
