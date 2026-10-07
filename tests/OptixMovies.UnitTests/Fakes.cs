using OptixMovies.Core;

namespace OptixMovies.UnitTests;

internal sealed class FakeMovieRepository : IMovieRepository
{
    public List<MovieQuery> Queries { get; } = [];

    public Task<Page<Movie>> SearchAsync(MovieQuery query, CancellationToken cancellationToken = default)
    {
        Queries.Add(query);
        return Task.FromResult(new Page<Movie>([], query.Page, query.PageSize, TotalCount: 0));
    }

    public Task<Movie?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult<Movie?>(null);

    public Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);

    public Task<DataStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new DataStatus("test", Exists: true, MovieCount: 0, EmbeddingsRequired: false));
}

internal sealed class FakeTitleSearch : ITitleSearch
{
    public List<TitleQuery> Queries { get; } = [];

    public Task<IReadOnlyList<TitleSuggestion>> SuggestAsync(
        TitleQuery query, CancellationToken cancellationToken = default)
    {
        Queries.Add(query);
        return Task.FromResult<IReadOnlyList<TitleSuggestion>>([]);
    }
}

internal sealed class FakeEmbedder : ITextEmbedder
{
    public static readonly float[] Vector = [1, 0, 0];

    public List<string> Queries { get; } = [];

    public List<string> Documents { get; } = [];

    public string Model => "fake-model";

    public string Variant => "test";

    public int Dimensions => Vector.Length;

    public Task<IReadOnlyList<float[]>> EmbedDocumentsAsync(
        IReadOnlyList<string> documents, CancellationToken cancellationToken = default)
    {
        Documents.AddRange(documents);
        return Task.FromResult<IReadOnlyList<float[]>>([.. documents.Select(_ => Vector)]);
    }

    public Task<float[]> EmbedQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        Queries.Add(query);
        return Task.FromResult(Vector);
    }
}

internal sealed class FakeVectorSearch : IVectorSearch
{
    public List<VectorQuery> NearestQueries { get; } = [];

    public List<SimilarQuery> SimilarQueries { get; } = [];

    /// <summary>What every search returns: <see langword="null"/> means the embeddings can't answer.</summary>
    public IReadOnlyList<Movie>? Result { get; set; } = [];

    public Task<IReadOnlyList<Movie>?> FindNearestAsync(
        VectorQuery query, CancellationToken cancellationToken = default)
    {
        NearestQueries.Add(query);
        return Task.FromResult(Result);
    }

    public Task<IReadOnlyList<Movie>?> FindSimilarAsync(
        SimilarQuery query, CancellationToken cancellationToken = default)
    {
        SimilarQueries.Add(query);
        return Task.FromResult(Result);
    }
}
