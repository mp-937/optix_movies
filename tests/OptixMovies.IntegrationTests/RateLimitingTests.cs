using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;

namespace OptixMovies.IntegrationTests;

/// <summary>Rate limiting by session cookie, with a burst of 2 and 1 more request a second.</summary>
public sealed class RateLimitingTests(TestData data) : IAsyncDisposable
{
    private readonly ApiFactory api = new(
        data.Database, new Dictionary<string, string> { ["RateLimiting:Burst"] = "2", ["RateLimiting:RequestsPerSecond"] = "1" });

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task FirstRequestStartsASessionWithAnHttpOnlyCookie()
    {
        var response = await api.CreateClient().GetAsync("/api/genres", Cancellation);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("session=", cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GoingOverTheBurstIsRejectedWithRetryAfter()
    {
        var client = api.CreateClient();

        HttpResponseMessage response = null!;

        for (var i = 0; i < 3; i++)
        {
            response = await client.GetAsync("/api/genres", Cancellation);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter);
    }

    [Fact]
    public async Task ForgedCookieGetsAFreshSession()
    {
        var client = api.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Add("Cookie", "session=forged");

        var response = await client.GetAsync("/api/genres", Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("session=", Assert.Single(response.Headers.GetValues("Set-Cookie")));
    }

    public ValueTask DisposeAsync() => api.DisposeAsync();
}
