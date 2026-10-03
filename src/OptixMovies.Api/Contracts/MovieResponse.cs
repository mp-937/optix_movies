using OptixMovies.Core;

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
    Uri PosterUrl)
{
    public static MovieResponse From(Movie movie) => new(
        movie.Id,
        movie.Title,
        movie.Overview,
        movie.ReleaseDate,
        movie.Popularity,
        movie.VoteCount,
        movie.VoteAverage,
        movie.OriginalLanguage,
        movie.Genres,
        movie.PosterUrl);
}
