using System.Net;

namespace OptixMovies.IntegrationTests;

/// <summary>The health endpoint, for load balancers and monitoring.</summary>
public sealed class HealthTests(TestData data)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task EmbeddedMoviesAreHealthy()
    {
        var response = await data.Api.CreateClient().GetAsync("/health", Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(Cancellation));
    }

    [Fact]
    public async Task MoviesThatNeedEmbeddingAreDegraded()
    {
        await using var database = await TestDatabase.CreateAsync(data.Movies);
        await using var api = new ApiFactory(database);

        var response = await api.CreateClient().GetAsync("/health", Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Degraded", await response.Content.ReadAsStringAsync(Cancellation));
    }

    [Fact]
    public async Task HealthChecksAreNotRateLimitedOrGivenSessions()
    {
        await using var api = new ApiFactory(
            data.Database, new Dictionary<string, string> { ["RateLimiting:Burst"] = "1", ["RateLimiting:RequestsPerSecond"] = "1" });
        var client = api.CreateClient();

        var responses = new List<HttpResponseMessage>();

        for (var i = 0; i < 3; i++)
        {
            responses.Add(await client.GetAsync("/health", Cancellation));
        }

        Assert.All(responses, response =>
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(response.Headers.Contains("Set-Cookie"));
        });
    }
}
