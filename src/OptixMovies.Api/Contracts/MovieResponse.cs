namespace OptixMovies.Api.Contracts;

public sealed record MovieResponse(
    int Id,
    string Title,
    string Overview,
    DateOnly ReleaseDate,
    double Popularity,
    int VoteCount,
    double VoteAverage,
    string OriginalLanguage,
    IReadOnlyList<string> Genres,
    Uri PosterUrl);
