using OptixMovies.Core;

namespace OptixMovies.UnitTests;

internal static class TestMovies
{
    public static Movie Movie(string title, IReadOnlyList<string> genres) => new(
        title,
        "Led by Woody, Andy's toys live happily.",
        new DateOnly(1995, 10, 30),
        Popularity: 171.714,
        VoteCount: 15127,
        VoteAverage: 8.0,
        OriginalLanguage: "en",
        genres,
        new Uri("https://image.tmdb.org/t/p/original/uXDfjJbdP4ijW5hWSBrPrlKpxab.jpg"));
}
