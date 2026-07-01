namespace DapperX.Query.Expressions;
public static class OrderByTranslator
{
    public static string Translate(IReadOnlyList<(string column, bool ascending)> segments, Func<string, string> resolveColumn)
    {
        if (!segments.Any()) return string.Empty;
        var parts = segments.Select(s => $"{resolveColumn(s.column)} {(s.ascending ? "ASC" : "DESC")}");
        return " ORDER BY " + string.Join(", ", parts);
    }
}
