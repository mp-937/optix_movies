using OptixMovies.Core;

namespace OptixMovies.Api.Contracts;

public sealed record TitleSuggestionResponse(int Id, string Title, int Year)
{
    public static TitleSuggestionResponse From(TitleSuggestion suggestion) =>
        new(suggestion.Id, suggestion.Title, suggestion.Year);
}
