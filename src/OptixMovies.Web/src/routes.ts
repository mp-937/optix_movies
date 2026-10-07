import { useSyncExternalStore } from 'react'

/** What the search box searches: titles, or descriptions by meaning. */
export type SearchBy = 'title' | 'description'

/** The two pages, addressed by the URL hash, such as `#/?q=heist+goes+wrong`, `#/?q=dark+knight&by=title` and `#/movies/155`. */
export type Route = { page: 'search'; query: string; by: SearchBy } | { page: 'movie'; id: number }

export function searchHref(query: string, by: SearchBy) {
  const params = new URLSearchParams()

  if (query) {
    params.set('q', query)
  }

  // Description search is the default, so only title search needs saying.
  if (by === 'title') {
    params.set('by', by)
  }

  const search = params.toString()

  return search ? `#/?${search}` : '#/'
}

export const movieHref = (id: number) => `#/movies/${id}`

export function useRoute(): Route {
  return parseRoute(useSyncExternalStore(subscribe, () => window.location.hash))
}

function parseRoute(hash: string): Route {
  const movie = /^#\/movies\/(\d+)$/.exec(hash)

  if (movie) {
    return { page: 'movie', id: Number(movie[1]) }
  }

  const params = new URLSearchParams(hash.split('?')[1])

  return {
    page: 'search',
    query: params.get('q') ?? '',
    by: params.get('by') === 'title' ? 'title' : 'description',
  }
}

function subscribe(onChange: () => void) {
  window.addEventListener('hashchange', onChange)
  return () => window.removeEventListener('hashchange', onChange)
}
