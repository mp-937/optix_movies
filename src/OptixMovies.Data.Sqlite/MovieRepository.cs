using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using OptixMovies.Core;

namespace OptixMovies.Data.Sqlite;

internal sealed class MovieRepository(MoviesDbContext db) : IMovieRepository
{
    private static readonly Expression<Func<MovieEntity, Movie>> ToMovie = movie => new Movie(
        movie.Title,
        movie.Overview,
        movie.ReleaseDate,
        movie.Popularity,
        movie.VoteCount,
        movie.VoteAverage,
        movie.OriginalLanguage,
        movie.Genres.OrderBy(genre => genre.Name).Select(genre => genre.Name).ToList(),
        movie.PosterUrl)
    {
        Id = movie.Id,
    };

    public async Task<Page<Movie>> SearchAsync(MovieQuery query, CancellationToken cancellationToken = default)
    {
        var movies = db.Movies.AsQueryable();

        if (query.Genre is { } genre)
        {
            movies = movies.Where(movie => movie.Genres.Any(g => EF.Functions.Collate(g.Name, "NOCASE") == genre));
        }

        var totalCount = await movies.CountAsync(cancellationToken);
        var items = await Order(movies, query.SortBy, query.SortDirection)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(ToMovie)
            .ToListAsync(cancellationToken);

        return new Page<Movie>(items, query.Page, query.PageSize, totalCount);
    }

    public Task<Movie?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.Movies.Where(movie => movie.Id == id).Select(ToMovie).SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default) =>
        await db.Genres
            .Select(genre => genre.Name)
            .OrderBy(name => EF.Functions.Collate(name, "NOCASE"))
            .ToListAsync(cancellationToken);

    // Ties are broken by id so that paging is stable: no movie appears on two pages.
    private static IOrderedQueryable<MovieEntity> Order(
        IQueryable<MovieEntity> movies, MovieSortBy sortBy, SortDirection direction) =>
        (sortBy, direction) switch
        {
            (MovieSortBy.Title, SortDirection.Asc) =>
                movies.OrderBy(m => EF.Functions.Collate(m.Title, "NOCASE")).ThenBy(m => m.Id),
            (MovieSortBy.Title, SortDirection.Desc) =>
                movies.OrderByDescending(m => EF.Functions.Collate(m.Title, "NOCASE")).ThenByDescending(m => m.Id),
            (MovieSortBy.ReleaseDate, SortDirection.Asc) =>
                movies.OrderBy(m => m.ReleaseDate).ThenBy(m => m.Id),
            (MovieSortBy.ReleaseDate, SortDirection.Desc) =>
                movies.OrderByDescending(m => m.ReleaseDate).ThenByDescending(m => m.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), $"Can't sort by {sortBy} {direction}."),
        };
}
