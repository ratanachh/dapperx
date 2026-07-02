using Dapper.Npa.Tests.Fixtures;

namespace Dapper.Npa.Tests;

public class CpqlGenerationTests
{
    private static string ReadGeneratedProductRepository()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "Dapper.Npa.Generator", "Dapper.Npa.Generator.DapperXSourceGenerator",
            "ProductRepositoryImpl.g.cs"));
        return File.ReadAllText(path);
    }

    [Theory]
    [InlineData(nameof(IProductRepository.FindByNameCpqlAsync))]
    [InlineData(nameof(IProductRepository.FindByCustomerNameCpqlAsync))]
    [InlineData(nameof(IProductRepository.CountByNameCpqlAsync))]
    public void ProductRepositoryImpl_implements_cpql_query_methods(string methodName)
    {
        Assert.NotNull(typeof(IProductRepository).GetMethod(methodName));
        Assert.NotNull(typeof(ProductRepositoryImpl).GetMethod(methodName));
    }

    [Fact]
    public void ProductRepositoryImpl_emits_translated_cpql_sql_literal()
    {
        var source = ReadGeneratedProductRepository();
        Assert.Contains("FindByNameCpqlAsync", source);
        Assert.Contains("FROM products", source);
        Assert.DoesNotContain("DPX025", source);
    }

    [Fact]
    public void ProductRepositoryImpl_cpql_method_has_method_name_literal()
    {
        var source = ReadGeneratedProductRepository();
        Assert.Contains("const string MethodName = \"FindByNameCpqlAsync\"", source);
    }
}
