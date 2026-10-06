using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using OptixMovies.Core;

namespace OptixMovies.Data.Sqlite;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IMovieRepository"/> and <see cref="ITitleSearch"/>, backed by the SQLite database in
    /// <paramref name="connectionString"/>.
    /// </summary>
    /// <exception cref="FileNotFoundException">The database file doesn't exist.</exception>
    public static IServiceCollection AddSqliteData(this IServiceCollection services, string connectionString)
    {
        var path = Path.GetFullPath(new SqliteConnectionStringBuilder(connectionString).DataSource);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"No movie database at {path}.", path);
        }

        return services
            .AddDbContext<MoviesDbContext>(options => options.UseSqlite(connectionString))
            .AddScoped<IMovieRepository, MovieRepository>()
            .AddScoped<ITitleSearch, TitleSearch>();
    }
}
