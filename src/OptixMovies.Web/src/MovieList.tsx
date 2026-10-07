import type { MovieListItem } from './api.ts'
import { movieHref } from './routes.ts'

/** Links to movies, with years to tell remakes apart. Search results and similar movies both use it. */
export function MovieList({ movies }: { movies: MovieListItem[] }) {
  return (
    <ul>
      {movies.map((movie) => (
        <li key={movie.id}>
          <a href={movieHref(movie.id)}>{movie.title}</a> <small>({movie.year})</small>
        </li>
      ))}
    </ul>
  )
}
