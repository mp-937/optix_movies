using Microsoft.Data.Sqlite;

using OptixMovies.Core;
using OptixMovies.Data.Sqlite;
using OptixMovies.Importer;

var force = args.Contains("--force");
string[] paths = [.. args.Where(arg => arg != "--force")];

if (paths is not [var csvPath, var databasePath])
{
    Console.Error.WriteLine("Usage: OptixMovies.Importer <csv file> <database file> [--force]");
    return 2;
}

if (File.Exists(databasePath) && !force)
{
    Console.WriteLine($"{databasePath} already exists. Run with --force to rebuild it.");
    return 0;
}

IReadOnlyList<Movie> movies;

try
{
    using var reader = File.OpenText(csvPath);
    movies = MovieCsv.Read(reader);
}
catch (Exception e) when (e is InvalidDataException or IOException)
{
    Console.Error.WriteLine($"Import failed: {e.Message}");
    return 1;
}

File.Delete(databasePath);
new FileInfo(databasePath).Directory?.Create();

try
{
    await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString());
    await MovieDatabase.CreateAsync(connection, movies);
}
catch
{
    // Never leave a half-built database behind: the next run would treat it as done.
    SqliteConnection.ClearAllPools();
    File.Delete(databasePath);
    throw;
}

Console.WriteLine($"Imported {movies.Count:N0} movies into {databasePath}.");
return 0;
