namespace Comeback.Profile.Infrastructure.Persistence;

/// <summary>
/// Builds LIKE/ILIKE patterns from user input while escaping the wildcard metacharacters
/// (<c>%</c> and <c>_</c>) and the backslash escape character itself, so a search term is matched
/// literally instead of being interpreted as a wildcard. Use together with the escape-character
/// overload of <c>EF.Functions.ILike(column, pattern, LikePattern.EscapeChar)</c>.
/// </summary>
internal static class LikePattern
{
    /// <remarks>
    /// Deliberately <c>static readonly</c> rather than <c>const</c>: a <c>const</c> is inlined into the
    /// LINQ expression tree as a <c>ConstantExpression</c>, which EF Core renders as a SQL literal
    /// (<c>ESCAPE '\'</c>) instead of a parameter. That literal only parses while the server has
    /// <c>standard_conforming_strings</c> on, so parameterising it keeps the query independent of
    /// per-session string-literal settings.
    /// </remarks>
    public static readonly string EscapeChar = "\\";

    public static string Escape(string input)
        => input
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");

    /// <summary>Returns a <c>%term%</c> "contains" pattern with the term's metacharacters escaped.</summary>
    public static string Contains(string input) => $"%{Escape(input)}%";
}
