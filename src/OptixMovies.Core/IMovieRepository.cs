namespace OptixMovies.Core;

/// <summary>Reads movies and their genres from storage.</summary>
public interface IMovieRepository
{
    /// <summary>Returns one page of the movies matching <paramref name="query"/>, in the order it asks for.</summary>
    Task<Page<Movie>> SearchAsync(MovieQuery query, CancellationToken cancellationToken = default);

    /// <summary>Returns the movie with the given id, or <see langword="null"/> if there isn't one.</summary>
    Task<Movie?> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Returns every genre name, A to Z ignoring case.</summary>
    Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default);
}
