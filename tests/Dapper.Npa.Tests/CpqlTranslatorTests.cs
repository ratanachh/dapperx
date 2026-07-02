using Dapper.Npa.Generator.Cpql;
using Dapper.Npa.Generator.Models;

namespace Dapper.Npa.Tests;

public class CpqlTranslatorTests
{
    private static EntityModel ProductModel() => new()
    {
        Namespace = "Dapper.Npa.Tests.Fixtures",
        ClassName = "Product",
        TableName = "products",
        Properties =
        [
            new PropertyModel { PropertyName = "Id", ColumnName = "id", IsId = true },
            new PropertyModel { PropertyName = "Name", ColumnName = "name" },
        ],
        Relationships =
        [
            new RelationshipModel
            {
                PropertyName = "Customer",
                Kind = "ManyToOne",
                ForeignKeyColumn = "customer_id",
                TargetEntity = "Dapper.Npa.Tests.Fixtures.Customer",
                ChildEntityFqn = "Dapper.Npa.Tests.Fixtures.Customer",
            },
        ],
    };

    private static EntityModel CustomerModel() => new()
    {
        Namespace = "Dapper.Npa.Tests.Fixtures",
        ClassName = "Customer",
        TableName = "customers",
        Properties =
        [
            new PropertyModel { PropertyName = "Id", ColumnName = "id", IsId = true },
            new PropertyModel { PropertyName = "Name", ColumnName = "name" },
        ],
    };

    [Fact]
    public void Translate_select_where_parameter()
    {
        var product = ProductModel();
        var models = new Dictionary<string, EntityModel>
        {
            [product.FullyQualifiedName] = product,
            [CustomerModel().FullyQualifiedName] = CustomerModel(),
        };
        var ast = CpqlParser.Parse("SELECT p FROM Product p WHERE p.Name = :name");
        var ctx = new CpqlTranslationContext(product, "SqlServer", models);
        var sql = CpqlTranslator.Translate(ast, ctx);
        Assert.Contains("FROM products p", sql);
        Assert.Contains("p.name = @name", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Translate_implicit_join_path()
    {
        var product = ProductModel();
        var customer = CustomerModel();
        var models = new Dictionary<string, EntityModel>
        {
            [product.FullyQualifiedName] = product,
            [customer.FullyQualifiedName] = customer,
        };
        var ast = CpqlParser.Parse("SELECT p FROM Product p WHERE p.Customer.Name = :name");
        var ctx = new CpqlTranslationContext(product, "SqlServer", models);
        var sql = CpqlTranslator.Translate(ast, ctx);
        Assert.Contains("INNER JOIN", sql);
        Assert.Contains("customers", sql);
    }

    [Fact]
    public void Scalar_lower_emits_sql_server_function()
    {
        var product = ProductModel();
        var models = new Dictionary<string, EntityModel> { [product.FullyQualifiedName] = product };
        var ast = CpqlParser.Parse("SELECT LOWER(p.Name) FROM Product p");
        var ctx = new CpqlTranslationContext(product, "SqlServer", models);
        var sql = CpqlTranslator.Translate(ast, ctx);
        Assert.Contains("LOWER(", sql, StringComparison.OrdinalIgnoreCase);
    }
}
