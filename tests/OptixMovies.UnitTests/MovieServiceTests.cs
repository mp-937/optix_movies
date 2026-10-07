using OptixMovies.Core;

namespace OptixMovies.UnitTests;

public sealed class MovieServiceTests
{
    private readonly FakeMovieRepository movies = new();
    private readonly FakeTitleSearch titles = new();
    private readonly FakeEmbedder embedder = new();
    private readonly FakeVectorSearch vectors = new();

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("?!")]
    public async Task BlankTitleQueryReturnsNothingWithoutSearching(string query)
    {
        var suggestions = await Service().SuggestTitlesAsync(query, limit: 8, Cancellation);

        Assert.Empty(suggestions);
        Assert.Empty(titles.Queries);
    }

    [Fact]
    public async Task IgnoredWordsAreLeftOutOfTheWordsToMatchButKeptForRanking()
    {
        await Service(ignoredWords: "the").SuggestTitlesAsync("The Batman", limit: 8, Cancellation);

        var query = Assert.Single(titles.Queries);
        Assert.Equal("the batman", query.Text);
        Assert.Equal(["batman"], query.Words);
        Assert.Equal(8, query.Limit);
    }

    [Fact]
    public async Task QueryOfOnlyIgnoredWordsStillSearchesForThem()
    {
        await Service(ignoredWords: "the").SuggestTitlesAsync("The", limit: 8, Cancellation);

        Assert.Equal(["the"], Assert.Single(titles.Queries).Words);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    [InlineData("  Drama ", "Drama")]
    public async Task BlankGenreMeansNoFilterAndGenresAreTrimmed(string? genre, string? expected)
    {
        await Service().SearchAsync(
            new MovieQuery(genre, MovieSortBy.Title, SortDirection.Asc, Page: 1, PageSize: 20), Cancellation);

        Assert.Equal(expected, Assert.Single(movies.Queries).Genre);
    }

    [Fact]
    public async Task SemanticSearchEmbedsTheTrimmedTextAsAQuery()
    {
        await Service().SemanticSearchAsync("  toys that come alive  ", limit: 5, Cancellation);

        Assert.Equal(["toys that come alive"], embedder.Queries);
        Assert.Empty(embedder.Documents);
        Assert.Equal(
            new VectorQuery(FakeEmbedder.Vector, "fake-model", "test", Limit: 5),
            Assert.Single(vectors.NearestQueries));
    }

    [Fact]
    public async Task SemanticSearchReturnsNullWhenTheEmbeddingsCantAnswer()
    {
        vectors.Result = null;

        Assert.Null(await Service().SemanticSearchAsync("toys that come alive", limit: 5, Cancellation));
    }

    [Fact]
    public async Task SimilarMoviesUsesTheConfiguredMinimumSimilarity()
    {
        await Service().FindSimilarAsync(id: 7, limit: 10, Cancellation);

        Assert.Equal(new SimilarQuery(7, MinSimilarity: 0.7, Limit: 10), Assert.Single(vectors.SimilarQueries));
    }

    private MovieService Service(params string[] ignoredWords) =>
        new(movies, titles, new TitleSuggestionSettings(ignoredWords), embedder, vectors, new SimilarMovieSettings(0.7));
}
