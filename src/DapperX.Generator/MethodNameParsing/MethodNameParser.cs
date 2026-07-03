namespace DapperX.Generator.MethodNameParsing;

using System.Linq;

/// <summary>Parses a derived query method name into a structured <see cref="ParsedDerivedQuery"/>.</summary>
internal static class MethodNameParser
{
    public static ParsedDerivedQuery? TryParse(string methodName, IReadOnlyList<string> pathKeys)
        => TryParseDetailed(methodName, pathKeys).Query;

    public static MethodNameParseResult TryParseDetailed(string methodName, IReadOnlyList<string> pathKeys)
    {
        // Strip Async suffix
        var name = methodName.EndsWith("Async", StringComparison.Ordinal)
            ? methodName.Substring(0, methodName.Length - 5)
            : methodName;

        // --- Subject keyword ---
        var subject = SubjectKind.Find;
        string? subjectRaw = null;
        foreach (var kw in OperatorKeywordTable.SubjectKeywords.OrderByDescending(k => k.Length))
        {
            if (name.StartsWith(kw, StringComparison.Ordinal))
            {
                subjectRaw = kw;
                subject = ParseSubject(kw);
                name = name.Substring(kw.Length);
                break;
            }
        }
        if (subjectRaw is null) return MethodNameParseResult.Trailing();

        // --- Result modifiers before "By" (Distinct, CountDistinct, First{n}, Top{n}) ---
        bool distinct = false;
        int? limitN = null;

        if (name.StartsWith("Distinct", StringComparison.Ordinal))
        { distinct = true; name = name.Substring(8); }
        else if (name.StartsWith("CountDistinct", StringComparison.Ordinal))
        { subject = SubjectKind.CountDistinct; name = name.Substring(13); }

        // First{n} / Top{n}
        foreach (var pfx in new[] { "First", "Top" })
        {
            if (!name.StartsWith(pfx, StringComparison.Ordinal)) continue;
            var rest = name.Substring(pfx.Length);
            var digits = new string(rest.TakeWhile(char.IsDigit).ToArray());
            if (digits.Length > 0) { limitN = int.Parse(digits); name = rest.Substring(digits.Length); }
            else if (pfx == "First" || pfx == "Top") { limitN = 1; name = rest; }
            break;
        }

        // --- By separator ---
        var conditions = new List<ConditionGroup>();
        var orderBySegments = new List<OrderBySegment>();

        var hasAmbiguity = false;

        // FindAllByX → optional "All" between subject and "By"
        if (name.StartsWith("AllBy", StringComparison.Ordinal))
            name = name.Substring(3);

        if (name.StartsWith("By", StringComparison.Ordinal))
        {
            name = name.Substring(2);
            var resolver = new PropertyFirstResolver(pathKeys);
            ParseConditions(name, resolver, conditions, orderBySegments, ref hasAmbiguity, out name);
        }

        // Parse trailing OrderBy modifiers
        ParseOrderBy(name, pathKeys, orderBySegments, ref hasAmbiguity, out name);

        name = StripRuntimeParameterSuffix(name);

        if (hasAmbiguity)
            return MethodNameParseResult.Ambiguous();
        if (name.Length > 0)
            return MethodNameParseResult.Trailing();

        return MethodNameParseResult.Success(new ParsedDerivedQuery
        {
            Subject = subject,
            Distinct = distinct,
            LimitN = limitN,
            Conditions = conditions,
            OrderBySegments = orderBySegments,
        });
    }

    // ─── Condition parsing ────────────────────────────────────────────────────

    private static void ParseConditions(
        string remaining,
        PropertyFirstResolver resolver,
        List<ConditionGroup> conditions,
        List<OrderBySegment> orderBy,
        ref bool hasAmbiguity,
        out string leftover)
    {
        var connector = LogicalConnector.None;

        while (remaining.Length > 0)
        {
            // Stop at OrderBy or known non-condition modifiers
            if (remaining.StartsWith("OrderBy", StringComparison.Ordinal)) break;

            // Logical connector
            if (remaining.StartsWith("And", StringComparison.Ordinal) && remaining.Length > 3 && char.IsUpper(remaining[3]))
            { connector = LogicalConnector.And; remaining = remaining.Substring(3); continue; }
            if (remaining.StartsWith("Or", StringComparison.Ordinal) && remaining.Length > 2 && char.IsUpper(remaining[2]))
            { connector = LogicalConnector.Or; remaining = remaining.Substring(2); continue; }

            if (resolver.IsAmbiguous(remaining))
            {
                hasAmbiguity = true;
                break;
            }

            var propName = resolver.TryMatchProperty(remaining);
            if (propName is null) break;

            remaining = remaining.Substring(propName.Length);

            // Parse operator suffix
            var (op, paramCount) = ParseOperator(ref remaining);

            conditions.Add(new ConditionGroup
            {
                PropertyName = propName,
                Operator = op,
                ParameterCount = paramCount,
                Connector = connector,
            });
            connector = LogicalConnector.None;
        }
        leftover = remaining;
    }

    private static (OperatorKind op, int paramCount) ParseOperator(ref string remaining)
    {
        // Ordered longest-first to avoid partial matches
        var table = new (string kw, OperatorKind op, int parms)[]
        {
            ("IsGreaterThanEqual",  OperatorKind.GreaterThanEqual, 1),
            ("GreaterThanEqual",    OperatorKind.GreaterThanEqual, 1),
            ("IsLessThanEqual",     OperatorKind.LessThanEqual,    1),
            ("LessThanEqual",       OperatorKind.LessThanEqual,    1),
            ("IsGreaterThan",       OperatorKind.GreaterThan,      1),
            ("GreaterThan",         OperatorKind.GreaterThan,      1),
            ("IsLessThan",          OperatorKind.LessThan,         1),
            ("LessThan",            OperatorKind.LessThan,         1),
            ("IsBetween",           OperatorKind.Between,          2),
            ("Between",             OperatorKind.Between,          2),
            ("IsNotContaining",     OperatorKind.NotContaining,    1),
            ("NotContaining",       OperatorKind.NotContaining,    1),
            ("NotContains",         OperatorKind.NotContaining,    1),
            ("IsContaining",        OperatorKind.Containing,       1),
            ("Containing",          OperatorKind.Containing,       1),
            ("Contains",            OperatorKind.Containing,       1),
            ("IsStartingWith",      OperatorKind.StartingWith,     1),
            ("StartingWith",        OperatorKind.StartingWith,     1),
            ("StartsWith",          OperatorKind.StartingWith,     1),
            ("IsEndingWith",        OperatorKind.EndingWith,       1),
            ("EndingWith",          OperatorKind.EndingWith,       1),
            ("EndsWith",            OperatorKind.EndingWith,       1),
            ("IsNotLike",           OperatorKind.NotLike,          1),
            ("NotLike",             OperatorKind.NotLike,          1),
            ("IsLike",              OperatorKind.Like,             1),
            ("Like",                OperatorKind.Like,             1),
            ("IsNotNull",           OperatorKind.IsNotNull,        0),
            ("NotNull",             OperatorKind.IsNotNull,        0),
            ("IsNull",              OperatorKind.IsNull,           0),
            ("Null",                OperatorKind.IsNull,           0),
            ("IsNotIn",             OperatorKind.NotIn,            1),
            ("NotIn",               OperatorKind.NotIn,            1),
            ("IsIn",                OperatorKind.In,               1),
            ("In",                  OperatorKind.In,               1),
            ("IsBefore",            OperatorKind.Before,           1),
            ("Before",              OperatorKind.Before,           1),
            ("IsAfter",             OperatorKind.After,            1),
            ("After",               OperatorKind.After,            1),
            ("IsTrue",              OperatorKind.IsTrue,           0),
            ("True",                OperatorKind.IsTrue,           0),
            ("IsFalse",             OperatorKind.IsFalse,          0),
            ("False",               OperatorKind.IsFalse,          0),
            ("IsNot",               OperatorKind.NotEqual,         1),
            ("Not",                 OperatorKind.NotEqual,         1),
            ("IgnoreCase",          OperatorKind.IgnoreCase,       1),
            ("IgnoringCase",        OperatorKind.IgnoreCase,       1),
            ("AllIgnoreCase",       OperatorKind.AllIgnoreCase,    1),
            ("AllIgnoringCase",     OperatorKind.AllIgnoreCase,    1),
            ("MatchesRegex",        OperatorKind.Regex,            1),
            ("Matches",             OperatorKind.Regex,            1),
            ("Regex",               OperatorKind.Regex,            1),
            ("Equals",              OperatorKind.Equal,            1),
            ("Is",                  OperatorKind.Equal,            1),
        };

        foreach (var (kw, op, parms) in table)
        {
            if (!remaining.StartsWith(kw, StringComparison.Ordinal)) continue;
            var after = remaining.Substring(kw.Length);
            // Must be followed by a word boundary (uppercase, end, or And/Or)
            if (after.Length == 0 || char.IsUpper(after[0]) || after.StartsWith("And") || after.StartsWith("Or"))
            {
                remaining = after;
                return (op, parms);
            }
        }
        return (OperatorKind.Equal, 1); // default: equality
    }

    // ─── OrderBy parsing ─────────────────────────────────────────────────────

    private static void ParseOrderBy(
        string remaining,
        IReadOnlyList<string> pathKeys,
        List<OrderBySegment> result,
        ref bool hasAmbiguity,
        out string leftover)
    {
        var resolver = new PropertyFirstResolver(pathKeys);
        while (remaining.StartsWith("OrderBy", StringComparison.Ordinal))
        {
            remaining = remaining.Substring(7);
            if (resolver.IsAmbiguous(remaining))
            {
                hasAmbiguity = true;
                break;
            }

            var prop = resolver.TryMatchProperty(remaining);
            if (prop is null) break;
            remaining = remaining.Substring(prop.Length);

            bool asc = true;
            if (remaining.StartsWith("Desc", StringComparison.Ordinal)) { asc = false; remaining = remaining.Substring(4); }
            else if (remaining.StartsWith("Asc", StringComparison.Ordinal)) { remaining = remaining.Substring(3); }

            result.Add(new OrderBySegment { PropertyName = prop, Ascending = asc });

            // Then chaining
            if (remaining.StartsWith("Then", StringComparison.Ordinal)) remaining = remaining.Substring(4);
        }
        leftover = remaining;
    }

    /// <summary>
    /// Trailing tokens that disambiguate overloads with runtime parameters (Sort, Pageable, LockMode)
    /// but are not part of the property-path / operator grammar.
    /// </summary>
    private static string StripRuntimeParameterSuffix(string remaining)
    {
        foreach (var suffix in RuntimeParameterSuffixes.OrderByDescending(s => s.Length))
        {
            if (remaining.Equals(suffix, StringComparison.Ordinal))
                return string.Empty;
        }
        return remaining;
    }

    private static readonly string[] RuntimeParameterSuffixes =
    [
        "AllIgnoreCase", "IgnoringCase", "IgnoreCase",
        "IncludingDeleted", "IncludeDeleted",
        "Paged", "Sorted", "Locked",
        "WithGraph",
    ];

    private static SubjectKind ParseSubject(string kw) => kw switch
    {
        "Find" or "Get" or "Query" or "Search" or "Read" => SubjectKind.Find,
        "Stream"   => SubjectKind.Stream,
        "Count"    => SubjectKind.Count,
        "Exists" or "Has" or "Contains" => SubjectKind.Exists,
        "Delete" or "Remove" => SubjectKind.Delete,
        "Insert" or "Add" or "Save" or "Create" => SubjectKind.Insert,
        "Update" or "Modify" => SubjectKind.Update,
        _          => SubjectKind.Find,
    };
}

// ─── Result types ─────────────────────────────────────────────────────────────

internal sealed class ParsedDerivedQuery
{
    public SubjectKind Subject { get; init; }
    public bool Distinct { get; init; }
    public int? LimitN { get; init; }
    public IReadOnlyList<ConditionGroup> Conditions { get; init; } = [];
    public IReadOnlyList<OrderBySegment> OrderBySegments { get; init; } = [];
}

internal sealed class ConditionGroup
{
    public string PropertyName { get; init; } = string.Empty;
    public OperatorKind Operator { get; init; }
    public int ParameterCount { get; init; }
    public LogicalConnector Connector { get; init; }
}

internal sealed class OrderBySegment
{
    public string PropertyName { get; init; } = string.Empty;
    public bool Ascending { get; init; } = true;
}

internal enum SubjectKind { Find, Stream, Count, CountDistinct, Exists, Delete, Insert, Update }
internal enum LogicalConnector { None, And, Or }
internal enum OperatorKind
{
    Equal, NotEqual,
    GreaterThan, GreaterThanEqual, LessThan, LessThanEqual, Between,
    Like, NotLike, Containing, NotContaining, StartingWith, EndingWith,
    In, NotIn, IsNull, IsNotNull, IsTrue, IsFalse,
    Before, After, IgnoreCase, AllIgnoreCase, Regex,
}
