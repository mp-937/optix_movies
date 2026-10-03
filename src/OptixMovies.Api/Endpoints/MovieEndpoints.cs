using Microsoft.AspNetCore.Http.HttpResults;

using OptixMovies.Api.Contracts;
using OptixMovies.Core;

namespace OptixMovies.Api.Endpoints;

public static class MovieEndpoints
{
    public static IEndpointRouteBuilder MapMovieEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").WithTags("Movies");

        api.MapGet("/movies", SearchMovies);
        api.MapGet("/movies/{id:int}", GetMovie);
        api.MapGet("/genres", GetGenres);

        return app;
    }

    private static Results<Ok<PageResponse<MovieResponse>>, ProblemHttpResult> SearchMovies(
        string? title,
        string? genre,
        string? actor,
        MovieSortBy? sortBy,
        SortDirection? sortDirection,
        int page = 1,
        int pageSize = 20) => NotImplemented();

    private static Results<Ok<MovieResponse>, NotFound, ProblemHttpResult> GetMovie(int id) => NotImplemented();

    private static async Task<Ok<IReadOnlyList<string>>> GetGenres(
        MovieService movies, CancellationToken cancellationToken) =>
        TypedResults.Ok(await movies.GetGenresAsync(cancellationToken));

    private static ProblemHttpResult NotImplemented() =>
        TypedResults.Problem(statusCode: StatusCodes.Status501NotImplemented, title: "Not implemented yet");
}
