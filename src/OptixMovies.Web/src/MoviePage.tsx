import { useState, type MouseEvent, type ReactNode } from 'react'
import { useApi, type Movie } from './api.ts'

const showPosters = import.meta.env.VITE_SHOW_POSTERS !== 'false'
const languages = new Intl.DisplayNames(['en-GB'], { type: 'language' })

export function MoviePage({ id }: { id: number }) {
  const { data: movie, error, loading } = useApi<Movie>(`/api/movies/${id}`)

  return (
    <>
      <p>
        <a href="#/" onClick={goBack}>
          ← Back
        </a>
      </p>

      {loading && <p aria-busy="true">Loading…</p>}
      {error && <p>{error}</p>}
      {movie && <MovieDetails movie={movie} />}
    </>
  )
}

function MovieDetails({ movie }: { movie: Movie }) {
  const [posterFailed, setPosterFailed] = useState(false)
  const hasPoster = showPosters && !posterFailed

  return (
    <article className={hasPoster ? 'movie with-poster' : 'movie'}>
      {hasPoster && (
        <img
          src={smallPoster(movie.posterUrl)}
          alt={`Poster for ${movie.title}`}
          width={342}
          height={513}
          onError={() => setPosterFailed(true)}
        />
      )}
      <div>
        <h1>{movie.title}</h1>
        <table>
          <tbody>
            <Row label="Released">{formatDate(movie.releaseDate)}</Row>
            <Row label="Genres">{movie.genres.join(', ')}</Row>
            <Row label="Overview">{movie.overview}</Row>
            <Row label="Rating">
              {movie.voteAverage.toFixed(1)} / 10 from {movie.voteCount.toLocaleString('en-GB')} votes
            </Row>
            <Row label="Popularity">{movie.popularity.toLocaleString('en-GB')}</Row>
            <Row label="Original language">{languageName(movie.originalLanguage)}</Row>
          </tbody>
        </table>
      </div>
    </article>
  )
}

function Row({ label, children }: { label: string; children: ReactNode }) {
  return (
    <tr>
      <th scope="row">{label}</th>
      <td>{children}</td>
    </tr>
  )
}

// TMDB serves each poster in several sizes, and the original is often several megabytes.
function smallPoster(url: string) {
  return url.replace('/t/p/original/', '/t/p/w342/')
}

function formatDate(isoDate: string) {
  return new Date(isoDate).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' })
}

function languageName(code: string) {
  try {
    return languages.of(code) ?? code
  } catch {
    return code
  }
}

// Back to the results the user came from. With no history, the link's own target, the search page, applies.
function goBack(event: MouseEvent<HTMLAnchorElement>) {
  if (window.history.length > 1) {
    event.preventDefault()
    window.history.back()
  }
}
