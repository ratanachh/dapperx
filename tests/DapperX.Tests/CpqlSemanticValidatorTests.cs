using DapperX.Generator.Cpql;
using DapperX.Generator.Models;

namespace DapperX.Tests;

public class CpqlSemanticValidatorTests
{
    private static EntityModel ProductModel() => new()
    {
        Namespace = "DapperX.Tests.Fixtures",
        ClassName = "Product",
        TableName = "products",
        Properties =
        [
            new PropertyModel { PropertyName = "Id", ColumnName = "id", IsId = true, ClrTypeName = "int", Nullable = false },
            new PropertyModel { PropertyName = "Name", ColumnName = "name", ClrTypeName = "string" },
        ],
        Relationships =
        [
            new RelationshipModel
            {
                PropertyName = "Customer",
                Kind = "ManyToOne",
                ForeignKeyColumn = "customer_id",
                TargetEntity = "DapperX.Tests.Fixtures.Customer",
                ChildEntityFqn = "DapperX.Tests.Fixtures.Customer",
            },
        ],
    };

    [Fact]
    public void Parser_accepts_nulls_first_last_in_order_by()
    {
        var ast = CpqlParser.Parse("SELECT p FROM Product p ORDER BY p.Name NULLS FIRST, p.Id NULLS LAST");
        Assert.NotNull(ast.Select);
        Assert.Equal(2, ast.Select!.OrderBy.Count);
        Assert.True(ast.Select.OrderBy[0].NullsFirst);
        Assert.False(ast.Select.OrderBy[1].NullsFirst);
    }

    [Fact]
    public void Parser_accepts_exists_and_in_subquery_predicates()
    {
        var ast = CpqlParser.Parse("""
            SELECT p FROM Product p
            WHERE EXISTS (SELECT c FROM Customer c WHERE c.Id = p.Customer.Id)
              AND p.Id IN (SELECT p2.Id FROM Product p2 WHERE p2.Name = :name)
            """);
        Assert.NotNull(ast.Select?.Where);
    }

    [Fact]
    public void Parser_accepts_case_without_else()
    {
        var ast = CpqlParser.Parse("""
            SELECT CASE WHEN p.Name = :name THEN 1 ELSE 0 END FROM Product p
            """);
        var item = ast.Select!.SelectList.Items[0].Value as CpqlCaseNode;
        Assert.NotNull(item);
        Assert.NotNull(item!.Else);
    }

    [Fact]
    public void Parser_accepts_new_expression()
    {
        var ast = CpqlParser.Parse("SELECT NEW ProductSummary(p.Id, p.Name) FROM Product p");
        var item = ast.Select!.SelectList.Items[0].Value as CpqlNewExprNode;
        Assert.NotNull(item);
        Assert.Equal("ProductSummary", item!.TypeName);
        Assert.Equal(2, item.Arguments.Count);
    }

    [Fact]
    public void Translate_exists_subquery_emits_sql()
    {
        var product = ProductModel();
        var customer = new EntityModel
        {
            Namespace = "DapperX.Tests.Fixtures",
            ClassName = "Customer",
            TableName = "customers",
            Properties =
            [
                new PropertyModel { PropertyName = "Id", ColumnName = "id", IsId = true },
                new PropertyModel { PropertyName = "Name", ColumnName = "name" },
            ],
        };
        var models = new Dictionary<string, EntityModel>
        {
            [product.FullyQualifiedName] = product,
            [customer.FullyQualifiedName] = customer,
        };
        var ast = CpqlParser.Parse("""
            SELECT p FROM Product p
            WHERE EXISTS (SELECT c FROM Customer c WHERE c.Name = :name)
            """);
        var sql = CpqlTranslator.Translate(ast, new CpqlTranslationContext(product, "SqlServer", models));
        Assert.Contains("EXISTS", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FROM customers", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Translate_in_subquery_emits_sql()
    {
        var product = ProductModel();
        var models = new Dictionary<string, EntityModel> { [product.FullyQualifiedName] = product };
        var ast = CpqlParser.Parse("""
            SELECT p FROM Product p
            WHERE p.Id IN (SELECT p2.Id FROM Product p2 WHERE p2.Name = :name)
            """);
        var sql = CpqlTranslator.Translate(ast, new CpqlTranslationContext(product, "SqlServer", models));
        Assert.Contains(" IN ", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductRepositoryImpl_cpql_methods_compile_clean()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            "ProductRepositoryImpl.g.cs"));
        var source = File.ReadAllText(path);
        Assert.Contains("FindByNameCpqlAsync", source);
        Assert.Contains("CountByNameCpqlAsync", source);
        Assert.Contains("const string sql =", source);
    }
}
