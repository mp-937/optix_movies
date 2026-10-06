import { MoviePage } from './MoviePage.tsx'
import { useRoute } from './routes.ts'
import { SearchPage } from './SearchPage.tsx'

function App() {
  const route = useRoute()

  return (
    <>
      <header className="container">
        <nav>
          <ul>
            <li>
              <a href="#/" className="contrast">
                <strong>OptixMovies</strong>
              </a>
            </li>
          </ul>
        </nav>
      </header>
      <main className="container">
        {route.page === 'movie' ? (
          <MoviePage key={route.id} id={route.id} />
        ) : (
          <SearchPage key={route.query} query={route.query} />
        )}
      </main>
    </>
  )
}

export default App
