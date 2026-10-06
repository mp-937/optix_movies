namespace OptixMovies.Core;

/// <summary>Finds titles for search-as-you-type. SQLite implements it today; a search engine could later.</summary>
public interface ITitleSearch
{
    /// <summary>
    /// Returns up to <see cref="TitleQuery.Limit"/> titles in which every query word starts a word:
    /// titles that start with the whole query first, then the most popular.
    /// </summary>
    Task<IReadOnlyList<TitleSuggestion>> SuggestAsync(TitleQuery query, CancellationToken cancellationToken = default);
}
