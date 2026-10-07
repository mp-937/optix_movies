namespace OptixMovies.Core;

/// <summary>Finds movies by their embeddings. sqlite-vec implements it today; a vector database could later.</summary>
public interface IVectorSearch
{
    /// <summary>
    /// Returns up to <see cref="VectorQuery.Limit"/> movies, nearest first, or <see langword="null"/> if the stored
    /// embeddings can't be compared with the query's: there are none, they're out of date, or another model or variant
    /// made them.
    /// </summary>
    Task<IReadOnlyList<Movie>?> FindNearestAsync(VectorQuery query, CancellationToken cancellationToken = default);
}
