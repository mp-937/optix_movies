using System.Net;

namespace OptixMovies.IntegrationTests;

/// <summary>Needs the internet: skip with <c>--filter-not-trait "Category=External"</c>.</summary>
[Trait("Category", "External")]
public sealed class PosterCdnTests(TestData data)
{
    [Fact]
    public async Task PostersLoadFromTmdbAtTheSizeTheUiAsksFor()
    {
        // The UI asks for TMDB's 342-pixel version rather than the original, which is often several megabytes.
        var poster = data.Movies.Single(movie => movie.Title == "Toy Story").PosterUrl.ToString()
            .Replace("/t/p/original/", "/t/p/w342/", StringComparison.Ordinal);
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        using var response = await client.GetAsync(poster, TestContext.Current.CancellationToken);

        Assert.StartsWith("https://image.tmdb.org/t/p/w342/", poster);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("image/", response.Content.Headers.ContentType?.MediaType);
    }
}
