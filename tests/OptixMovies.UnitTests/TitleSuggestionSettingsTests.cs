using OptixMovies.Core;

namespace OptixMovies.UnitTests;

public sealed class TitleSuggestionSettingsTests
{
    [Fact]
    public void NormalisesIgnoredWords() =>
        Assert.Equal(["of", "the"], new TitleSuggestionSettings(["The", "OF"]).IgnoredWords.Order());
}
