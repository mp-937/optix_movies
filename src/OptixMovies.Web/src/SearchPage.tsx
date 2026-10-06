import { useState, type FormEvent } from 'react'
import { useApi, type TitleSuggestion } from './api.ts'
import { movieHref, searchHref } from './routes.ts'

const resultLimit = 20

export function SearchPage({ query }: { query: string }) {
  const [text, setText] = useState(query)
  const { data: titles, error, loading } = useApi<TitleSuggestion[]>(
    query ? `/api/movies/title-suggestions?query=${encodeURIComponent(query)}&limit=${resultLimit}` : null,
  )

  function search(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (text.trim()) {
      window.location.hash = searchHref(text.trim())
    }
  }

  return (
    <>
      <form role="search" onSubmit={search}>
        <input
          type="search"
          aria-label="Movie title"
          placeholder="Search by title"
          value={text}
          onChange={(event) => setText(event.target.value)}
          autoFocus
        />
        <button type="submit">Search</button>
      </form>

      {loading && <p aria-busy="true">Searching…</p>}
      {error && <p>{error}</p>}
      {titles?.length === 0 && <p>No titles match “{query}”.</p>}
      {titles && titles.length > 0 && (
        <ul>
          {titles.map((title) => (
            <li key={title.id}>
              <a href={movieHref(title.id)}>{title.title}</a> <small>({title.year})</small>
            </li>
          ))}
        </ul>
      )}
    </>
  )
}
