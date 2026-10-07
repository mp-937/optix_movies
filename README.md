# OptixMovies

A .NET 10 Web API, with a small React UI, for searching the Kaggle "9000+ Movies" dataset. Built as the Optix technical test.

## Running it

Requires .NET 10 SDK, and Node.js 22.12 or later for the UI. The batch files are for Windows; the commands work anywhere, run from the repository root.

1. **Build the database** from the CSV: `import.bat`, or `dotnet run --project src/OptixMovies.Importer -- data/mymoviedb.csv data/movies.db`.
2. **Embed the movies** for semantic search, which takes a minute or two: `import.bat` does this after building the database, `embed.bat` does it alone, or `dotnet run --project src/OptixMovies.Embedder -- data/movies.db`. Without embeddings, the API starts with a warning, and semantic search returns 503 until the movies are embedded.
3. **Start the API**: `start-api.bat`, or `dotnet run --project src/OptixMovies.Api`. Swagger is at http://localhost:5246/swagger in development.
4. **Start the UI**: `start-web.bat`, or `npm install` then `npm run dev` in `src/OptixMovies.Web`. It runs at http://127.0.0.1:5173 and needs the API running.

## Tests

`dotnet test`, from the repository root, runs both test projects in a few seconds. They don't need `data/movies.db`.

- `tests/OptixMovies.UnitTests` covers Core's logic and the CSV reader, with fakes: no files, network or model.
- `tests/OptixMovies.IntegrationTests` covers the real embedding model, SQLite with sqlite-vec, and the API in memory. Each run imports and embeds 18 movies from the dataset into a temporary database.
- One test checks that TMDB's poster CDN responds, so it needs the internet; `dotnet test --filter-not-trait "Category=External"` skips it.

`global.json` tells the .NET 10 SDK to run tests with Microsoft Testing Platform, which xUnit v3 uses.

## API

| Endpoint | Purpose |
| --- | --- |
| `GET /api/movies/title-suggestions?query=dark kni&limit=8` | Search as you type: matching titles with ids and years |
| `GET /api/movies/semantic-search?query=a kid befriends an alien&limit=10` | Search by meaning: the movies whose title, genres and overview best match a description |
| `GET /api/movies?genre=Drama&sortBy=ReleaseDate&sortDirection=Desc&page=1&pageSize=20` | Browse, filter by genre, sort and page |
| `GET /api/movies/{id}` | One movie |
| `GET /api/movies/{id}/similar?limit=10` | The movies most like this one, leaving out any that aren't alike enough |
| `GET /api/genres` | Every genre |

## Projects

- `src/OptixMovies.Api` — the ASP.NET Core API
- `src/OptixMovies.Core` — business logic and the interfaces it needs
- `src/OptixMovies.Data.Sqlite` — the SQLite implementation of those interfaces
- `src/OptixMovies.Embeddings.Onnx` — the embedding model, run in-process
- `src/OptixMovies.Importer` — builds the SQLite database from the CSV
- `src/OptixMovies.Embedder` — embeds the movies in the database for semantic search
- `src/OptixMovies.Web` — the React UI

## Notes and caveats

- **The UI lives in this repository for consistency.** In a real product it would usually have its own repository: it is deployed, versioned and scaled separately from the API, often by a different team, with its own toolchain and pipeline.
- **The UI's API types are generated, which is more than three types need.** `npm run types` generates them from the API's OpenAPI document. Writing them by hand would be lighter here, but generation keeps the UI in step as the API grows.
- **There are no actors or directors.** The dataset has no cast or crew information; either would need an external source such as TMDB's API.
- **Title search doesn't tolerate typos.** Each word typed must start a word in the title, ignoring case, accents and punctuation. Suggestions appear as you type, and the browser's spell checker underlines misspelt words, which covers most of what typo tolerance would add without Elasticsearch or an IMDb API integration; only titles misspelt on purpose miss out. The search sits behind `ITitleSearch`, so a search engine could replace SQLite later without touching the rest.
- **Abandoned requests stop their queries.** Each keystroke cancels the previous suggestion request, and the cancellation reaches SQLite, which would otherwise finish a query nobody is waiting for.
- **Sort values are case-sensitive**, so it's `sortBy=ReleaseDate`, not `releaseDate`, because that is how ASP.NET Core binds enums.
- **There's no Docker setup.** For one API and a SQLite file, containers would be overkill.
- **Rate limiting is basic.** Each browser session, identified by a signed cookie, may make 10 requests a second with bursts of 20, and each IP address may start 20 sessions a minute, so clearing or forging cookies doesn't escape the limit. People behind one IP address, such as a company network, share that allowance for new sessions. At this scale there isn't much more to do; in production, a proxy with bot protection, such as Cloudflare, would be the next step. The limits are in `appsettings.json`.
- **Semantic search uses bge-small-en-v1.5**, a compact embedding model (34 MB, 8-bit) that lives in the repository and runs in-process. It can easily be upgraded, for example to nomic-embed-text-v1.5: the change stays inside the embeddings project, apart from retuning the similar-movies threshold below, and `embed.bat` then re-embeds the movies.
- **Similar movies leave out weak matches.** A movie page lists up to 10 movies whose embeddings have a cosine similarity of at least 0.7 with its own; below that, pairs are mostly unrelated. A typical movie gets about three, and some, such as Groundhog Day, get none. The threshold suits this model and is in `appsettings.json`.
- **Still to do:** other hardening, such as consistent error responses, security headers and health checks.
- **`dotnet format`** warns that it skips the UI's `.esproj`; that's expected.
