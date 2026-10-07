namespace OptixMovies.Core;

/// <summary>A search for the movies most like the one with <paramref name="MovieId"/>, leaving it out.</summary>
public sealed record SimilarQuery(int MovieId, double MinSimilarity, int Limit);
