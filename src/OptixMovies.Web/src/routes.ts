import { useSyncExternalStore } from 'react'

/** The two pages, addressed by the URL hash, such as `#/?q=dark%20knight` and `#/movies/155`. */
export type Route = { page: 'search'; query: string } | { page: 'movie'; id: number }

export const searchHref = (query: string) => `#/?q=${encodeURIComponent(query)}`
export const movieHref = (id: number) => `#/movies/${id}`

export function useRoute(): Route {
  return parseRoute(useSyncExternalStore(subscribe, () => window.location.hash))
}

function parseRoute(hash: string): Route {
  const movie = /^#\/movies\/(\d+)$/.exec(hash)

  if (movie) {
    return { page: 'movie', id: Number(movie[1]) }
  }

  return { page: 'search', query: new URLSearchParams(hash.split('?')[1]).get('q') ?? '' }
}

function subscribe(onChange: () => void) {
  window.addEventListener('hashchange', onChange)
  return () => window.removeEventListener('hashchange', onChange)
}
