@echo off
rem Starts the UI on http://localhost:5173. Start the API first; npm packages are installed on the first run.
cd /d "%~dp0OptixMovies.Web"
if not exist node_modules call npm install
npm run dev
