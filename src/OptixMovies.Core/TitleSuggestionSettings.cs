namespace OptixMovies.Core;

/// <summary>Settings for title suggestions. The API reads them from configuration.</summary>
public sealed class TitleSuggestionSettings(IEnumerable<string> ignoredWords)
{
    /// <summary>Words left out of a search unless they're all it contains, such as "the".</summary>
    public IReadOnlySet<string> IgnoredWords { get; } = ignoredWords.Select(SearchText.Normalize).ToHashSet();
}
