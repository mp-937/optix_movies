using System.Net;
using System.Net.Http.Json;

using Microsoft.Extensions.DependencyInjection;

using OptixMovies.Core;

namespace OptixMovies.IntegrationTests;

/// <summary>Every error is a problem details response; only errors the API checks for say what went wrong.</summary>
public sealed class ErrorHandlingTests(TestData data)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("/api/movies/999999")]
    [InlineData("/api/nothing-here")]
    public async Task NotFoundIsAProblem(string url)
    {
        var (status, problem) = await GetProblemAsync(data.Api.CreateClient(), url);

        Assert.Equal(HttpStatusCode.NotFound, status);
        Assert.Equal(404, problem.Status);
    }

    [Theory]
    [InlineData("/api/movies/title-suggestions", "query")]
    [InlineData("/api/movies?sortBy=Rating", "sortBy")]
    [InlineData("/api/movies?page=two", "page")]
    public async Task MissingOrMalformedParameterSaysWhich(string url, string parameter)
    {
        var (status, problem) = await GetProblemAsync(data.Api.CreateClient(), url);

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Contains(parameter, problem.Detail);
    }

    [Fact]
    public async Task UnexpectedErrorIsGenericToClientsButLoggedInFull()
    {
        await using var api = WithVectorSearch(new FailingVectorSearch());

        var (status, problem) = await GetProblemAsync(api.CreateClient(), "/api/movies/semantic-search?query=toys");

        Assert.Equal(HttpStatusCode.InternalServerError, status);
        Assert.Equal("An error occurred while processing your request.", problem.Title);
        Assert.DoesNotContain(FailingVectorSearch.Secret, problem.Detail ?? "");
        Assert.Contains(api.Logs.LoggedErrors, exception => exception?.Message == FailingVectorSearch.Secret);
    }

    [Fact]
    public async Task RequestThatTakesTooLongTimesOut()
    {
        await using var api = WithVectorSearch(new HangingVectorSearch(), ("RequestTimeout", "00:00:00.2"));

        var (status, problem) = await GetProblemAsync(api.CreateClient(), "/api/movies/semantic-search?query=toys");

        Assert.Equal(HttpStatusCode.GatewayTimeout, status);
        Assert.Equal(504, problem.Status);
    }

    private ApiFactory WithVectorSearch(IVectorSearch vectors, params (string Key, string Value)[] settings)
    {
        var allSettings = settings.ToDictionary(setting => setting.Key, setting => setting.Value);
        allSettings["WarmUpOnStartup"] = "false"; // It would run the replaced search before the API starts.

        return new ApiFactory(data.Database, allSettings, services => services.AddScoped(_ => vectors));
    }

    private static async Task<(HttpStatusCode Status, Problem Problem)> GetProblemAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url, Cancellation);

        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return (response.StatusCode, (await response.Content.ReadFromJsonAsync<Problem>(Cancellation))!);
    }

    private sealed record Problem(int Status, string Title, string? Detail);

    private sealed class FailingVectorSearch : IVectorSearch
    {
        public const string Secret = "Internal details clients mustn't see";

        public Task<IReadOnlyList<Movie>?> FindNearestAsync(VectorQuery query, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(Secret);

        public Task<IReadOnlyList<Movie>?> FindSimilarAsync(SimilarQuery query, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(Secret);
    }

    private sealed class HangingVectorSearch : IVectorSearch
    {
        public async Task<IReadOnlyList<Movie>?> FindNearestAsync(
            VectorQuery query, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return null;
        }

        public Task<IReadOnlyList<Movie>?> FindSimilarAsync(SimilarQuery query, CancellationToken cancellationToken) =>
            FindNearestAsync(null!, cancellationToken);
    }
}
