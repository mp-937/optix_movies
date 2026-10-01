@echo off
rem Builds data\movies.db from data\mymoviedb.csv. Pass --force to rebuild an existing database.
dotnet run --project "%~dp0src\OptixMovies.Importer" -- "%~dp0data\mymoviedb.csv" "%~dp0data\movies.db" %*
