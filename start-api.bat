@echo off
rem Starts the API on http://localhost:5246, with Swagger at /swagger. Build the database first with import.bat.
dotnet run --project "%~dp0src\OptixMovies.Api"
