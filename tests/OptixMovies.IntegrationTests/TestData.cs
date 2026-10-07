using OptixMovies.Core;
using OptixMovies.Embeddings.Onnx;
using OptixMovies.Importer;

[assembly: AssemblyFixture(typeof(OptixMovies.IntegrationTests.TestData))]

namespace OptixMovies.IntegrationTests;

/// <summary>
/// Shared by every test: the embedding model, loaded once; the fixture movies, imported and embedded into a temporary
/// database as <c>import.bat</c> would; and the API serving that database.
/// </summary>
public sealed class TestData : IAsyncLifetime
{
    public OnnxTextEmbedder Embedder { get; } = new();

    /// <summary>18 real rows from the dataset, picked for accents, punctuation, sequels and unrelated movies.</summary>
    public IReadOnlyList<Movie> Movies { get; } = ReadFixtureMovies();

    /// <summary>The fixture movies, imported and embedded.</summary>
    public TestDatabase Database { get; private set; } = null!;

    /// <summary>The API, serving <see cref="Database"/>.</summary>
    public ApiFactory Api { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        Database = await TestDatabase.CreateAsync(Movies, Embedder);
        Api = new ApiFactory(Database);
    }

    public async ValueTask DisposeAsync()
    {
        await Api.DisposeAsync();
        await Database.DisposeAsync();
        Embedder.Dispose();
    }

    private static IReadOnlyList<Movie> ReadFixtureMovies()
    {
        using var reader = File.OpenText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "movies.csv"));
        return MovieCsv.Read(reader);
    }
}
