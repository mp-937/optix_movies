namespace OptixMovies.Core;

/// <summary>
/// A normalised title search: <paramref name="Text"/> is the whole query and <paramref name="Words"/> are the words
/// that must each start a word in the title.
/// </summary>
public sealed record TitleQuery(string Text, IReadOnlyList<string> Words, int Limit);
