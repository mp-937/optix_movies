namespace OptixMovies.IntegrationTests;

/// <summary>API responses carry security headers; Swagger UI, a development tool, doesn't.</summary>
public sealed class SecurityHeaderTests(TestData data)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("/api/genres")] // A success.
    [InlineData("/api/movies/999999")] // An error, which clears the headers set before it.
    public async Task ApiResponsesCarrySecurityHeaders(string url)
    {
        var response = await data.Api.CreateClient().GetAsync(url, Cancellation);

        Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
        Assert.Equal("default-src 'none'; frame-ancestors 'none'", Header(response, "Content-Security-Policy"));
        Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
        Assert.False(response.Headers.Contains("Server"));
    }

    [Fact]
    public async Task SwaggerUiIsLeftAlone()
    {
        var response = await data.Api.CreateClient().GetAsync("/swagger/index.html", Cancellation);

        response.EnsureSuccessStatusCode();
        Assert.False(response.Headers.Contains("Content-Security-Policy"));
    }

    private static string? Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? string.Join(", ", values) : null;
}
