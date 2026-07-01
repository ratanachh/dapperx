using System.Text.RegularExpressions;

namespace DapperX.Runtime.Query;

/// <summary>Inserts SQL Server table hints immediately after the FROM table reference.</summary>
public static partial class SqlServerTableHint
{
    [GeneratedRegex(@"\sFROM\s+([^\s]+)(?:\s+(\w+))?(?=\s+(?:WHERE|JOIN|INNER|LEFT|RIGHT|OUTER|CROSS|ORDER|GROUP|HAVING)\b|\s*$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FromTableRegex();

    public static string Apply(string sql, string hint)
    {
        if (string.IsNullOrWhiteSpace(hint))
            return sql;

        return FromTableRegex().Replace(sql, match =>
        {
            var table = match.Groups[1].Value;
            var alias = match.Groups[2].Success ? $" {match.Groups[2].Value}" : string.Empty;
            return $" FROM {table}{alias} {hint}";
        }, 1);
    }
}
