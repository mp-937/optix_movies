import { useEffect, useState } from 'react'
import type { components } from './api-schema'
import type { SearchBy } from './routes.ts'

export type Movie = components['schemas']['MovieResponse']
type TitleSuggestion = components['schemas']['TitleSuggestionResponse']

/** A movie in a list of results: enough to show it and link to it. */
export type MovieListItem = { id: number; title: string; year: number }

export const listItem = (movie: Movie): MovieListItem => ({
  id: movie.id,
  title: movie.title,
  year: Number(movie.releaseDate.slice(0, 4)),
})

const resultLimit = 20

type Result<T> = { url: string; data?: T; error?: string }

/** Fetches JSON from the API whenever `url` changes. A null `url` fetches nothing. */
export function useApi<T>(url: string | null) {
  const [result, setResult] = useState<Result<T>>()

  useEffect(() => {
    if (url === null) {
      return
    }

    const request = new AbortController()

    getJson<T>(url, request.signal).then(
      (data) => setResult({ url, data }),
      (error: unknown) => {
        if (!request.signal.aborted) {
          setResult({ url, error: describe(error) })
        }
      },
    )

    return () => request.abort()
  }, [url])

  // A result for a previous URL is stale: show nothing while the current one loads rather than the wrong data.
  const current = result?.url === url ? result : undefined

  return { data: current?.data, error: current?.error, loading: url !== null && current === undefined }
}

type Results = { query: string; by: SearchBy; movies?: MovieListItem[]; error?: string }

/**
 * `query` and `by` say what the results on show were for: `query` is undefined until the first results arrive, and
 * empty for no search.
 */
type SearchState = { query?: string; by: SearchBy; movies?: MovieListItem[]; error?: string; busy: boolean }

/**
 * Searches titles, or descriptions by meaning, for what the user is typing, once typing pauses for `delay`
 * milliseconds. Every keystroke cancels the pending wait and any request still in flight. The last results stay on
 * show until new ones arrive, and `busy` is true from the first keystroke.
 */
export function useSearch(text: string, by: SearchBy, delay: number): SearchState {
  const query = text.trim()
  const [shown, setShown] = useState<Results>()

  useEffect(() => {
    if (!query) {
      return
    }

    const request = new AbortController()

    const wait = setTimeout(() => {
      search(query, by, request.signal).then(
        (movies) => setShown({ query, by, movies }),
        (error: unknown) => {
          if (!request.signal.aborted) {
            // Keep the previous results on screen, with the error above them.
            setShown((previous) => ({ query, by, movies: previous?.movies, error: describe(error) }))
          }
        },
      )
    }, delay)

    return () => {
      clearTimeout(wait)
      request.abort()
    }
  }, [query, by, delay])

  if (!query) {
    return { query: '', by, busy: false }
  }

  return { ...shown, by: shown?.by ?? by, busy: shown?.query !== query || shown?.by !== by }
}

async function search(query: string, by: SearchBy, signal: AbortSignal): Promise<MovieListItem[]> {
  const parameters = `query=${encodeURIComponent(query)}&limit=${resultLimit}`

  if (by === 'title') {
    return getJson<TitleSuggestion[]>(`/api/movies/title-suggestions?${parameters}`, signal)
  }

  const movies = await getJson<Movie[]>(`/api/movies/semantic-search?${parameters}`, signal)

  return movies.map(listItem)
}

async function getJson<T>(url: string, signal: AbortSignal): Promise<T> {
  const response = await fetch(url, { signal })

  if (!response.ok) {
    throw new Error(
      response.status === 404
        ? 'Not found.'
        : response.status === 429
          ? 'Too many requests. Wait a moment and try again.'
          : response.status === 503
            ? 'Unavailable until the movies are embedded.'
            : `The API returned an error (${response.status}).`,
    )
  }

  return (await response.json()) as T
}

function describe(error: unknown) {
  if (error instanceof TypeError) {
    return "Couldn't reach the API."
  }

  return error instanceof Error ? error.message : String(error)
}
