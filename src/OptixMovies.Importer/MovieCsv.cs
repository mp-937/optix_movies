using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

using OptixMovies.Core;

namespace OptixMovies.Importer;

/// <summary>Reads movies from the dataset's CSV export.</summary>
public static class MovieCsv
{
    /// <exception cref="InvalidDataException">The CSV is malformed, lacks a column, or has an invalid value.</exception>
    public static IReadOnlyList<Movie> Read(TextReader reader)
    {
        // Rows end with LF or CRLF, but a few overviews contain bare CRs, one of them outside quotes,
        // so only LF may end a row.
        using var rows = new StringReader(reader.ReadToEnd().Replace("\r\n", "\n"));
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            NewLine = "\n",
            DetectColumnCountChanges = true,
        };
        using var csv = new CsvReader(rows, configuration);
        var movies = new List<Movie>();

        try
        {
            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                movies.Add(ReadMovie(csv));
            }
        }
        catch (CsvHelperException e)
        {
            throw new InvalidDataException($"Row {csv.Parser.Row}: the CSV is malformed.", e);
        }

        return movies;
    }

    private static Movie ReadMovie(CsvReader csv) => new(
        Title: Field(csv, "Title", Text),
        Overview: Field(csv, "Overview", Text),
        ReleaseDate: Field(csv, "Release_Date", Date),
        Popularity: Field(csv, "Popularity", Number),
        VoteCount: Field(csv, "Vote_Count", Count),
        VoteAverage: Field(csv, "Vote_Average", Number),
        OriginalLanguage: Field(csv, "Original_Language", Text),
        Genres: Field(csv, "Genre", GenreList),
        PosterUrl: Field(csv, "Poster_Url", Url));

    private static T Field<T>(CsvReader csv, string column, Func<string, T> parse)
    {
        if (!csv.TryGetField(column, out string? text) || text is null)
        {
            throw new InvalidDataException($"The CSV has no {column} column.");
        }

        try
        {
            return parse(text);
        }
        catch (Exception e) when (e is FormatException or OverflowException)
        {
            throw new InvalidDataException($"Row {csv.Parser.Row}: '{text}' is not a valid {column}.", e);
        }
    }

    private static string Text(string text) =>
        string.IsNullOrWhiteSpace(text) ? throw new FormatException() : text.Trim().ReplaceLineEndings("\n");

    private static DateOnly Date(string text) =>
        DateOnly.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static double Number(string text) =>
        double.Parse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

    private static int Count(string text) =>
        int.Parse(text, NumberStyles.None, CultureInfo.InvariantCulture);

    private static string[] GenreList(string text) =>
        text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) is { Length: > 0 } genres
            ? genres
            : throw new FormatException();

    private static Uri Url(string text) => new(text, UriKind.Absolute);
}
