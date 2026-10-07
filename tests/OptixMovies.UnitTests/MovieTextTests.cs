using OptixMovies.Core;

namespace OptixMovies.UnitTests;

public sealed class MovieTextTests
{
    [Fact]
    public void JoinsTitleGenresAndOverview() =>
        Assert.Equal(
            "Toy Story. Animation, Comedy. Led by Woody, Andy's toys live happily.",
            MovieText.ForEmbedding(TestMovies.Movie("Toy Story", ["Animation", "Comedy"])));

    [Fact]
    public void LeavesOutEmptyParts() =>
        Assert.Equal(
            "Toy Story. Led by Woody, Andy's toys live happily.",
            MovieText.ForEmbedding(TestMovies.Movie("Toy Story", [])));
}
