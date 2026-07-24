namespace Comeback.Profile.Infrastructure.Persistence;

/// <summary>
/// Builds LIKE/ILIKE patterns from user input while escaping the wildcard metacharacters
/// (<c>%</c>, <c>_</c>) and the escape character (<c>\</c>) itself, so a search term is matched
/// literally instead of being interpreted as a wildcard. Use together with the escape-character
/// overload of <c>EF.Functions.ILike(column, pattern, LikePattern.EscapeChar)</c>.
/// </summary>
internal static class LikePattern
{
    public const string EscapeChar = "\\";

    public static string Escape(string input)
        => input
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");

    /// <summary>Returns a <c>%term%</c> "contains" pattern with the term's metacharacters escaped.</summary>
    public static string Contains(string input) => $"%{Escape(input)}%";
}
