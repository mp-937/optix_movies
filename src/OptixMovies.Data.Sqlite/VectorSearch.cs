using System.Runtime.InteropServices;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using OptixMovies.Core;

namespace OptixMovies.Data.Sqlite;

internal sealed class VectorSearch(MoviesDbContext db) : IVectorSearch
{
    public Task<IReadOnlyList<Movie>?> FindNearestAsync(
        VectorQuery query, CancellationToken cancellationToken = default) =>
        db.RunInterruptibleAsync<IReadOnlyList<Movie>?>(
            async () =>
            {
                var status = await db.EmbeddingStatus.SingleAsync(cancellationToken);

                if (status.Required || status.Model != query.Model || status.Variant != query.Variant)
                {
                    return null;
                }

                // Only this search loads sqlite-vec, so if the extension can't load, everything else still works.
                ((SqliteConnection)db.Database.GetDbConnection()).LoadExtension("vec0");

                var vector = MemoryMarshal.AsBytes(query.Vector.AsSpan()).ToArray();
                var ids = await db.Database
                    .SqlQuery<int>($"""
                        SELECT MovieId AS Value FROM MovieEmbeddings
                        WHERE Embedding MATCH {vector} AND k = {query.Limit}
                        ORDER BY distance
                        """)
                    .ToListAsync(cancellationToken);
                var movies = await db.Movies
                    .Where(movie => ids.Contains(movie.Id))
                    .Select(MovieRepository.ToMovie)
                    .ToDictionaryAsync(movie => movie.Id, cancellationToken);

                return [.. ids.Select(id => movies[id])];
            },
            cancellationToken);
}
