namespace OptixMovies.Core;

/// <summary>Which movies to find, in what order, and which page of them.</summary>
public sealed record MovieQuery(string? Genre, MovieSortBy SortBy, SortDirection SortDirection, int Page, int PageSize);
