namespace OptixMovies.Data.Sqlite;

internal sealed class MovieEntity
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string SearchTitle { get; set; }
    public required string Overview { get; set; }
    public DateOnly ReleaseDate { get; set; }
    public double Popularity { get; set; }
    public int VoteCount { get; set; }
    public double VoteAverage { get; set; }
    public required string OriginalLanguage { get; set; }
    public required Uri PosterUrl { get; set; }
    public List<GenreEntity> Genres { get; set; } = [];
}
