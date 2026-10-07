using System.Diagnostics;

using Microsoft.Data.Sqlite;

using OptixMovies.Data.Sqlite;
using OptixMovies.Embeddings.Onnx;

var force = args.Contains("--force");
string[] paths = [.. args.Where(arg => arg != "--force")];

if (paths is not [var databasePath])
{
    Console.Error.WriteLine("Usage: OptixMovies.Embedder <database file> [--force]");
    return 2;
}

if (!File.Exists(databasePath))
{
    Console.Error.WriteLine($"No movie database at {databasePath}. Run the importer first.");
    return 1;
}

try
{
    using var embedder = new OnnxTextEmbedder();
    await using var connection = new SqliteConnection(
        new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWrite }.ToString());
    var stopwatch = Stopwatch.StartNew();
    var count = await MovieDatabase.EmbedAsync(
        connection, embedder, force, new ConsoleProgress($"Embedding movies with {embedder.Model} ({embedder.Variant})"));

    Console.WriteLine(count is null
        ? $"The embeddings in {databasePath} are up to date. Run with --force to rebuild them."
        : $"Embedded {count:N0} movies into {databasePath} in {stopwatch.Elapsed.TotalSeconds:N0} s.");
    return 0;
}
catch (FileNotFoundException e)
{
    Console.Error.WriteLine($"Embedding failed: {e.Message}");
    return 1;
}

/// <summary>
/// Shows progress on one console line. <see cref="Progress{T}"/> would report on the thread pool, so its last update
/// could appear after the final message.
/// </summary>
internal sealed class ConsoleProgress(string task) : IProgress<double>
{
    public void Report(double value)
    {
        Console.Write($"\r{task}: {value:P0}");

        if (value >= 1)
        {
            Console.WriteLine();
        }
    }
}
