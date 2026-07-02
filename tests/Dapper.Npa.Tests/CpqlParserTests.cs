using Dapper.Npa.Generator.Cpql;

namespace Dapper.Npa.Tests;

public class CpqlParserTests
{
    [Fact]
    public void Parse_select_with_where_and_parameter()
    {
        var ast = CpqlParser.Parse("SELECT p FROM Product p WHERE p.Name = :name");
        Assert.NotNull(ast.Select);
        Assert.Equal("Product", ast.Select!.From.EntityOrCteName);
        Assert.Equal("p", ast.Select.From.Alias);
        Assert.NotNull(ast.Select.Where);
    }

    [Fact]
    public void Parse_delete_statement()
    {
        var ast = CpqlParser.Parse("DELETE FROM Product p WHERE p.Id = :id");
        Assert.NotNull(ast.Delete);
        Assert.Equal("Product", ast.Delete!.EntityName);
    }

    [Fact]
    public void Parse_update_statement()
    {
        var ast = CpqlParser.Parse("UPDATE Product p SET p.Name = :name WHERE p.Id = :id");
        Assert.NotNull(ast.Update);
        Assert.Single(ast.Update!.Assignments);
    }

    [Fact]
    public void Parse_aggregate_count()
    {
        var ast = CpqlParser.Parse("SELECT COUNT(p) FROM Product p WHERE p.Name = :name");
        var item = ast.Select!.SelectList.Items[0].Value;
        Assert.IsType<CpqlAggregateNode>(item);
    }

    [Fact]
    public void Parse_nested_subquery_throws()
    {
        Assert.Throws<CpqlParseException>(() =>
            CpqlParser.Parse("SELECT p FROM Product p WHERE p.Id IN (SELECT c FROM Customer c WHERE c.Id IN (SELECT x FROM Product x))"));
    }
}
