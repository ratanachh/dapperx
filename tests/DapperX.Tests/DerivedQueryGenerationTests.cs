using System.Reflection;
using DapperX.Tests.Fixtures;

namespace DapperX.Tests;

public class DerivedQueryGenerationTests
{
    private static string ReadGeneratedProductRepository()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            "ProductRepositoryImpl.g.cs"));
        return File.ReadAllText(path);
    }

    [Theory]
    [InlineData(nameof(IProductRepository.FindAllByNameAsync))]
    [InlineData(nameof(IProductRepository.ExistsByNameAsync))]
    [InlineData(nameof(IProductRepository.CountByNameAsync))]
    [InlineData(nameof(IProductRepository.FindByNameOrderByIdDescAsync))]
    [InlineData(nameof(IProductRepository.FindByAddressCityAsync))]
    [InlineData(nameof(IProductRepository.FindByCustomerNameAsync))]
    [InlineData(nameof(IProductRepository.FindByCustomerIdAsync))]
    [InlineData(nameof(IProductRepository.CreateAsync))]
    [InlineData(nameof(IProductRepository.FindByNameNativeAsync))]
    public void ProductRepositoryImpl_implements_derived_query_methods(string methodName)
    {
        var ifaceMethod = typeof(IProductRepository).GetMethod(methodName);
        var implMethod = typeof(ProductRepositoryImpl).GetMethod(methodName);
        Assert.NotNull(ifaceMethod);
        Assert.NotNull(implMethod);
        Assert.True(implMethod!.IsPublic);
    }

    [Fact]
    public void ProductRepositoryImpl_implements_FindByNameAsync_single()
    {
        var parameterTypes = new[] { typeof(string) };
        Assert.NotNull(typeof(IProductRepository).GetMethod(nameof(IProductRepository.FindByNameAsync), parameterTypes));
        Assert.NotNull(typeof(ProductRepositoryImpl).GetMethod(nameof(IProductRepository.FindByNameAsync), parameterTypes));
    }

    [Fact]
    public void ProductRepositoryImpl_implements_FindByNameAsync_with_sort()
    {
        var parameterTypes = new[] { typeof(string), typeof(DapperX.Abstractions.Sorting.Sort) };
        Assert.NotNull(typeof(IProductRepository).GetMethod(nameof(IProductRepository.FindByNameAsync), parameterTypes));
        Assert.NotNull(typeof(ProductRepositoryImpl).GetMethod(nameof(IProductRepository.FindByNameAsync), parameterTypes));
    }

    [Fact]
    public void ProductRepositoryImpl_implements_FindByNameAsync_with_sort_and_pageable()
    {
        var parameterTypes = new[]
        {
            typeof(string),
            typeof(DapperX.Abstractions.Sorting.Sort),
            typeof(DapperX.Abstractions.Paging.Pageable),
        };
        Assert.NotNull(typeof(IProductRepository).GetMethod(nameof(IProductRepository.FindByNameAsync), parameterTypes));
        Assert.NotNull(typeof(ProductRepositoryImpl).GetMethod(nameof(IProductRepository.FindByNameAsync), parameterTypes));
    }

    [Fact]
    public void ProductRepositoryImpl_implements_bulk_insert_overload()
    {
        var parameterTypes = new[] { typeof(IEnumerable<Product>) };
        Assert.NotNull(typeof(IProductRepository).GetMethod(nameof(IProductRepository.InsertAsync), parameterTypes));
        Assert.NotNull(typeof(ProductRepositoryImpl).GetMethod(nameof(IProductRepository.InsertAsync), parameterTypes));
    }

    [Fact]
    public void ProductRepositoryImpl_FindByNameWithSort_uses_qualified_product_name()
    {
        var source = ReadGeneratedProductRepository();
        Assert.Contains("WHERE e.name = @name", source);
        Assert.DoesNotContain("INNER JOIN customers nav_Customer ON e.customer_id = nav_Customer.id WHERE name = @name", source);
    }

    [Fact]
    public void ProductRepositoryImpl_FindByCustomerName_uses_qualified_navigation_name()
    {
        var source = ReadGeneratedProductRepository();
        Assert.Contains("WHERE nav_Customer.name = @customerName", source);
    }

    [Fact]
    public void ProductRepositoryImpl_FindByNameWithSort_includes_customer_join()
    {
        var source = ReadGeneratedProductRepository();
        Assert.Contains("FindByNameAsync(string name, DapperX.Abstractions.Sorting.Sort sort)", source);
        Assert.Contains("INNER JOIN customers nav_Customer", source);
    }

    [Fact]
    public void ProductRepositoryImpl_CreateAsync_forwards_to_InsertAsync()
    {
        var source = ReadGeneratedProductRepository();
        Assert.Contains("public async System.Threading.Tasks.Task CreateAsync", source);
        Assert.Contains("await InsertAsync(product);", source);
    }

    [Fact]
    public void ProductRepositoryImpl_bulk_insert_forwards_to_InsertManyAsync()
    {
        var source = ReadGeneratedProductRepository();
        Assert.Contains("await InsertManyAsync(products);", source);
    }

    [Fact]
    public void ProductRepositoryImpl_native_query_embeds_sql_literal()
    {
        var source = ReadGeneratedProductRepository();
        Assert.Contains("SELECT id, name FROM products WHERE name = @name", source);
    }
}
