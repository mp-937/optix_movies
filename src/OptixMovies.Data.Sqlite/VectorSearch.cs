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

                var vector = MemoryMarshal.AsBytes(query.Vector.AsSpan()).ToArray();

                return await FindAsync(
                    $"""
                    SELECT MovieId AS Value FROM MovieEmbeddings
                    WHERE Embedding MATCH {vector} AND k = {query.Limit}
                    ORDER BY distance
                    """,
                    cancellationToken);
            },
            cancellationToken);

    // Compares stored vectors with each other, so any model will do, as long as no movie has changed since.
    public Task<IReadOnlyList<Movie>?> FindSimilarAsync(
        SimilarQuery query, CancellationToken cancellationToken = default) =>
        db.RunInterruptibleAsync<IReadOnlyList<Movie>?>(
            async () =>
            {
                if (await db.EmbeddingStatus.AnyAsync(status => status.Required, cancellationToken))
                {
                    return null;
                }

                var maxDistance = 1 - query.MinSimilarity; // sqlite-vec's cosine distance

                return await FindAsync(
                    $"""
                    SELECT MovieId AS Value FROM (
                        SELECT MovieId, distance FROM MovieEmbeddings
                        WHERE Embedding MATCH (SELECT Embedding FROM MovieEmbeddings WHERE MovieId = {query.MovieId})
                            AND k = {query.Limit + 1})
                    WHERE MovieId <> {query.MovieId} AND distance <= {maxDistance}
                    ORDER BY distance
                    """,
                    cancellationToken);
            },
            cancellationToken);

    /// <summary>
    /// Runs a sqlite-vec query for movie ids, nearest first, and returns those movies in the same order. Only these
    /// searches load sqlite-vec, so if the extension can't load, everything else still works.
    /// </summary>
    private async Task<IReadOnlyList<Movie>> FindAsync(FormattableString nearestIds, CancellationToken cancellationToken)
    {
        ((SqliteConnection)db.Database.GetDbConnection()).LoadExtension("vec0");

        var ids = await db.Database.SqlQuery<int>(nearestIds).ToListAsync(cancellationToken);
        var movies = await db.Movies
            .Where(movie => ids.Contains(movie.Id))
            .Select(MovieRepository.ToMovie)
            .ToDictionaryAsync(movie => movie.Id, cancellationToken);

        return [.. ids.Select(id => movies[id])];
    }
}
