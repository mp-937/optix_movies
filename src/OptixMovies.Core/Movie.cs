namespace OptixMovies.Core;

/// <summary>A movie in the catalogue.</summary>
public sealed record Movie(
    string Title,
    string Overview,
    DateOnly ReleaseDate,
    double Popularity,
    int VoteCount,
    double VoteAverage,
    string OriginalLanguage,
    IReadOnlyList<string> Genres,
    Uri PosterUrl);
