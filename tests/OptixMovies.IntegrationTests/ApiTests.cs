using System.Net;
using System.Net.Http.Json;

using OptixMovies.Api.Contracts;

namespace OptixMovies.IntegrationTests;

/// <summary>The API's endpoints, serving the embedded fixture movies.</summary>
public sealed class ApiTests(TestData data)
{
    private readonly HttpClient client = data.Api.CreateClient();

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GenresAreListedAToZIgnoringCase()
    {
        var genres = await client.GetFromJsonAsync<List<string>>("/api/genres", Cancellation);

        Assert.Equal(
            data.Movies.SelectMany(movie => movie.Genres).Distinct().Order(StringComparer.OrdinalIgnoreCase),
            genres);
    }

    [Fact]
    public async Task PagesCoverEveryMovieOnce()
    {
        var pages = new List<PageResponse<MovieResponse>>();

        for (var page = 1; page <= 4; page++)
        {
            pages.Add((await client.GetFromJsonAsync<PageResponse<MovieResponse>>(
                $"/api/movies?page={page}&pageSize=5", Cancellation))!);
        }

        Assert.All(pages, page => Assert.Equal((data.Movies.Count, 4), (page.TotalCount, page.TotalPages)));
        Assert.Equal([5, 5, 5, 3], pages.Select(page => page.Items.Count));
        Assert.Equal(data.Movies.Count, pages.SelectMany(page => page.Items).DistinctBy(movie => movie.Id).Count());
    }

    [Fact]
    public async Task GenreFilterIgnoresCaseAndResultsSortByReleaseDate()
    {
        var page = await client.GetFromJsonAsync<PageResponse<MovieResponse>>(
            "/api/movies?genre=drama&sortBy=ReleaseDate&sortDirection=Desc&pageSize=100", Cancellation);

        Assert.Equal(data.Movies.Count(movie => movie.Genres.Contains("Drama")), page!.TotalCount);
        Assert.All(page.Items, movie => Assert.Contains("Drama", movie.Genres));
        Assert.Equal(page.Items.Select(movie => movie.ReleaseDate).OrderDescending(), page.Items.Select(movie => movie.ReleaseDate));
    }

    [Theory]
    [InlineData("/api/movies?pageSize=0", "pageSize")]
    [InlineData("/api/movies?page=0", "page")]
    [InlineData("/api/movies?pageSize=101", "pageSize")]
    public async Task InvalidPagingIsRejectedWithErrorsForEachField(string url, string field)
    {
        var response = await client.GetAsync(url, Cancellation);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Cancellation);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(field, problem!.Errors.Keys);
    }

    [Fact]
    public async Task InvalidSortIsRejected()
    {
        var response = await client.GetAsync("/api/movies?sortBy=Rating", Cancellation);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MovieIsFoundByIdAndAnUnknownIdIsNotFound()
    {
        var toyStory = await FindAsync("Toy Story");

        var movie = await client.GetFromJsonAsync<MovieResponse>($"/api/movies/{toyStory.Id}", Cancellation);
        var unknown = await client.GetAsync("/api/movies/999999", Cancellation);

        Assert.Equal("Toy Story", movie!.Title);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Theory]
    [InlineData("dark kni", "The Dark Knight")]
    [InlineData("leon", "Léon: The Professional")]
    [InlineData("spider man", "Spider-Man: No Way Home")]
    public async Task TitleSuggestionsMatchWordStartsIgnoringAccentsAndPunctuation(string query, string title)
    {
        var suggestions = await client.GetFromJsonAsync<List<TitleSuggestionResponse>>(
            $"/api/movies/title-suggestions?query={Uri.EscapeDataString(query)}", Cancellation);

        Assert.Equal(title, suggestions![0].Title);
    }

    [Fact]
    public async Task TitleSuggestionsRespectTheLimit()
    {
        var suggestions = await client.GetFromJsonAsync<List<TitleSuggestionResponse>>(
            "/api/movies/title-suggestions?query=the&limit=2", Cancellation);

        Assert.Equal(2, suggestions!.Count);
    }

    [Fact]
    public async Task SemanticSearchFindsMoviesByMeaning()
    {
        var movies = await client.GetFromJsonAsync<List<MovieResponse>>(
            "/api/movies/semantic-search?query=toys%20that%20come%20alive&limit=2", Cancellation);

        Assert.Equal(2, movies!.Count);
        Assert.StartsWith("Toy Story", movies[0].Title);
    }

    [Theory]
    [InlineData("%20%20")]
    [InlineData(null)]
    public async Task SemanticSearchRejectsBlankOrOverlongQueries(string? query)
    {
        var response = await client.GetAsync(
            $"/api/movies/semantic-search?query={query ?? new string('a', 201)}", Cancellation);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SimilarMoviesAreSequelsNotTheMovieItselfOrUnrelatedOnes()
    {
        var toyStory = await FindAsync("Toy Story");

        var similar = await client.GetFromJsonAsync<List<MovieResponse>>(
            $"/api/movies/{toyStory.Id}/similar", Cancellation);
        var unknown = await client.GetAsync("/api/movies/999999/similar", Cancellation);

        Assert.Contains(similar!, movie => movie.Title == "Toy Story 2");
        Assert.Contains(similar!, movie => movie.Title == "Toy Story 3");
        Assert.DoesNotContain(similar!, movie => movie.Title is "Toy Story" or "Jaws");
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    private async Task<MovieResponse> FindAsync(string title) =>
        (await client.GetFromJsonAsync<PageResponse<MovieResponse>>("/api/movies?pageSize=100", Cancellation))!
            .Items.Single(movie => movie.Title == title);

    private sealed record ValidationProblem(Dictionary<string, string[]> Errors);
}
