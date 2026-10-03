namespace OptixMovies.Core;

/// <summary>Reads movies and their genres from storage.</summary>
public interface IMovieRepository
{
    /// <summary>Returns every genre name, A to Z ignoring case.</summary>
    Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default);
}
