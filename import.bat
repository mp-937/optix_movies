@echo off
rem Builds data\movies.db from data\mymoviedb.csv, then embeds the movies for semantic search.
rem Pass --force to rebuild an existing database.
dotnet run --project "%~dp0src\OptixMovies.Importer" -- "%~dp0data\mymoviedb.csv" "%~dp0data\movies.db" %*
if errorlevel 1 exit /b %errorlevel%
call "%~dp0embed.bat"
if errorlevel 1 (
    echo The movies are imported, but semantic search is unavailable until embed.bat succeeds.
    exit /b 1
)
