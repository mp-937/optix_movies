using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using OptixMovies.Core;
using OptixMovies.Data.Sqlite;

namespace OptixMovies.IntegrationTests;

/// <summary>The SQLite database, as the importer and embedder build it and the API reads it, with sqlite-vec.</summary>
public sealed class DatabaseTests(TestData data)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ImportStoresTheMoviesAndMarksThemAsNeedingEmbedding()
    {
        await using var database = await TestDatabase.CreateAsync(data.Movies);
        await using var services = database.Services();
        var repository = services.GetRequiredService<IMovieRepository>();

        var status = await repository.GetStatusAsync(Cancellation);
        var stored = await AllMoviesAsync(repository);

        Assert.Equal((true, data.Movies.Count, true), (status.Exists, status.MovieCount, status.EmbeddingsRequired));
        Assert.All(data.Movies, movie =>
        {
            var match = Assert.Single(stored, candidate => candidate.Title == movie.Title);
            Assert.Equal(movie.Genres.Order(), match.Genres);
            Assert.Equal(movie.Overview, match.Overview);
        });
    }

    [Fact]
    public async Task ImportRefusesADatabaseThatAlreadyHasASchema()
    {
        await using var database = await TestDatabase.CreateAsync(data.Movies);
        await using var connection = database.Connect();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => MovieDatabase.CreateAsync(connection, data.Movies, Cancellation));
    }

    [Fact]
    public async Task EmbeddingWritesAVectorPerMovieAndRecordsTheModel()
    {
        await using var database = await TestDatabase.CreateAsync(data.Movies);
        await using var connection = database.Connect();

        var embedded = await MovieDatabase.EmbedAsync(connection, data.Embedder, cancellationToken: Cancellation);

        await using var services = database.Services();
        var status = await services.GetRequiredService<IMovieRepository>().GetStatusAsync(Cancellation);
        Assert.Equal(data.Movies.Count, embedded);
        Assert.Equal(data.Movies.Count, await CountVectorsAsync(database));
        Assert.Equal(
            (false, "bge-small-en-v1.5", "int8"),
            (status.EmbeddingsRequired, status.EmbeddingModel, status.EmbeddingVariant));
    }

    [Fact]
    public async Task EmbeddingIsSkippedWhenUpToDateAndRedoneWhenForcedOrTheModelChanges()
    {
        await using var database = await TestDatabase.CreateAsync(data.Movies, data.Embedder);
        await using var connection = database.Connect();

        var unchanged = await MovieDatabase.EmbedAsync(connection, data.Embedder, cancellationToken: Cancellation);
        var forced = await MovieDatabase.EmbedAsync(connection, data.Embedder, force: true, cancellationToken: Cancellation);
        var otherModel = await MovieDatabase.EmbedAsync(connection, new OtherModelEmbedder(), cancellationToken: Cancellation);

        Assert.Null(unchanged);
        Assert.Equal(data.Movies.Count, forced);
        Assert.Equal(data.Movies.Count, otherModel);
    }

    [Fact]
    public async Task SqliteVecFindsAMovieByItsOwnText()
    {
        await using var services = data.Database.Services();
        var toyStory = await MovieAsync(services, "Toy Story");
        var vector = Assert.Single(
            await data.Embedder.EmbedDocumentsAsync([MovieText.ForEmbedding(toyStory)], Cancellation));

        var nearest = await services.GetRequiredService<IVectorSearch>().FindNearestAsync(
            new VectorQuery(vector, data.Embedder.Model, data.Embedder.Variant, Limit: 1), Cancellation);

        Assert.Equal("Toy Story", Assert.Single(nearest!).Title);
    }

    [Fact]
    public async Task SearchesCantAnswerWhenTheEmbeddingsAreOutOfDate()
    {
        await using var database = await TestDatabase.CreateAsync(data.Movies);
        await using var services = database.Services();
        var vectors = services.GetRequiredService<IVectorSearch>();
        var toyStory = await MovieAsync(services, "Toy Story");

        Assert.Null(await vectors.FindNearestAsync(Query(), Cancellation));
        Assert.Null(await vectors.FindSimilarAsync(new SimilarQuery(toyStory.Id, 0.7, Limit: 10), Cancellation));
    }

    [Fact]
    public async Task OnlySimilarMoviesCanAnswerWhenAnotherModelMadeTheEmbeddings()
    {
        await using var database = await TestDatabase.CreateAsync(data.Movies, new OtherModelEmbedder());
        await using var services = database.Services();
        var vectors = services.GetRequiredService<IVectorSearch>();
        var toyStory = await MovieAsync(services, "Toy Story");

        Assert.Null(await vectors.FindNearestAsync(Query(), Cancellation));
        Assert.NotNull(await vectors.FindSimilarAsync(new SimilarQuery(toyStory.Id, 0.7, Limit: 10), Cancellation));
    }

    [Fact]
    public async Task SimilarMoviesLeaveOutTheMovieItselfAndWeakMatchesMostAlikeFirst()
    {
        await using var services = data.Database.Services();
        var vectors = services.GetRequiredService<IVectorSearch>();
        var toyStory = await MovieAsync(services, "Toy Story");

        var similar = await vectors.FindSimilarAsync(new SimilarQuery(toyStory.Id, 0.7, Limit: 10), Cancellation);
        var identicalOnly = await vectors.FindSimilarAsync(new SimilarQuery(toyStory.Id, 1, Limit: 10), Cancellation);
        Assert.NotNull(similar);

        var embeddings = await data.Embedder.EmbedDocumentsAsync(
            [MovieText.ForEmbedding(toyStory), .. similar.Select(MovieText.ForEmbedding)], Cancellation);
        var similarities = embeddings.Skip(1).Select(vector => Vectors.Similarity(embeddings[0], vector)).ToList();

        Assert.DoesNotContain(similar, movie => movie.Id == toyStory.Id);
        Assert.Contains(similar, movie => movie.Title == "Toy Story 2");
        Assert.Contains(similar, movie => movie.Title == "Toy Story 3");
        Assert.All(similarities, similarity => Assert.InRange(similarity, 0.7, 1));
        Assert.Equal(similarities.OrderDescending(), similarities);
        Assert.Empty(identicalOnly!);
    }

    private VectorQuery Query() =>
        new(new float[data.Embedder.Dimensions], data.Embedder.Model, data.Embedder.Variant, Limit: 1);

    private static async Task<IReadOnlyList<Movie>> AllMoviesAsync(IMovieRepository repository) =>
        (await repository.SearchAsync(
            new MovieQuery(null, MovieSortBy.Title, SortDirection.Asc, Page: 1, PageSize: 100), Cancellation)).Items;

    private static async Task<Movie> MovieAsync(IServiceProvider services, string title) =>
        (await AllMoviesAsync(services.GetRequiredService<IMovieRepository>())).Single(movie => movie.Title == title);

    private static async Task<long> CountVectorsAsync(TestDatabase database)
    {
        await using var connection = database.Connect();
        await connection.OpenAsync(Cancellation);
        connection.LoadExtension("vec0");
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM MovieEmbeddings";
        return (long)(await command.ExecuteScalarAsync(Cancellation))!;
    }
}
