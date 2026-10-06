namespace OptixMovies.Core;

/// <summary>Movie features, independent of HTTP and storage. Every endpoint goes through here.</summary>
public sealed class MovieService(IMovieRepository movies, ITitleSearch titles, TitleSuggestionSettings settings)
{
    public Task<Page<Movie>> SearchAsync(MovieQuery query, CancellationToken cancellationToken = default) =>
        movies.SearchAsync(query with { Genre = Clean(query.Genre) }, cancellationToken);

    public Task<Movie?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        movies.GetAsync(id, cancellationToken);

    public Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default) =>
        movies.GetGenresAsync(cancellationToken);

    /// <summary>
    /// Suggests titles for what the user has typed so far: each word must start a word in the title, ignoring case,
    /// accents and punctuation. Ignored words such as "the" are dropped unless they're all there is, and titles that
    /// start with the whole text come first.
    /// </summary>
    public async Task<IReadOnlyList<TitleSuggestion>> SuggestTitlesAsync(
        string query, int limit, CancellationToken cancellationToken = default)
    {
        var text = SearchText.Normalize(query);

        if (text.Length == 0)
        {
            return [];
        }

        var words = text.Split(' ');
        string[] significantWords = [.. words.Where(word => !settings.IgnoredWords.Contains(word))];

        return await titles.SuggestAsync(
            new TitleQuery(text, significantWords.Length > 0 ? significantWords : words, limit), cancellationToken);
    }

    /// <summary>Trims text, treating blank text as no filter at all.</summary>
    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
