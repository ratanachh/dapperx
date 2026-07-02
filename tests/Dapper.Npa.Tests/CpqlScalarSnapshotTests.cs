using Dapper.Npa.Generator.Cpql;

namespace Dapper.Npa.Tests;

public class CpqlScalarSnapshotTests
{
    public static IEnumerable<object[]> ScalarFunctionCases()
    {
        var providers = new[] { "SqlServer", "PostgreSql", "MySql", "Sqlite" };
        var functions = new (string Name, string[] Args, string? CastType)[]
        {
            ("LOWER", ["col"], null),
            ("UPPER", ["col"], null),
            ("TRIM", ["col"], null),
            ("LTRIM", ["col"], null),
            ("RTRIM", ["col"], null),
            ("LENGTH", ["col"], null),
            ("SUBSTRING", ["col", "1", "3"], null),
            ("REPLACE", ["col", "'a'", "'b'"], null),
            ("LEFT", ["col", "5"], null),
            ("RIGHT", ["col", "5"], null),
            ("COALESCE", ["col", "0"], null),
            ("ABS", ["col"], null),
            ("CEILING", ["col"], null),
            ("FLOOR", ["col"], null),
            ("ROUND", ["col", "2"], null),
            ("POWER", ["col", "2"], null),
            ("MOD", ["col", "3"], null),
            ("YEAR", ["col"], null),
            ("MONTH", ["col"], null),
            ("DAY", ["col"], null),
            ("HOUR", ["col"], null),
            ("MINUTE", ["col"], null),
            ("SECOND", ["col"], null),
        };

        foreach (var provider in providers)
        foreach (var fn in functions)
            yield return new object[] { provider, fn.Name, fn.Args, fn.CastType };
    }

    [Theory]
    [MemberData(nameof(ScalarFunctionCases))]
    public void Emit_scalar_function_is_provider_specific(string provider, string name, string[] args, string? castType)
    {
        var sql = CpqlScalarFunctions.Emit(name, args, provider, castType);
        Assert.False(string.IsNullOrWhiteSpace(sql));
        Assert.Contains(args[0], sql);

        switch (name)
        {
            case "LEFT" when provider == "Sqlite":
                Assert.Contains("SUBSTR", sql, StringComparison.OrdinalIgnoreCase);
                break;
            case "RIGHT" when provider == "Sqlite":
                Assert.Contains("SUBSTR", sql, StringComparison.OrdinalIgnoreCase);
                break;
            case "MOD" when provider is "SqlServer":
                Assert.Contains("%", sql);
                break;
            case "MOD" when provider == "Sqlite":
                Assert.Contains("MOD", sql, StringComparison.OrdinalIgnoreCase);
                break;
            case "YEAR" when provider == "Sqlite":
                Assert.Contains("strftime('%Y'", sql, StringComparison.Ordinal);
                break;
            case "MONTH" when provider == "Sqlite":
                Assert.Contains("strftime('%m'", sql, StringComparison.Ordinal);
                break;
            case "DAY" when provider == "Sqlite":
                Assert.Contains("strftime('%d'", sql, StringComparison.Ordinal);
                break;
            case "HOUR" or "MINUTE" or "SECOND" when provider == "Sqlite":
                Assert.Contains("strftime(", sql, StringComparison.Ordinal);
                break;
            case "CEILING" when provider == "PostgreSql":
                Assert.Contains("CEIL", sql, StringComparison.OrdinalIgnoreCase);
                break;
            case "LENGTH" when provider != "PostgreSql":
                Assert.Contains("LEN", sql, StringComparison.OrdinalIgnoreCase);
                break;
            default:
                Assert.Contains(name, sql, StringComparison.OrdinalIgnoreCase);
                break;
        }
    }

    [Theory]
    [InlineData("SqlServer", "NOW", "SYSUTCDATETIME")]
    [InlineData("PostgreSql", "NOW", "CURRENT_TIMESTAMP")]
    [InlineData("SqlServer", "CONCAT", "CONCAT(")]
    [InlineData("Sqlite", "CONCAT", " || ")]
    public void Emit_special_scalars_match_provider(string provider, string name, string expectedFragment)
    {
        var args = name == "CONCAT" ? new[] { "a", "b" } : Array.Empty<string>();
        var sql = CpqlScalarFunctions.Emit(name, args, provider);
        Assert.Contains(expectedFragment, sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Emit_cast_sqlite_date_uses_date_function()
    {
        var sql = CpqlScalarFunctions.Emit("CAST", ["col"], "Sqlite", "DATE");
        Assert.Equal("date(col)", sql);
    }

    [Fact]
    public void Emit_cast_sqlite_string_maps_to_text()
    {
        var sql = CpqlScalarFunctions.Emit("CAST", ["col"], "Sqlite", "STRING");
        Assert.Equal("CAST(col AS TEXT)", sql);
    }
}
