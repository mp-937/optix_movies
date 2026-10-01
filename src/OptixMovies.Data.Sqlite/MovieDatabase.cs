using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using OptixMovies.Core;

namespace OptixMovies.Data.Sqlite;

/// <summary>Creates the SQLite movie database.</summary>
public static class MovieDatabase
{
    /// <summary>
    /// Creates the schema in an empty database and fills it with <paramref name="movies"/>.
    /// Everything happens in one transaction, so a failure leaves the database empty.
    /// </summary>
    /// <exception cref="InvalidOperationException">The database already has a schema.</exception>
    public static async Task CreateAsync(
        SqliteConnection connection,
        IEnumerable<Movie> movies,
        CancellationToken cancellationToken = default)
    {
        var options = new DbContextOptionsBuilder<MoviesDbContext>().UseSqlite(connection).Options;
        await using var db = new MoviesDbContext(options);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (!await db.Database.EnsureCreatedAsync(cancellationToken))
        {
            throw new InvalidOperationException("The database already has a schema.");
        }

        var genres = new Dictionary<string, GenreEntity>();

        db.Movies.AddRange(movies.Select(movie => new MovieEntity
        {
            Title = movie.Title,
            Overview = movie.Overview,
            ReleaseDate = movie.ReleaseDate,
            Popularity = movie.Popularity,
            VoteCount = movie.VoteCount,
            VoteAverage = movie.VoteAverage,
            OriginalLanguage = movie.OriginalLanguage,
            PosterUrl = movie.PosterUrl,
            Genres = [.. movie.Genres.Distinct().Select(GenreNamed)],
        }));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        GenreEntity GenreNamed(string name) =>
            genres.TryGetValue(name, out var genre) ? genre : genres[name] = new GenreEntity { Name = name };
    }
}
