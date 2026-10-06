# OptixMovies

A .NET 10 Web API, with a small React UI, for searching the Kaggle "9000+ Movies" dataset. Built as the Optix technical test.

## Running it

You need the .NET 10 SDK, and Node.js 22.12 or later for the UI. The batch files are for Windows; the commands work anywhere, run from the repository root.

1. **Build the database** from the CSV: `import.bat`, or `dotnet run --project src/OptixMovies.Importer -- data/mymoviedb.csv data/movies.db`. It only runs when `data/movies.db` is missing; add `--force` to rebuild it.
2. **Start the API**: `src\start_api.bat`, or `dotnet run --project src/OptixMovies.Api`. Swagger is at http://localhost:5246/swagger in development.
3. **Start the UI**: `src\start_web.bat`, or `npm install` then `npm run dev` in `src/OptixMovies.Web`. It runs at http://localhost:5173 and needs the API running.

## API

| Endpoint | Purpose |
| --- | --- |
| `GET /api/movies/title-suggestions?query=dark kni&limit=8` | Search as you type: matching titles with ids and years |
| `GET /api/movies?genre=Drama&sortBy=ReleaseDate&sortDirection=Desc&page=1&pageSize=20` | Browse, filter by genre, sort and page |
| `GET /api/movies/{id}` | One movie |
| `GET /api/genres` | Every genre |

## Projects

- `src/OptixMovies.Api` — the ASP.NET Core API
- `src/OptixMovies.Core` — business logic and the interfaces it needs
- `src/OptixMovies.Data.Sqlite` — the SQLite implementation of those interfaces
- `src/OptixMovies.Importer` — builds the SQLite database from the CSV
- `src/OptixMovies.Web` — the React UI

Each design decision, with the alternatives considered and the reasons, is recorded in [docs/decisions.md](docs/decisions.md).

## Notes and caveats

- **The UI lives in this repository for consistency.** In a real product it would usually have its own repository: it is deployed, versioned and scaled separately from the API, often by a different team, with its own toolchain and pipeline.
- **The UI's API types are generated, which is more than three types need.** `npm run types` generates them from the API's OpenAPI document. Writing them by hand would be lighter here, but generation keeps the UI in step as the API grows.
- **There are no actors or directors.** The dataset has no cast or crew information; either would need an external source such as TMDB's API.
- **Title search doesn't tolerate typos.** Each word typed must start a word in the title, ignoring case, accents and punctuation, and users see suggestions as they type. The search sits behind `ITitleSearch`, so a search engine such as Elasticsearch could replace SQLite without touching the rest.
- **The database is rebuilt, not migrated.** After a schema change, run the importer with `--force`, with the API stopped: Windows won't replace a database that's open.
- **Sort values are case-sensitive**, so it's `sortBy=ReleaseDate`, not `releaseDate`, because that is how ASP.NET Core binds enums.
- **Still to do:** rate limiting and other hardening, such as consistent error responses, security headers and health checks; unit and integration tests; semantic search over the overviews; and Docker, which the Windows 10 LTSC machine this was built on can't run through Docker Desktop.
- **Visual Studio:** .NET 10 needs Visual Studio 2026, because Visual Studio 2022 can't target it. The individual components ".NET SDK" and "Development tools for .NET" are enough for the API; the "ASP.NET and web development" workload also installs .NET Framework, IIS Express and LocalDB, none of which this uses. Opening the UI's `.esproj` needs Visual Studio's JavaScript project support.
- **Local HTTPS:** `dotnet run` serves plain HTTP on port 5246, so `https://localhost:5246` fails with `SSL_ERROR_RX_RECORD_TOO_LONG`. Visual Studio's default `https` profile adds `https://localhost:7228` but also uses port 5246, so only one copy of the API can run at a time. `dotnet dev-certs https --trust` stops the browser's certificate warning.
- **`dotnet format`** warns that it skips the UI's `.esproj`; that's expected.
