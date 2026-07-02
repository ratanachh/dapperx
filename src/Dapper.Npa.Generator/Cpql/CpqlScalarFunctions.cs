namespace Dapper.Npa.Generator.Cpql;

internal static class CpqlScalarFunctions
{
    private static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        "LOWER", "UPPER", "TRIM", "LTRIM", "RTRIM", "LENGTH", "SUBSTRING", "REPLACE", "LEFT", "RIGHT",
        "COALESCE", "ABS", "CEILING", "FLOOR", "ROUND", "POWER", "MOD",
        "YEAR", "MONTH", "DAY", "HOUR", "MINUTE", "SECOND",
        "NOW", "CURRENT_DATE", "CURRENT_TIMESTAMP", "DATEADD", "DATEDIFF",
        "ROW_NUMBER", "RANK", "DENSE_RANK", "NTILE", "LAG", "LEAD",
        "CAST", "NULLIF", "CONCAT",
        "COUNT", "SUM", "AVG", "MIN", "MAX",
    };

    public static bool IsKnownFunction(string name) => Known.Contains(name);

    public static string Emit(string name, IReadOnlyList<string> args, string provider, string? castType = null)
    {
        var p = provider;
        switch (name.ToUpperInvariant())
        {
            case "LOWER": return $"LOWER({args[0]})";
            case "UPPER": return $"UPPER({args[0]})";
            case "TRIM": return EmitTrim(args[0], p);
            case "LTRIM": return $"LTRIM({args[0]})";
            case "RTRIM": return $"RTRIM({args[0]})";
            case "LENGTH":
                return p == "PostgreSql" ? $"LENGTH({args[0]})" : $"LEN({args[0]})";
            case "SUBSTRING":
                if (args.Count == 2)
                    return p == "SqlServer"
                        ? $"SUBSTRING({args[0]}, {args[1]}, LEN({args[0]}))"
                        : $"SUBSTRING({args[0]} FROM {args[1]})";
                return $"SUBSTRING({args[0]}, {args[1]}, {args[2]})";
            case "REPLACE": return $"REPLACE({args[0]}, {args[1]}, {args[2]})";
            case "LEFT":
                return p == "Sqlite"
                    ? $"SUBSTR({args[0]}, 1, {args[1]})"
                    : $"LEFT({args[0]}, {args[1]})";
            case "RIGHT":
                return p == "Sqlite"
                    ? $"SUBSTR({args[0]}, -({args[1]}))"
                    : $"RIGHT({args[0]}, {args[1]})";
            case "COALESCE": return $"COALESCE({string.Join(", ", args)})";
            case "ABS": return $"ABS({args[0]})";
            case "CEILING": return p == "PostgreSql" ? $"CEIL({args[0]})" : $"CEILING({args[0]})";
            case "FLOOR": return $"FLOOR({args[0]})";
            case "ROUND": return args.Count > 1 ? $"ROUND({args[0]}, {args[1]})" : $"ROUND({args[0]})";
            case "POWER": return $"POWER({args[0]}, {args[1]})";
            case "MOD":
                return p == "SqlServer" ? $"({args[0]} % {args[1]})" : $"MOD({args[0]}, {args[1]})";
            case "YEAR": return EmitDatePart("YEAR", args[0], p);
            case "MONTH": return EmitDatePart("MONTH", args[0], p);
            case "DAY": return EmitDatePart("DAY", args[0], p);
            case "HOUR": return EmitDatePart("HOUR", args[0], p);
            case "MINUTE": return EmitDatePart("MINUTE", args[0], p);
            case "SECOND": return EmitDatePart("SECOND", args[0], p);
            case "NOW":
            case "CURRENT_TIMESTAMP":
                return p == "SqlServer" ? "SYSUTCDATETIME()" : "CURRENT_TIMESTAMP";
            case "CURRENT_DATE":
                return p == "SqlServer" ? "CAST(GETUTCDATE() AS DATE)" : "CURRENT_DATE";
            case "DATEADD":
                return p == "SqlServer"
                    ? $"DATEADD({args[0]}, {args[1]}, {args[2]})"
                    : $"({args[2]} + ({args[1]} || ' ' || {args[0]})::interval)";
            case "DATEDIFF":
                return p == "SqlServer"
                    ? $"DATEDIFF({args[0]}, {args[1]}, {args[2]})"
                    : $"EXTRACT(EPOCH FROM ({args[2]} - {args[1]}))";
            case "CONCAT":
                if (p == "Sqlite")
                    return string.Join(" || ", args);
                return $"CONCAT({string.Join(", ", args)})";
            case "NULLIF": return $"NULLIF({args[0]}, {args[1]})";
            case "CAST":
                if (p == "Sqlite")
                {
                    return (castType ?? "STRING").ToUpperInvariant() switch
                    {
                        "DATE" => $"date({args[0]})",
                        "DATETIME" or "TIMESTAMP" => $"datetime({args[0]})",
                        _ => $"CAST({args[0]} AS {MapCastType(castType ?? "STRING", p)})",
                    };
                }
                return $"CAST({args[0]} AS {MapCastType(castType ?? "STRING", p)})";
            default:
                return $"{name.ToUpperInvariant()}({string.Join(", ", args)})";
        }
    }

    public static string EmitBooleanLiteral(bool value, string provider)
    {
        if (provider == "PostgreSql")
            return value ? "TRUE" : "FALSE";
        return value ? "1" : "0";
    }

    public static string MapCastType(string cpqlType, string provider)
    {
        var t = cpqlType.ToUpperInvariant();
        if (provider == "Sqlite")
        {
            return t switch
            {
                "STRING" => "TEXT",
                "INT" => "INTEGER",
                "LONG" => "INTEGER",
                "DECIMAL" => "REAL",
                "DOUBLE" => "REAL",
                "BOOLEAN" => "INTEGER",
                _ => cpqlType,
            };
        }

        return t switch
        {
            "STRING" => provider == "PostgreSql" ? "TEXT" : "NVARCHAR(MAX)",
            "INT" => "INT",
            "LONG" => "BIGINT",
            "DECIMAL" => "DECIMAL(18,4)",
            "DOUBLE" => provider == "PostgreSql" ? "DOUBLE PRECISION" : "FLOAT",
            "DATE" => "DATE",
            "DATETIME" or "TIMESTAMP" => provider == "SqlServer" ? "DATETIME2" : "TIMESTAMP",
            "BOOLEAN" => provider == "PostgreSql" ? "BOOLEAN" : "BIT",
            _ => cpqlType,
        };
    }

    private static string EmitTrim(string arg, string p) => $"TRIM({arg})";

    private static string EmitDatePart(string part, string arg, string p)
    {
        if (p == "SqlServer")
            return $"DATEPART({part}, {arg})";

        if (p == "Sqlite")
        {
            var format = part switch
            {
                "YEAR" => "%Y",
                "MONTH" => "%m",
                "DAY" => "%d",
                "HOUR" => "%H",
                "MINUTE" => "%M",
                "SECOND" => "%S",
                _ => "%Y",
            };
            return $"CAST(strftime('{format}', {arg}) AS INTEGER)";
        }

        return $"EXTRACT({part} FROM {arg})";
    }
}
