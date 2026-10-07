using System.Net;
using System.Net.Http.Json;

namespace OptixMovies.IntegrationTests;

/// <summary>How the API behaves when the movies' embeddings can't be used.</summary>
public sealed class EmbeddingStateTests(TestData data)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SemanticSearchAndSimilarMoviesAreUnavailableUntilTheMoviesAreEmbedded()
    {
        await using var database = await TestDatabase.CreateAsync(data.Movies);
        await using var api = new ApiFactory(database);
        var client = api.CreateClient();

        var search = await client.GetAsync("/api/movies/semantic-search?query=toys", Cancellation);
        var similar = await client.GetAsync("/api/movies/1/similar", Cancellation);
        var problem = await search.Content.ReadFromJsonAsync<Problem>(Cancellation);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, search.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, similar.StatusCode);
        Assert.Equal("application/problem+json", search.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Semantic search is unavailable until the movies are embedded", problem!.Title);
    }

    [Fact]
    public async Task StartupWarnsWhenTheMoviesNeedEmbedding()
    {
        await using var database = await TestDatabase.CreateAsync(data.Movies);
        await using var api = new ApiFactory(database);

        api.CreateClient();

        Assert.Contains(api.Logs.Warnings, warning => warning.Contains("The movies need embedding"));
    }

    [Fact]
    public async Task StartupWarnsWhenAnotherModelEmbeddedTheMovies()
    {
        await using var database = await TestDatabase.CreateAsync(data.Movies, new OtherModelEmbedder());
        await using var api = new ApiFactory(database);

        api.CreateClient();

        Assert.Contains(
            api.Logs.Warnings,
            warning => warning.Contains("embedded with other-model (v1), but the API uses bge-small-en-v1.5 (int8)"));
    }

    private sealed record Problem(string Title);
}
