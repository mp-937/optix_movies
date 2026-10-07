namespace OptixMovies.Core;

/// <summary>The text that stands for a movie in semantic search.</summary>
public static class MovieText
{
    /// <summary>
    /// Joins the title, genres and overview, as in "Toy Story. Animation, Comedy. Led by Woody, …". The genres say
    /// what overviews rarely do, such as that a movie is animated.
    /// </summary>
    public static string ForEmbedding(Movie movie) =>
        string.Join(". ", new[] { movie.Title, string.Join(", ", movie.Genres), movie.Overview }
            .Where(part => part.Length > 0));
}
