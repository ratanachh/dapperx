using Dapper.Npa.Generator.Cpql;
using Dapper.Npa.Generator.Models;

namespace Dapper.Npa.Tests;

public class CpqlWindowFunctionTests
{
    private static EntityModel ProductModel() => new()
    {
        Namespace = "Dapper.Npa.Tests.Fixtures",
        ClassName = "Product",
        TableName = "products",
        Properties =
        [
            new PropertyModel { PropertyName = "Id", ColumnName = "id", IsId = true, ClrTypeName = "int" },
            new PropertyModel { PropertyName = "Name", ColumnName = "name", ClrTypeName = "string" },
        ],
    };

    private static Dictionary<string, EntityModel> Models(EntityModel m)
        => new() { [m.FullyQualifiedName] = m };

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    [InlineData("MySql")]
    [InlineData("Sqlite")]
    public void Row_number_over_order_by_translates_with_over_clause(string provider)
    {
        var model = ProductModel();
        var ast = CpqlParser.Parse("SELECT ROW_NUMBER() OVER (ORDER BY p.Name) FROM Product p");
        var sql = CpqlTranslator.Translate(ast, new CpqlTranslationContext(model, provider, Models(model)));
        Assert.Contains("ROW_NUMBER()", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OVER (", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sum_over_partition_by_translates()
    {
        var model = ProductModel();
        var ast = CpqlParser.Parse("SELECT SUM(p.Id) OVER (PARTITION BY p.Name) FROM Product p");
        var sql = CpqlTranslator.Translate(ast, new CpqlTranslationContext(model, "SqlServer", Models(model)));
        Assert.Contains("SUM(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PARTITION BY", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CpqlSemanticValidator_rejects_window_in_where()
    {
        var source = ReadGeneratorSource("Validation/CpqlSemanticValidator.cs");
        Assert.Contains("DPXCPQL012", source);
        Assert.Contains("Window functions are not allowed in WHERE or HAVING", source);
    }

    private static string ReadGeneratorSource(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Dapper.Npa.Generator", relativePath));
        return File.ReadAllText(path);
    }
}
