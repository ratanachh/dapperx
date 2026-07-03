using DapperX.Abstractions.Query;
using DapperX.Abstractions.Repositories;
using DapperX.Query.Expressions;
using DapperX.Query.Projections;
using DapperX.Query.Query;
using DapperX.Runtime.Query;
using DapperX.Tests.Fixtures;

namespace DapperX.Tests;

public class QueryGenerationTests
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

    private static string ReadGeneratedArchivedItemRepository()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            "ArchivedItemRepositoryImpl.g.cs"));
        return File.ReadAllText(path);
    }

    private static string ReadGeneratedEagerOrderRepository()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            "EagerOrderRepositoryImpl.g.cs"));
        return File.ReadAllText(path);
    }

    private static string ReadGeneratedUserRepository()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            "UserRepositoryImpl.g.cs"));
        return File.ReadAllText(path);
    }

    [Fact]
    public void ProductRepositoryImpl_exposes_Query_method()
    {
        Assert.NotNull(typeof(ProductRepositoryImpl).GetMethod(nameof(IRepository<Product, int>.Query)));
        var repo = new ProductRepositoryImpl(null!);
        Assert.IsAssignableFrom<IQuery<Product>>(repo.Query());
    }

    [Fact]
    public void ProductRepositoryImpl_emits_query_base_sql_and_include_join_catalog()
    {
        var source = ReadGeneratedProductRepository();
        Assert.Contains("QueryBaseSql", source);
        Assert.Contains("FROM products e", source);
        Assert.Contains("QueryIncludeJoinSql", source);
        Assert.Contains("Customer", source);
        Assert.Contains("AttachCustomerForQueryAsync", source);
    }

    [Fact]
    public void EagerOrderRepositoryImpl_bakes_eager_join_into_query_base()
    {
        var source = ReadGeneratedEagerOrderRepository();
        Assert.Contains("QueryBaseSql", source);
        Assert.Contains("INNER JOIN accounts nav_Account", source);
    }

    [Fact]
    public void UserRepositoryImpl_emits_primary_key_join_include_sql()
    {
        var source = ReadGeneratedUserRepository();
        Assert.Contains("QueryIncludeJoinSql", source);
        Assert.Contains("Profile", source);
        Assert.Contains("INNER JOIN user_profiles nav_Profile ON e.id = nav_Profile.id", source);
    }

    [Fact]
    public void ArchivedItemRepositoryImpl_query_base_has_no_baked_soft_delete_where()
    {
        var source = ReadGeneratedArchivedItemRepository();
        Assert.Contains("QueryBaseSql", source);
        Assert.Contains("FROM archived_items e", source);
        Assert.DoesNotContain("is_deleted = 0 FROM", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SoftDeleteColumn", source);
    }

    [Fact]
    public void WhereTranslator_resolves_product_columns_via_generated_switch()
    {
        var translator = new WhereTranslator(ProductRepositoryImpl.ResolveColumn);
        var predicates = new List<System.Linq.Expressions.Expression<Func<Product, bool>>>
        {
            p => p.Name == "test",
        };
        var (sql, _) = translator.Translate(predicates);
        Assert.Contains("name = @p0", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WhereTranslator_translates_null_and_like()
    {
        var translator = new WhereTranslator(ProductRepositoryImpl.ResolveColumn);
        var predicates = new List<System.Linq.Expressions.Expression<Func<Product, bool>>>
        {
            p => p.Name == null!,
            p => p.Name.Contains("ab"),
        };
        var (sql, _) = translator.Translate(predicates);
        Assert.Contains("IS NULL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIKE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WhereTranslator_translates_enumerable_contains_in()
    {
        var ids = new[] { 1, 2, 3 };
        var translator = new WhereTranslator(ProductRepositoryImpl.ResolveColumn);
        var predicates = new List<System.Linq.Expressions.Expression<Func<Product, bool>>>
        {
            p => System.Linq.Enumerable.Contains(ids, p.Id),
        };
        var (sql, _) = translator.Translate(predicates);
        Assert.Contains("IN @", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductRepositoryImpl_emits_projection_catalog_and_soft_delete_flag()
    {
        var source = ReadGeneratedProductRepository();
        Assert.Contains("QueryProjectionBaseSql", source);
        Assert.Contains("ProductSummary", source);
        Assert.Contains("SoftDeleteSupported = false", source);
    }

    [Fact]
    public void ArchivedItemRepositoryImpl_sets_soft_delete_supported()
    {
        var source = ReadGeneratedArchivedItemRepository();
        Assert.Contains("SoftDeleteSupported = true", source);
    }

    [Fact]
    public void RepositoryQuery_Select_uses_projection_catalog()
    {
        var repo = new ProductRepositoryImpl(null!);
        var dtoQuery = repo.Query().Select<ProductSummary>();
        Assert.IsAssignableFrom<IQuery<ProductSummary>>(dtoQuery);
    }

    [Fact]
    public void ProjectionMaterializer_requires_projection_attribute()
    {
        Assert.Throws<InvalidOperationException>(() => ProjectionMaterializer.EnsureProjection<Product>());
    }

    [Fact]
    public void RepositoryQuery_IncludeDeleted_fails_when_soft_delete_unsupported()
    {
        var repo = new ProductRepositoryImpl(null!);
        Assert.Throws<InvalidOperationException>(() => repo.Query().IncludeDeleted());
    }

    [Fact]
    public void QueryExecutor_IncludeDeleted_omits_soft_delete_predicate()
    {
        var config = new QueryRuntimeConfig
        {
            Provider = "SqlServer",
            MainAlias = "e",
            SoftDeleteSupported = true,
            SoftDeleteColumn = "is_deleted",
            IncludeJoinSql = new Dictionary<string, string>(),
        };
        var defaultState = QueryBuilderStateSnapshot.From(new DapperX.Query.Query.QueryBuilder<ArchivedItem>().Build());
        var (_, defaultSql, _) = QueryExecutor.BuildSelectSql(
            "SELECT e.id, e.name, e.is_deleted FROM archived_items e",
            defaultState,
            ArchivedItemRepositoryImpl.ResolveColumn,
            config);
        Assert.Contains("e.is_deleted = 0", defaultSql);

        var includeDeleted = QueryBuilderStateSnapshot.From(
            new DapperX.Query.Query.QueryBuilder<ArchivedItem>().IncludeDeleted().Build());
        var (_, sql, _) = QueryExecutor.BuildSelectSql(
            "SELECT e.id, e.name, e.is_deleted FROM archived_items e",
            includeDeleted,
            ArchivedItemRepositoryImpl.ResolveColumn,
            config);
        Assert.DoesNotContain("is_deleted = 0", sql);
    }

    [Fact]
    public void RepositoryQuery_Select_carries_entity_where_state_to_projection_sql()
    {
        var config = new QueryRuntimeConfig
        {
            Provider = "SqlServer",
            MainAlias = "e",
            SoftDeleteSupported = false,
            IncludeJoinSql = new Dictionary<string, string>(),
        };
        var carried = QueryBuilderStateSnapshot.From(
            new DapperX.Query.Query.QueryBuilder<Product>().Where(p => p.Name == "x").Build());
        var projectionSql = "SELECT e.id, e.name FROM products e";
        var dtoQuery = new RepositoryQuery<ProductSummary>(
            null!,
            projectionSql,
            " FROM products e",
            ProductRepositoryImpl.ResolveColumn,
            config,
            carriedState: carried);
        var (_, sql, _) = QueryExecutor.BuildSelectSql(
            projectionSql,
            dtoQuery.BuildEffectiveStateForTests(),
            ProductRepositoryImpl.ResolveColumn,
            config);
        Assert.Contains("name = @p0", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QueryBuilderStateSnapshot_AsSlice_flag_is_preserved_through_merge()
    {
        var carried = QueryBuilderStateSnapshot.From(new DapperX.Query.Query.QueryBuilder<Product>().AsSlice().Build());
        var merged = carried.MergeWith(QueryBuilderStateSnapshot.From(new DapperX.Query.Query.QueryBuilder<Product>().Skip(2).Take(11).Build()));
        Assert.True(merged.AsSlice);
        Assert.Equal(2, merged.Skip);
        Assert.Equal(11, merged.Take);
    }
}
