namespace Dapper.Npa.Generator.MethodNameParsing;

/// <summary>
/// Implements the property-first longest-match algorithm (Requirements.md Section 5).
/// At each parse position: try the longest prefix that matches a known entity property
/// before attempting operator keyword parsing.
/// This means entity property names always win over same-named keywords.
/// </summary>
internal sealed class PropertyFirstResolver
{
    private readonly IReadOnlyList<string> _propertyNames;

    public PropertyFirstResolver(IReadOnlyList<string> propertyNames)
    {
        _propertyNames = propertyNames;
    }

    /// <summary>
    /// Try to match the longest entity property name starting at <paramref name="segment"/>.
    /// Returns the matching property name or null if no property matches.
    /// </summary>
    public IReadOnlyList<string> GetAllMatches(string remaining)
        => _propertyNames
            .Where(p => remaining.StartsWith(p, StringComparison.Ordinal)
                        && (remaining.Length == p.Length || IsWordBoundary(remaining[p.Length])))
            .OrderByDescending(p => p.Length)
            .ToList();

    public string? TryMatchProperty(string remaining)
        => GetAllMatches(remaining).FirstOrDefault();

    /// <summary>
    /// Two or more path keys match at the same position, or a property path can also be read as
    /// an operator prefix plus a shorter property (e.g. <c>NotDeleted</c> vs <c>Not</c> + <c>Deleted</c>).
    /// </summary>
    public bool IsAmbiguous(string remaining)
    {
        var propertyMatches = GetAllMatches(remaining);
        if (propertyMatches.Count > 1)
            return true;

        var longest = propertyMatches.FirstOrDefault();
        if (longest is null)
            return false;

        foreach (var prefix in AmbiguityOperatorPrefixes)
        {
            if (!remaining.StartsWith(prefix, StringComparison.Ordinal))
                continue;
            if (remaining.Length <= prefix.Length || !IsWordBoundary(remaining[prefix.Length]))
                continue;

            var after = remaining.Substring(prefix.Length);
            var inner = _propertyNames
                .Where(p => after.StartsWith(p, StringComparison.Ordinal)
                            && (after.Length == p.Length || IsWordBoundary(after[p.Length])))
                .OrderByDescending(p => p.Length)
                .FirstOrDefault();
            if (inner is null)
                continue;

            if (string.Equals(prefix + inner, longest, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static readonly string[] AmbiguityOperatorPrefixes = ["All", "Not", "Is"];

    // Word boundary = uppercase letter (PascalCase segment boundary)
    private static bool IsWordBoundary(char c) => char.IsUpper(c);
}
