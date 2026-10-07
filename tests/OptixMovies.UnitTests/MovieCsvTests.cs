using System.Globalization;

using OptixMovies.Core;
using OptixMovies.Importer;

namespace OptixMovies.UnitTests;

public sealed class MovieCsvTests
{
    private const string Header =
        "Release_Date,Title,Overview,Popularity,Vote_Count,Vote_Average,Original_Language,Genre,Poster_Url";

    private const string ToyStory =
        "1995-10-30,Toy Story,\"Led by Woody, Andy's toys live happily.\",171.714,15127,8.0,en," +
        "\"Animation, Adventure, Family, Comedy\",https://image.tmdb.org/t/p/original/uXDfjJbdP4ijW5hWSBrPrlKpxab.jpg";

    [Fact]
    public void ReadsEveryFieldOfARowInAnyLocale()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE"); // Writes 171.714 as 171,714.

        try
        {
            var movie = Assert.Single(Read(Header, ToyStory));

            Assert.Equal("Toy Story", movie.Title);
            Assert.Equal("Led by Woody, Andy's toys live happily.", movie.Overview);
            Assert.Equal(new DateOnly(1995, 10, 30), movie.ReleaseDate);
            Assert.Equal(171.714, movie.Popularity);
            Assert.Equal(15127, movie.VoteCount);
            Assert.Equal(8.0, movie.VoteAverage);
            Assert.Equal("en", movie.OriginalLanguage);
            Assert.Equal(["Animation", "Adventure", "Family", "Comedy"], movie.Genres);
            Assert.Equal(
                new Uri("https://image.tmdb.org/t/p/original/uXDfjJbdP4ijW5hWSBrPrlKpxab.jpg"), movie.PosterUrl);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void MatchesColumnsByName()
    {
        var movie = Assert.Single(Read(
            "Title,Extra,Genre,Poster_Url,Overview,Release_Date,Popularity,Vote_Count,Vote_Average,Original_Language",
            "Toy Story,ignored,Animation,https://image.tmdb.org/t/p/original/a.jpg,Toys live.,1995-10-30,171.714,15127,8.0,en"));

        Assert.Equal("Toy Story", movie.Title);
        Assert.Equal("Toys live.", movie.Overview);
        Assert.Equal(new DateOnly(1995, 10, 30), movie.ReleaseDate);
    }

    [Theory]
    [InlineData("\"Woody leads.\rBuzz arrives.\"")] // Quoted.
    [InlineData("Woody leads.\rBuzz arrives.")] // Unquoted, as one row in the dataset has.
    public void BareCarriageReturnInAnOverviewDoesNotEndTheRow(string overview)
    {
        var movies = Read(
            Header,
            $"1995-10-30,Toy Story,{overview},171.714,15127,8.0,en,Animation,https://image.tmdb.org/t/p/original/a.jpg",
            "1999-10-30,Toy Story 2,Woody is stolen.,149.026,12000,7.6,en,Animation,https://image.tmdb.org/t/p/original/b.jpg");

        Assert.Equal(["Toy Story", "Toy Story 2"], movies.Select(movie => movie.Title));
        Assert.Equal("Woody leads.\nBuzz arrives.", movies[0].Overview);
    }

    [Fact]
    public void InvalidValueNamesTheRowAndColumn()
    {
        var error = Assert.Throws<InvalidDataException>(() => Read(Header, ToyStory.Replace("171.714", "abc")));

        Assert.Equal("Row 2: 'abc' is not a valid Popularity.", error.Message);
    }

    [Fact]
    public void MissingColumnFails()
    {
        var error = Assert.Throws<InvalidDataException>(() => Read(
            Header.Replace(",Genre", ""),
            ToyStory.Replace(",\"Animation, Adventure, Family, Comedy\"", "")));

        Assert.Equal("The CSV has no Genre column.", error.Message);
    }

    [Fact]
    public void BlankRequiredTextFails()
    {
        var error = Assert.Throws<InvalidDataException>(() => Read(Header, ToyStory.Replace("Toy Story", " ")));

        Assert.Equal("Row 2: ' ' is not a valid Title.", error.Message);
    }

    private static IReadOnlyList<Movie> Read(params string[] lines) =>
        MovieCsv.Read(new StringReader(string.Join("\n", lines) + "\n"));
}
