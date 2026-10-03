namespace OptixMovies.Core;

/// <summary>Movie features, independent of HTTP and storage. Every endpoint goes through here.</summary>
public sealed class MovieService(IMovieRepository movies)
{
    public Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default) =>
        movies.GetGenresAsync(cancellationToken);
}
