using System.Runtime.InteropServices;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using OptixMovies.Core;

namespace OptixMovies.Data.Sqlite;

/// <summary>Builds the SQLite movie database: first the movies, then their embeddings for semantic search.</summary>
public static class MovieDatabase
{
    private const int EmbeddingChunkSize = 500;

    /// <summary>
    /// Creates the schema in an empty database, fills it with <paramref name="movies"/> and marks them as needing
    /// embedding. Everything happens in one transaction, so a failure leaves the database empty.
    /// </summary>
    /// <exception cref="InvalidOperationException">The database already has a schema.</exception>
    public static async Task CreateAsync(
        SqliteConnection connection,
        IEnumerable<Movie> movies,
        CancellationToken cancellationToken = default)
    {
        await using var db = CreateContext(connection);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (!await db.Database.EnsureCreatedAsync(cancellationToken))
        {
            throw new InvalidOperationException("The database already has a schema.");
        }

        var genres = new Dictionary<string, GenreEntity>();

        db.Movies.AddRange(movies.Select(movie => new MovieEntity
        {
            Title = movie.Title,
            SearchTitle = SearchText.Normalize(movie.Title),
            Overview = movie.Overview,
            ReleaseDate = movie.ReleaseDate,
            Popularity = movie.Popularity,
            VoteCount = movie.VoteCount,
            VoteAverage = movie.VoteAverage,
            OriginalLanguage = movie.OriginalLanguage,
            PosterUrl = movie.PosterUrl,
            Genres = [.. movie.Genres.Distinct().Select(GenreNamed)],
        }));
        db.EmbeddingStatus.Add(new EmbeddingStatusEntity { Required = true });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        GenreEntity GenreNamed(string name) =>
            genres.TryGetValue(name, out var genre) ? genre : genres[name] = new GenreEntity { Name = name };
    }

    /// <summary>
    /// Embeds every movie with <paramref name="embedder"/>, unless the embeddings are up to date: no movie has changed
    /// since they were made, by the same model and variant. <paramref name="force"/> embeds the movies regardless.
    /// The vectors and the status are replaced in one transaction, so a failure keeps the previous ones.
    /// </summary>
    /// <param name="progress">Receives the fraction of movies embedded so far, from 0 to 1.</param>
    /// <returns>How many movies were embedded, or <see langword="null"/> if the embeddings were up to date.</returns>
    public static async Task<int?> EmbedAsync(
        SqliteConnection connection,
        ITextEmbedder embedder,
        bool force = false,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await connection.OpenAsync(cancellationToken);
        connection.LoadExtension("vec0"); // sqlite-vec, which searches vectors inside SQLite.

        await using var db = CreateContext(connection);
        var status = await db.EmbeddingStatus.SingleAsync(cancellationToken);

        if (!force && !status.Required && status.Model == embedder.Model && status.Variant == embedder.Variant)
        {
            return null;
        }

        var movies = await db.Movies
            .OrderBy(movie => movie.Id)
            .Select(MovieRepository.ToMovie)
            .ToListAsync(cancellationToken);
        var vectors = new List<float[]>(movies.Count);

        foreach (var chunk in movies.Chunk(EmbeddingChunkSize))
        {
            vectors.AddRange(await embedder.EmbedDocumentsAsync(
                [.. chunk.Select(MovieText.ForEmbedding)], cancellationToken));
            progress?.Report((double)vectors.Count / movies.Count);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var sqliteTransaction = (SqliteTransaction)transaction.GetDbTransaction();

        await ExecuteAsync("DROP TABLE IF EXISTS MovieEmbeddings");
        await ExecuteAsync(
            "CREATE VIRTUAL TABLE MovieEmbeddings USING vec0(" +
            $"MovieId INTEGER PRIMARY KEY, Embedding FLOAT[{embedder.Dimensions}] distance_metric=cosine)");

        await using var insert = connection.CreateCommand();
        insert.Transaction = sqliteTransaction;
        insert.CommandText = "INSERT INTO MovieEmbeddings (MovieId, Embedding) VALUES ($movieId, $embedding)";
        var movieId = insert.Parameters.Add("$movieId", SqliteType.Integer);
        var embedding = insert.Parameters.Add("$embedding", SqliteType.Blob);

        for (var i = 0; i < movies.Count; i++)
        {
            movieId.Value = movies[i].Id;
            embedding.Value = MemoryMarshal.AsBytes(vectors[i].AsSpan()).ToArray(); // sqlite-vec's format: raw floats
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        status.Required = false;
        status.Model = embedder.Model;
        status.Variant = embedder.Variant;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return movies.Count;

        async Task ExecuteAsync(string sql)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = sqliteTransaction;
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static MoviesDbContext CreateContext(SqliteConnection connection) =>
        new(new DbContextOptionsBuilder<MoviesDbContext>().UseSqlite(connection).Options);
}
