import { useEffect, useState, type FormEvent } from 'react'
import { useTitleSuggestions } from './api.ts'
import { movieHref, searchHref } from './routes.ts'

/** How long typing must pause before suggestions load. */
const typingPause = 300

export function SearchPage({ query: initialQuery }: { query: string }) {
  const [text, setText] = useState(initialQuery)
  // Search without waiting when the page opens with a query, such as after Back, and on Enter or the Search button.
  const [searchNow, setSearchNow] = useState(initialQuery !== '')
  const { query, titles, error, busy } = useTitleSuggestions(text, searchNow ? 0 : typingPause)

  // Keep the address in step with the results on show, so Back from a movie returns to them, without adding history.
  useEffect(() => {
    if (query !== undefined) {
      window.history.replaceState(null, '', query ? searchHref(query) : '#/')
    }
  }, [query])

  function search(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSearchNow(true)
  }

  return (
    <>
      <form role="search" onSubmit={search}>
        <input
          type="search"
          aria-label="Movie title"
          placeholder="Search by title"
          spellCheck
          value={text}
          onChange={(event) => {
            setText(event.target.value)
            setSearchNow(false)
          }}
          autoFocus
        />
        <button type="submit" aria-busy={busy}>
          Search
        </button>
      </form>

      {!busy && error && <p>{error}</p>}
      {!busy && titles?.length === 0 && <p>No titles match “{query}”.</p>}
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
