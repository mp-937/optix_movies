using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using OptixMovies.Core;

namespace OptixMovies.Data.Sqlite;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IMovieRepository"/>, <see cref="ITitleSearch"/> and <see cref="IVectorSearch"/>, backed by
    /// the SQLite database in <paramref name="connectionString"/>. <see cref="IMovieRepository.GetStatusAsync"/>
    /// reports a missing file.
    /// </summary>
    public static IServiceCollection AddSqliteData(this IServiceCollection services, string connectionString) =>
        services
            .AddDbContext<MoviesDbContext>(options => options.UseSqlite(connectionString))
            .AddScoped<IMovieRepository, MovieRepository>()
            .AddScoped<ITitleSearch, TitleSearch>()
            .AddScoped<IVectorSearch, VectorSearch>();
}
