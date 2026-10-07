using Microsoft.EntityFrameworkCore;

namespace OptixMovies.Data.Sqlite;

internal sealed class MoviesDbContext(DbContextOptions<MoviesDbContext> options) : DbContext(options)
{
    public DbSet<MovieEntity> Movies => Set<MovieEntity>();

    public DbSet<GenreEntity> Genres => Set<GenreEntity>();

    public DbSet<EmbeddingStatusEntity> EmbeddingStatus => Set<EmbeddingStatusEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MovieEntity>(movies =>
        {
            movies.HasIndex(movie => movie.Title);
            movies.HasIndex(movie => movie.ReleaseDate);
            movies.HasMany(movie => movie.Genres)
                .WithMany()
                .UsingEntity(
                    "MovieGenres",
                    toGenre => toGenre.HasOne(typeof(GenreEntity)).WithMany().HasForeignKey("GenreId"),
                    toMovie => toMovie.HasOne(typeof(MovieEntity)).WithMany().HasForeignKey("MovieId"),
                    join => join.HasKey("MovieId", "GenreId"));
        });

        modelBuilder.Entity<GenreEntity>()
            .HasIndex(genre => genre.Name)
            .IsUnique();
    }
}
