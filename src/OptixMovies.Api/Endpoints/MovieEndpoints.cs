using System.ComponentModel.DataAnnotations;

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
        api.MapGet("/movies/{id:int}/similar", GetSimilarMovies)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
        api.MapGet("/movies/title-suggestions", SuggestTitles);
        api.MapGet("/movies/semantic-search", SemanticSearch)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
        api.MapGet("/genres", GetGenres);

        return app;
    }

    // page is capped so that the row offset, (page - 1) * pageSize, can't overflow.
    private static async Task<Ok<PageResponse<MovieResponse>>> SearchMovies(
        MovieService movies,
        [StringLength(100)] string? genre,
        [EnumDataType(typeof(MovieSortBy))] MovieSortBy sortBy = MovieSortBy.Title,
        [EnumDataType(typeof(SortDirection))] SortDirection sortDirection = SortDirection.Asc,
        [Range(1, 1_000_000)] int page = 1,
        [Range(1, 100)] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var results = await movies.SearchAsync(
            new MovieQuery(genre, sortBy, sortDirection, page, pageSize), cancellationToken);

        return TypedResults.Ok(PageResponse.From(results, MovieResponse.From));
    }

    private static async Task<Results<Ok<MovieResponse>, NotFound>> GetMovie(
        MovieService movies, int id, CancellationToken cancellationToken) =>
        await movies.GetAsync(id, cancellationToken) is { } movie
            ? TypedResults.Ok(MovieResponse.From(movie))
            : TypedResults.NotFound();

    private static async Task<Results<Ok<IReadOnlyList<MovieResponse>>, NotFound, ProblemHttpResult>> GetSimilarMovies(
        MovieService movies,
        int id,
        [Range(1, 50)] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (await movies.GetAsync(id, cancellationToken) is null)
        {
            return TypedResults.NotFound();
        }

        if (await movies.FindSimilarAsync(id, limit, cancellationToken) is not { } similar)
        {
            return EmbeddingsUnavailable();
        }

        IReadOnlyList<MovieResponse> response = [.. similar.Select(MovieResponse.From)];

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<IReadOnlyList<TitleSuggestionResponse>>> SuggestTitles(
        MovieService movies,
        [Required, StringLength(100)] string query,
        [Range(1, 20)] int limit = 8,
        CancellationToken cancellationToken = default)
    {
        var suggestions = await movies.SuggestTitlesAsync(query, limit, cancellationToken);
        IReadOnlyList<TitleSuggestionResponse> response = [.. suggestions.Select(TitleSuggestionResponse.From)];

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<IReadOnlyList<MovieResponse>>, ProblemHttpResult>> SemanticSearch(
        MovieService movies,
        [Required, StringLength(200)] string query,
        [Range(1, 50)] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (await movies.SemanticSearchAsync(query, limit, cancellationToken) is not { } results)
        {
            return EmbeddingsUnavailable();
        }

        IReadOnlyList<MovieResponse> response = [.. results.Select(MovieResponse.From)];

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<IReadOnlyList<string>>> GetGenres(
        MovieService movies, CancellationToken cancellationToken) =>
        TypedResults.Ok(await movies.GetGenresAsync(cancellationToken));

    private static ProblemHttpResult EmbeddingsUnavailable() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Semantic search is unavailable until the movies are embedded");
}
