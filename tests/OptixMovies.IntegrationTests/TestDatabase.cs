using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using OptixMovies.Core;
using OptixMovies.Data.Sqlite;

namespace OptixMovies.IntegrationTests;

/// <summary>A movie database in a temporary file, deleted when disposed.</summary>
public sealed class TestDatabase : IAsyncDisposable
{
    private TestDatabase() =>
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"optixmovies-tests-{Guid.NewGuid():N}.db");

    public string Path { get; }

    /// <summary>How the API connects: read-only.</summary>
    public string ReadOnlyConnectionString => $"Data Source={Path};Mode=ReadOnly";

    /// <summary>Imports <paramref name="movies"/>, as the importer does, then embeds them if given an embedder.</summary>
    public static async Task<TestDatabase> CreateAsync(IReadOnlyList<Movie> movies, ITextEmbedder? embedder = null)
    {
        var database = new TestDatabase();
        await using var connection = database.Connect();
        await MovieDatabase.CreateAsync(connection, movies);

        if (embedder is not null)
        {
            await MovieDatabase.EmbedAsync(connection, embedder);
        }

        return database;
    }

    /// <summary>A read-write connection, as the importer and embedder use.</summary>
    public SqliteConnection Connect() => new($"Data Source={Path}");

    /// <summary>Data.Sqlite's services, registered as the API registers them.</summary>
    public ServiceProvider Services() =>
        new ServiceCollection().AddSqliteData(ReadOnlyConnectionString).BuildServiceProvider();

    public ValueTask DisposeAsync()
    {
        // Windows can't delete a file that pooled connections still hold open.
        SqliteConnection.ClearAllPools();
        File.Delete(Path);
        return ValueTask.CompletedTask;
    }
}
