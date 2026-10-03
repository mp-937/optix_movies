namespace OptixMovies.Core;

/// <summary>Movie features, independent of HTTP and storage. Every endpoint goes through here.</summary>
public sealed class MovieService(IMovieRepository movies)
{
    public Task<Page<Movie>> SearchAsync(MovieQuery query, CancellationToken cancellationToken = default) =>
        movies.SearchAsync(query with { Genre = Clean(query.Genre) }, cancellationToken);

    public Task<Movie?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        movies.GetAsync(id, cancellationToken);

    public Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default) =>
        movies.GetGenresAsync(cancellationToken);

    /// <summary>Trims text, treating blank text as no filter at all.</summary>
    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
