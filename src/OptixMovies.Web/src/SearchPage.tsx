import { useEffect, useRef, useState, type FormEvent } from 'react'
import { useSearch } from './api.ts'
import { MovieList } from './MovieList.tsx'
import { searchHref, type SearchBy } from './routes.ts'

/** How long typing must pause before results load. */
const typingPause = 300

// In the order the radio buttons show them, the default first.
const modes: Record<SearchBy, { label: string; field: string; placeholder: string }> = {
  description: {
    label: 'Search by description',
    field: 'Movie description',
    placeholder: 'Astronaut stranded alone on Mars tries to survive while NASA works to bring him home',
  },
  title: { label: 'Search by title', field: 'Movie title', placeholder: 'Search by title' },
}

export function SearchPage({ query: initialQuery, by: initialBy }: { query: string; by: SearchBy }) {
  const [text, setText] = useState(initialQuery)
  const [by, setBy] = useState(initialBy)
  // Search without waiting when the page opens with a query, such as after Back, on Enter or the Search button, and
  // when the search mode changes.
  const [searchNow, setSearchNow] = useState(initialQuery !== '')
  const { query, by: shownBy, movies, error, busy } = useSearch(text, by, searchNow ? 0 : typingPause)
  const input = useRef<HTMLInputElement>(null)

  // Keep the address in step with the results on show, so Back from a movie returns to them, without adding history.
  useEffect(() => {
    if (query !== undefined) {
      window.history.replaceState(null, '', searchHref(query, shownBy))
    }
  }, [query, shownBy])

  function search(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSearchNow(true)
  }

  function switchTo(mode: SearchBy) {
    setBy(mode)
    setSearchNow(true)
    input.current?.focus()
  }

  return (
    <>
      <fieldset className="search-by">
        {(Object.keys(modes) as SearchBy[]).map((mode) => (
          <label key={mode}>
            <input type="radio" name="by" checked={by === mode} onChange={() => switchTo(mode)} />
            {modes[mode].label}
          </label>
        ))}
      </fieldset>

      <form role="search" onSubmit={search}>
        <input
          ref={input}
          type="search"
          aria-label={modes[by].field}
          placeholder={modes[by].placeholder}
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
      {!busy && movies?.length === 0 && (
        <p>
          No {shownBy === 'title' ? 'titles' : 'movies'} match “{query}”.
        </p>
      )}
      {movies && movies.length > 0 && <MovieList movies={movies} />}
    </>
  )
}
