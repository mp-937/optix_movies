using Microsoft.EntityFrameworkCore;

using OptixMovies.Core;

namespace OptixMovies.Data.Sqlite;

internal sealed class TitleSearch(MoviesDbContext db) : ITitleSearch
{
    public async Task<IReadOnlyList<TitleSuggestion>> SuggestAsync(
        TitleQuery query, CancellationToken cancellationToken = default)
    {
        var movies = db.Movies.AsQueryable();

        // Search words are normalised to letters and digits, so they can't contain LIKE wildcards.
        foreach (var word in query.Words)
        {
            var startsAWord = $"% {word}%";
            movies = movies.Where(movie => EF.Functions.Like(" " + movie.SearchTitle, startsAWord));
        }

        return await movies
            .OrderBy(movie => movie.SearchTitle.StartsWith(query.Text) ? 0 : 1)
            .ThenByDescending(movie => movie.Popularity)
            .ThenBy(movie => movie.Id)
            .Take(query.Limit)
            .Select(movie => new TitleSuggestion(movie.Id, movie.Title, movie.ReleaseDate.Year))
            .ToListAsync(cancellationToken);
    }
}
