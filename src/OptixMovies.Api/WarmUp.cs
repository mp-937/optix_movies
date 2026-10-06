using OptixMovies.Core;

namespace OptixMovies.Api;

internal static class WarmUp
{
    /// <summary>
    /// Runs each kind of query once before the API starts serving, so the first visitor doesn't wait seconds while
    /// .NET compiles code and EF Core builds its model and queries.
    /// </summary>
    public static async Task WarmUpAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var movies = scope.ServiceProvider.GetRequiredService<MovieService>();

        await movies.GetGenresAsync();
        await movies.GetAsync(1);
        await movies.SearchAsync(new MovieQuery(null, MovieSortBy.Title, SortDirection.Asc, Page: 1, PageSize: 1));
        await movies.SearchAsync(new MovieQuery("Drama", MovieSortBy.Title, SortDirection.Asc, Page: 1, PageSize: 1));
        await movies.SuggestTitlesAsync("warm", limit: 1);
        await movies.SuggestTitlesAsync("warm up", limit: 1);
    }
}
