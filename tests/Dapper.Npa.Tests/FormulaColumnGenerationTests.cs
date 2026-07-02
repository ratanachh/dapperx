namespace Dapper.Npa.Tests;

public class FormulaColumnGenerationTests
{
    private static string ReadGenerated(string implFileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "Dapper.Npa.Generator", "Dapper.Npa.Generator.DapperXSourceGenerator",
            implFileName));
        return File.ReadAllText(path);
    }

    [Fact]
    public void ProductRepositoryImpl_select_sql_supports_embedded_columns()
    {
        var source = ReadGenerated("ProductRepositoryImpl.g.cs");
        Assert.Contains("address_city", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormulaOrderRepositoryImpl_select_includes_formula_expression()
    {
        var source = ReadGenerated("FormulaOrderRepositoryImpl.g.cs");
        Assert.Contains("SELECT COUNT(*) FROM order_items oi WHERE oi.order_id = formula_orders.id", source);
        Assert.Contains("AS item_count", source);
    }

    [Fact]
    public void FormulaOrderRepositoryImpl_insert_and_update_exclude_formula_column()
    {
        var source = ReadGenerated("FormulaOrderRepositoryImpl.g.cs");
        var insertSql = ExtractSqlConstant(source, "InsertSql");
        var updateSql = ExtractSqlConstant(source, "UpdateSql");
        Assert.DoesNotContain("item_count", insertSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@ItemCount", source);
        Assert.DoesNotContain("item_count =", updateSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormulaOrderRepositoryImpl_resolve_column_excludes_formula_property()
    {
        var source = ReadGenerated("FormulaOrderRepositoryImpl.g.cs");
        var resolveStart = source.IndexOf("public static string ResolveColumn", StringComparison.Ordinal);
        Assert.True(resolveStart >= 0);
        var resolveEnd = source.IndexOf("protected override string SelectByIdSql", resolveStart, StringComparison.Ordinal);
        var body = source.Substring(resolveStart, resolveEnd - resolveStart);
        Assert.Contains("nameof(Dapper.Npa.Tests.Fixtures.FormulaOrder.Id)", body);
        Assert.Contains("nameof(Dapper.Npa.Tests.Fixtures.FormulaOrder.Code)", body);
        Assert.DoesNotContain("ItemCount", body);
    }

    private static string ExtractSqlConstant(string source, string constantName)
    {
        var marker = $"protected override string {constantName} => \"";
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Missing {constantName} in generated source.");
        start += marker.Length;
        var end = source.IndexOf("\"; SELECT SCOPE_IDENTITY()", start, StringComparison.Ordinal);
        if (end < 0)
            end = source.IndexOf("\"; SELECT", start, StringComparison.Ordinal);
        if (end < 0)
            end = source.IndexOf("\";", start, StringComparison.Ordinal);
        Assert.True(end > start, $"Unterminated {constantName} literal.");
        return source.Substring(start, end - start);
    }
}
