namespace OptixMovies.Core;

/// <summary>Settings for similar movies. The API reads them from configuration.</summary>
/// <param name="MinSimilarity">
/// How alike two movies' embeddings must be, as a cosine similarity, for one to be suggested beside the other. The
/// right value depends on the embedding model: with bge-small-en-v1.5, movies less alike than 0.7 are mostly unrelated.
/// </param>
public sealed record SimilarMovieSettings(double MinSimilarity);
