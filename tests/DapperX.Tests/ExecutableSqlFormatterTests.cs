using System.Globalization;
using DapperX.Core.Enums;
using DapperX.Runtime.Logging;

namespace DapperX.Tests;

public class ExecutableSqlFormatterTests
{
    [Fact]
    public void Format_string_escapes_single_quotes()
    {
        var sql = ExecutableSqlFormatter.Format(
            "SELECT * FROM t WHERE name = @name",
            new Dictionary<string, object?> { ["name"] = "it's" },
            DatabaseProvider.SqlServer);

        Assert.Equal("SELECT * FROM t WHERE name = 'it''s'", sql);
    }

    [Theory]
    [InlineData(42)]
    [InlineData(99L)]
    [InlineData((short)7)]
    [InlineData((byte)3)]
    public void Format_integers_unquoted_invariant(object value)
    {
        var sql = ExecutableSqlFormatter.Format(
            "SELECT @n",
            new Dictionary<string, object?> { ["n"] = value },
            DatabaseProvider.SqlServer);

        Assert.Equal($"SELECT {Convert.ToString(value, CultureInfo.InvariantCulture)}", sql);
    }

    [Fact]
    public void Format_decimal_uses_invariant_separator()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            var sql = ExecutableSqlFormatter.Format(
                "SELECT @p",
                new Dictionary<string, object?> { ["p"] = 45.67m },
                DatabaseProvider.SqlServer);

            Assert.Equal("SELECT 45.67", sql);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData(DatabaseProvider.SqlServer, true, "1")]
    [InlineData(DatabaseProvider.SqlServer, false, "0")]
    [InlineData(DatabaseProvider.MySql, true, "1")]
    [InlineData(DatabaseProvider.PostgreSql, true, "TRUE")]
    [InlineData(DatabaseProvider.PostgreSql, false, "FALSE")]
    public void Format_bool_is_dialect_aware(DatabaseProvider provider, bool value, string expected)
    {
        var sql = ExecutableSqlFormatter.Format(
            "SELECT @b",
            new Dictionary<string, object?> { ["b"] = value },
            provider);

        Assert.Equal($"SELECT {expected}", sql);
    }

    [Fact]
    public void Format_null_guid_datetime_and_in_list()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var dt = new DateTime(2024, 1, 15, 9, 30, 0);
        var sql = ExecutableSqlFormatter.Format(
            "SELECT @a, @g, @d, @ids",
            new Dictionary<string, object?>
            {
                ["a"] = null,
                ["g"] = id,
                ["d"] = dt,
                ["ids"] = new object?[] { 1, "x", null },
            },
            DatabaseProvider.SqlServer);

        Assert.Contains("NULL", sql);
        Assert.Contains(id.ToString(), sql);
        Assert.Contains("2024-01-15 09:30:00", sql);
        Assert.Contains("(1, 'x', NULL)", sql);
    }
}
