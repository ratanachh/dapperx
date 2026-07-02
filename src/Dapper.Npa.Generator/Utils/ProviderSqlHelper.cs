namespace Dapper.Npa.Generator.Utils;

using System.Text.RegularExpressions;

/// <summary>Provider-specific SQL literal forms (compile-time only).</summary>
internal static class ProviderSqlHelper
{
    public static string InClause(string column, string paramName, string provider)
        => provider == "PostgreSql"
            ? $"{column} = ANY(@{paramName})"
            : $"{column} IN @{paramName}";

    public static string NotInClause(string column, string paramName, string provider)
        => provider == "PostgreSql"
            ? $"NOT ({column} = ANY(@{paramName}))"
            : $"{column} NOT IN @{paramName}";

    public static string BooleanLiteral(bool value, string provider)
        => provider == "PostgreSql"
            ? value ? "true" : "false"
            : value ? "1" : "0";

    public static string SoftDeleteActivePredicate(string column, string provider, string? tableAlias = null)
    {
        var col = string.IsNullOrEmpty(tableAlias) ? column : $"{tableAlias}.{column}";
        return $"{col} = {BooleanLiteral(false, provider)}";
    }

    public static string NormalizeBooleanLiteralsInCondition(string condition, string provider)
    {
        if (provider != "PostgreSql")
            return condition;
        return Regex.Replace(
            Regex.Replace(condition, @"=\s*0\b", "= false"),
            @"=\s*1\b", "= true");
    }
}
