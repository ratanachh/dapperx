using DapperX.Generator.Cpql;
using DapperX.Generator.Models;
using DapperX.Tests.Fixtures;

namespace DapperX.Tests;

public class EpicFollowUpTests
{
    private static EntityModel ProductModel() => new()
    {
        Namespace = "DapperX.Tests.Fixtures",
        ClassName = "Product",
        TableName = "products",
        Properties =
        [
            new PropertyModel { PropertyName = "Id", ColumnName = "id", IsId = true, ClrTypeName = "int" },
            new PropertyModel { PropertyName = "Name", ColumnName = "name", ClrTypeName = "string" },
        ],
    };

    [Fact]
    public void Translate_left_scalar_sqlite_uses_substr()
    {
        var product = ProductModel();
        var models = new Dictionary<string, EntityModel> { [product.FullyQualifiedName] = product };
        var ast = CpqlParser.Parse("SELECT LEFT(p.Name, 3) FROM Product p");
        var ctx = new CpqlTranslationContext(product, "Sqlite", models);
        var sql = CpqlTranslator.Translate(ast, ctx);
        Assert.Contains("SUBSTR", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Translate_new_projection_emits_column_list()
    {
        var product = ProductModel();
        var models = new Dictionary<string, EntityModel> { [product.FullyQualifiedName] = product };
        var ast = CpqlParser.Parse("SELECT NEW ProductSummary(p.Id, p.Name) FROM Product p");
        var ctx = new CpqlTranslationContext(product, "SqlServer", models);
        var sql = CpqlTranslator.Translate(ast, ctx);
        Assert.Contains("p.id", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("p.name", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NEW", sql);
    }

    [Fact]
    public void Translate_count_for_page_cpql()
    {
        var product = ProductModel();
        var models = new Dictionary<string, EntityModel> { [product.FullyQualifiedName] = product };
        var ast = CpqlParser.Parse("SELECT p FROM Product p WHERE p.Name = :name");
        var ctx = new CpqlTranslationContext(product, "SqlServer", models);
        var countSql = CpqlTranslator.TranslateCount(ast, ctx);
        Assert.Contains("SELECT COUNT(*)", countSql);
        Assert.Contains("FROM products p", countSql);
        Assert.Contains("p.name = @name", countSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OrderRepositoryImpl_emits_insert_graph_async()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            "OrderRepositoryImpl.g.cs"));
        var source = File.ReadAllText(path);
        Assert.Contains("InsertGraphAsync", source);
        Assert.Contains("UpdateGraphAsync", source);
        Assert.Contains("DeleteGraphAsync", source);
        Assert.Contains("OrderItemRepositoryImpl", source);
        Assert.Contains("child.OrderId = root.Id", source);
        Assert.Contains("transaction ??= _connection.BeginTransaction();", source);
        Assert.Contains("if (ownsTransaction) transaction.Rollback();", source);
        Assert.Contains("if (ownsTransaction) transaction.Commit();", source);
    }

    [Fact]
    public void StudentRepositoryImpl_emits_m2m_join_insert_with_dedupe_and_guard()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            "StudentRepositoryImpl.g.cs"));
        var source = File.ReadAllText(path);
        Assert.Contains("JoinInsert_Courses_Sql", source);
        Assert.Contains(".Where(x => x.childId is not null)", source);
        Assert.Contains(".Distinct()", source);
        Assert.Contains("if (joinRows.Count > 0)", source);
    }
}
