namespace OptixMovies.Core;

/// <summary>One page of results, with the total across all pages.</summary>
public sealed record Page<T>(IReadOnlyList<T> Items, int Number, int Size, int TotalCount);
