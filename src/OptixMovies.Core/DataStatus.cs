namespace OptixMovies.Core;

/// <summary>The state of the stored data, checked when the API starts.</summary>
/// <param name="Location">Where the data lives, such as a file path, for messages.</param>
/// <param name="Exists">Whether there's a database at all.</param>
/// <param name="MovieCount">How many movies the database holds.</param>
/// <param name="EmbeddingsRequired">Whether movies have changed since they were last embedded.</param>
/// <param name="EmbeddingModel">The model that last embedded the movies, if any.</param>
/// <param name="EmbeddingVariant">Which build of that model.</param>
public sealed record DataStatus(
    string Location,
    bool Exists,
    int MovieCount,
    bool EmbeddingsRequired,
    string? EmbeddingModel = null,
    string? EmbeddingVariant = null);
