namespace OptixMovies.Core;

/// <summary>A title offered while the user types. The year tells remakes apart.</summary>
public sealed record TitleSuggestion(int Id, string Title, int Year);
