using DapperX.Core.Enums;

namespace DapperX.Runtime.Logging;

using System.Collections;
using System.Globalization;
using DapperX.Core.Enums;

/// <summary>
/// Substitutes @param values into SQL string for human-readable output.
/// Output is NEVER passed to Dapper — logging only.
/// </summary>
public static class ExecutableSqlFormatter
{
    public static string Format(string sql, IReadOnlyDictionary<string, object?> parameters, DatabaseProvider provider)
    {
        var result = sql;
        foreach (var name in parameters.Keys.OrderByDescending(static k => k.Length))
        {
            if (parameters.TryGetValue(name, out var value))
                result = result.Replace($"@{name}", FormatValue(value, provider), StringComparison.Ordinal);
        }

        return result;
    }

    private static string FormatValue(object? value, DatabaseProvider provider) => value switch
    {
        null => "NULL",
        string s => $"'{s.Replace("'", "''", StringComparison.Ordinal)}'",
        char c => $"'{c}'",
        bool b => provider == DatabaseProvider.PostgreSql ? (b ? "TRUE" : "FALSE") : (b ? "1" : "0"),
        byte n => n.ToString(CultureInfo.InvariantCulture),
        short n => n.ToString(CultureInfo.InvariantCulture),
        int n => n.ToString(CultureInfo.InvariantCulture),
        long n => n.ToString(CultureInfo.InvariantCulture),
        DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
        DateTimeOffset dto => $"'{dto:yyyy-MM-dd HH:mm:ss}'",
        Guid g => $"'{g}'",
        float f => f.ToString("G", CultureInfo.InvariantCulture),
        double d => d.ToString("G", CultureInfo.InvariantCulture),
        decimal m => m.ToString(CultureInfo.InvariantCulture),
        IEnumerable enumerable => FormatCollection(enumerable, provider),
        _ => value.ToString() ?? "NULL",
    };

    private static string FormatCollection(IEnumerable items, DatabaseProvider provider) // non-generic IN lists
        => "(" + string.Join(", ", items.Cast<object?>().Select(i => FormatValue(i, provider))) + ")";
}
