using Dapper.Npa.Runtime.Query;

namespace Dapper.Npa.Tests;

public class SqlServerTableHintTests
{
    [Fact]
    public void Apply_inserts_hint_after_table_before_where()
    {
        const string sql = "SELECT id FROM catalog_products WHERE sku = @sku";
        const string hint = "WITH (UPDLOCK, ROWLOCK)";
        var result = SqlServerTableHint.Apply(sql, hint);
        Assert.Equal("SELECT id FROM catalog_products WITH (UPDLOCK, ROWLOCK) WHERE sku = @sku", result);
        Assert.True(result.IndexOf(hint, StringComparison.Ordinal) < result.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Apply_inserts_hint_after_table_alias_before_where()
    {
        const string sql = "SELECT e.id FROM catalog_products e WHERE e.sku = @sku";
        const string hint = "WITH (HOLDLOCK, ROWLOCK)";
        var result = SqlServerTableHint.Apply(sql, hint);
        Assert.Equal("SELECT e.id FROM catalog_products e WITH (HOLDLOCK, ROWLOCK) WHERE e.sku = @sku", result);
    }

    [Fact]
    public void Apply_does_not_append_hint_at_end_of_sql()
    {
        const string sql = "SELECT id FROM catalog_products WHERE sku = @sku";
        const string hint = "WITH (UPDLOCK, ROWLOCK)";
        var result = SqlServerTableHint.Apply(sql, hint);
        Assert.False(result.TrimEnd().EndsWith(hint, StringComparison.Ordinal));
    }
}
