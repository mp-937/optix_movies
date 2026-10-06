using System.Globalization;
using System.Text;

namespace OptixMovies.Core;

/// <summary>
/// Reduces text to lowercase words of letters and digits, without accents,
/// so that "Pokémon: The King's Man" and "pokemon the kings man" match.
/// </summary>
public static class SearchText
{
    public static string Normalize(string text)
    {
        var words = new StringBuilder(text.Length);

        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (char.IsLetterOrDigit(c))
            {
                words.Append(char.ToLowerInvariant(c));
            }
            else if (IsWordBreak(c) && words.Length > 0 && words[^1] != ' ')
            {
                words.Append(' ');
            }
        }

        return words.ToString().TrimEnd();
    }

    // Accents (split off by FormD), apostrophes and full stops vanish: "Pokémon" becomes "pokemon", "King's" "kings"
    // and "L.A." "la". Anything else that isn't a letter or digit separates words: "Spider-Man" becomes "spider man".
    private static bool IsWordBreak(char c) =>
        CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && c is not ('\'' or '’' or '.');
}
