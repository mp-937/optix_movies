@echo off
rem Embeds the movies in data\movies.db for semantic search, if they or the model have changed since they were last
rem embedded. Pass --force to embed them anyway.
dotnet run --project "%~dp0src\OptixMovies.Embedder" -- "%~dp0data\movies.db" %*
