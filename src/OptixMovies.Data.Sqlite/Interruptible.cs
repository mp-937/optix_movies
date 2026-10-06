using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using SQLitePCL;

namespace OptixMovies.Data.Sqlite;

internal static class Interruptible
{
    /// <summary>
    /// Runs <paramref name="query"/> so that cancellation stops SQLite mid-query. On its own, SQLite only checks the
    /// token before a query starts, so a request the client abandoned would keep the database busy to the end.
    /// </summary>
    public static async Task<T> RunInterruptibleAsync<T>(
        this MoviesDbContext db, Func<Task<T>> query, CancellationToken cancellationToken)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);
        var connection = (SqliteConnection)db.Database.GetDbConnection();

        try
        {
            await using (cancellationToken.Register(() => raw.sqlite3_interrupt(connection.Handle)))
            {
                return await query();
            }
        }
        catch (SqliteException e) when (e.SqliteErrorCode == raw.SQLITE_INTERRUPT)
        {
            // Report it as the cancellation it is, so ASP.NET Core treats it as an aborted request, not an error.
            throw new OperationCanceledException("The query was interrupted.", e, cancellationToken);
        }
    }
}
