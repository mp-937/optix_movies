import { useEffect, useState } from 'react'
import type { components } from './api-schema'

export type Movie = components['schemas']['MovieResponse']
export type TitleSuggestion = components['schemas']['TitleSuggestionResponse']

const suggestionLimit = 20

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

type Suggestions = { query: string; titles?: TitleSuggestion[]; error?: string }

/** `query` is what the suggestions on show were for: undefined until the first ones arrive, empty for no search. */
type SuggestionState = { query?: string; titles?: TitleSuggestion[]; error?: string; busy: boolean }

/**
 * Title suggestions for what the user is typing, fetched once typing pauses for `delay` milliseconds. Every keystroke
 * cancels the pending wait and any request still in flight. The last suggestions stay on show until new ones arrive,
 * and `busy` is true from the first keystroke.
 */
export function useTitleSuggestions(text: string, delay: number): SuggestionState {
  const query = text.trim()
  const [shown, setShown] = useState<Suggestions>()

  useEffect(() => {
    if (!query) {
      return
    }

    const request = new AbortController()
    const url = `/api/movies/title-suggestions?query=${encodeURIComponent(query)}&limit=${suggestionLimit}`

    const wait = setTimeout(() => {
      getJson<TitleSuggestion[]>(url, request.signal).then(
        (titles) => setShown({ query, titles }),
        (error: unknown) => {
          if (!request.signal.aborted) {
            // Keep the previous suggestions on screen, with the error above them.
            setShown((previous) => ({ query, titles: previous?.titles, error: describe(error) }))
          }
        },
      )
    }, delay)

    return () => {
      clearTimeout(wait)
      request.abort()
    }
  }, [query, delay])

  if (!query) {
    return { query: '', busy: false }
  }

  return { ...shown, busy: shown?.query !== query }
}

async function getJson<T>(url: string, signal: AbortSignal): Promise<T> {
  const response = await fetch(url, { signal })

  if (!response.ok) {
    throw new Error(
      response.status === 404
        ? 'Not found.'
        : response.status === 429
          ? 'Too many requests. Wait a moment and try again.'
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
