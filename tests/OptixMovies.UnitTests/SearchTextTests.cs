using OptixMovies.Core;

namespace OptixMovies.UnitTests;

public sealed class SearchTextTests
{
    [Theory]
    [InlineData("Pokémon", "pokemon")]
    [InlineData("Léon", "leon")]
    [InlineData("AMÉLIE", "amelie")]
    public void LowercasesAndStripsAccents(string text, string expected) =>
        Assert.Equal(expected, SearchText.Normalize(text));

    [Theory]
    [InlineData("The King's Man", "the kings man")]
    [InlineData("L.A. Confidential", "la confidential")]
    public void DropsApostrophesAndFullStops(string text, string expected) =>
        Assert.Equal(expected, SearchText.Normalize(text));

    [Theory]
    [InlineData("Spider-Man:  Far From Home", "spider man far from home")]
    [InlineData("  Mission: Impossible – Fallout  ", "mission impossible fallout")]
    public void BreaksWordsAtOtherPunctuationAndCollapsesSpaces(string text, string expected) =>
        Assert.Equal(expected, SearchText.Normalize(text));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("?! …")]
    public void ReturnsEmptyTextWithoutLettersOrDigits(string text) =>
        Assert.Equal("", SearchText.Normalize(text));
}
