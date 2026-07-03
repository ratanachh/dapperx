namespace DapperX.Generator.MethodNameParsing;

/// <summary>
/// Compile-time table of all reserved derived-query operator keywords.
/// Used by PropertyFirstResolver to detect when a method name segment
/// is a keyword vs a property name.
/// </summary>
internal static class OperatorKeywordTable
{
    // Subject prefixes (before "By")
    public static readonly HashSet<string> SubjectKeywords = new(StringComparer.Ordinal)
    {
        "Find","Get","Query","Search","Read","Stream",
        "Count","Exists","Has","Contains",
        "Delete","Remove","Insert","Add","Save","Create","Update","Modify"
    };

    // All operator keywords that appear AFTER "By" in method names.
    // Property names that collide with these trigger a DPX015 warning.
    public static readonly HashSet<string> OperatorKeywords = new(StringComparer.Ordinal)
    {
        // Comparison
        "Is","Not","IsNot","Equals","GreaterThan","IsGreaterThan",
        "GreaterThanEqual","IsGreaterThanEqual","LessThan","IsLessThan",
        "LessThanEqual","IsLessThanEqual","Between","IsBetween",
        // String
        "Like","IsLike","NotLike","IsNotLike",
        "Containing","Contains","IsContaining","NotContaining","NotContains","IsNotContaining",
        "StartingWith","StartsWith","IsStartingWith",
        "EndingWith","EndsWith","IsEndingWith",
        // Collections
        "In","IsIn","NotIn","IsNotIn",
        // Null
        "Null","IsNull","NotNull","IsNotNull",
        // Boolean
        "True","IsTrue","False","IsFalse",
        // Date
        "Before","IsBefore","After","IsAfter",
        // Regex
        "Regex","Matches","MatchesRegex","IsMatches",
        // Case
        "IgnoreCase","IgnoringCase","AllIgnoreCase","AllIgnoringCase",
        // Logical
        "And","Or",
        // Modifiers
        "OrderBy","Then","Asc","Desc",
        "Distinct","CountDistinct",
        "First","Top",
    };

    public static bool IsOperator(string segment) => OperatorKeywords.Contains(segment);
    public static bool IsSubject(string segment) => SubjectKeywords.Contains(segment);
}
