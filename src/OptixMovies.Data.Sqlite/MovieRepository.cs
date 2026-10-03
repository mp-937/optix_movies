using Microsoft.EntityFrameworkCore;

using OptixMovies.Core;

namespace OptixMovies.Data.Sqlite;

internal sealed class MovieRepository(MoviesDbContext db) : IMovieRepository
{
    public async Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken cancellationToken = default) =>
        await db.Genres
            .Select(genre => genre.Name)
            .OrderBy(name => EF.Functions.Collate(name, "NOCASE"))
            .ToListAsync(cancellationToken);
}
