@echo off
rem Runs the unit and integration tests. One test needs the internet; skip it with --filter-not-trait "Category=External".
rem Runs from the repository root, where global.json tells dotnet test to use Microsoft Testing Platform.
cd /d "%~dp0"
dotnet test %*
