import { useEffect, useState } from 'react'
import type { components } from './api-schema'

export type Movie = components['schemas']['MovieResponse']
export type TitleSuggestion = components['schemas']['TitleSuggestionResponse']

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

async function getJson<T>(url: string, signal: AbortSignal): Promise<T> {
  const response = await fetch(url, { signal })

  if (!response.ok) {
    throw new Error(response.status === 404 ? 'Not found.' : `The API returned an error (${response.status}).`)
  }

  return (await response.json()) as T
}

function describe(error: unknown) {
  if (error instanceof TypeError) {
    return "Couldn't reach the API."
  }

  return error instanceof Error ? error.message : String(error)
}
