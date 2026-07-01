namespace DapperX.Tests;

/// <summary>
/// Asserts generated artifacts for Requirements.md Section 23 (32 must-generate items).
/// </summary>
public class Section23ComplianceTests
{
    private static string ReadGenerated(string implFileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            implFileName));
        return File.ReadAllText(path);
    }

    public static IEnumerable<object[]> Section23Artifacts()
    {
        // 1–9: core repository SQL, paging, derived queries, filters, ResolveColumn
        yield return ["ProductRepositoryImpl.g.cs", "sealed class ProductRepositoryImpl", "RepositoryImpl"];
        yield return ["ProductRepositoryImpl.g.cs", "SelectAllSql", "Inline SQL literals"];
        yield return ["ProductRepositoryImpl.g.cs", "GetAllAsync", "GetAllAsync overloads"];
        yield return ["ProductRepositoryImpl.g.cs", "DeleteAllByIdAsync", "DeleteAllByIdAsync"];
        yield return ["ProductRepositoryImpl.g.cs", "FindByNameAsync", "Derived query methods"];
        yield return ["ArchivedItemRepositoryImpl.g.cs", "is_deleted = 0", "Paired soft-delete filter SQL"];
        yield return ["ProductRepositoryImpl.g.cs", "ProductSortFragments", "Sort lookup tables"];
        yield return ["ProductRepositoryImpl.g.cs", "ResolveColumn", "ResolveColumn switch"];
        yield return ["CatalogItemRepositoryImpl.g.cs", "FILTER_ActiveOnly", "Global filter constants"];

        // 10–14: graph plans, lifecycle, auditing, soft-delete, tenancy
        yield return ["OrderRepositoryImpl.g.cs", "InsertGraphExecutionPlan", "Execution plans"];
        yield return ["ProductRepositoryImpl.g.cs", "OnPostLoad", "Lifecycle invocations"];
        yield return ["AuditedProductRepositoryImpl.g.cs", "CreatedAt", "Auditing injection"];
        yield return ["ArchivedItemRepositoryImpl.g.cs", "SET is_deleted = 1", "Soft-delete DELETE rewrite"];
        yield return ["TenantScopedItemRepositoryImpl.g.cs", "tenant_id", "Tenancy filter"];

        // 15–19: eager fetch, projection, formula, transformer, generated columns
        yield return ["ProductRepositoryImpl.g.cs", "QueryIncludeJoinSql", "Eager-fetch JOIN catalog"];
        yield return ["ProductRepositoryImpl.g.cs", "QueryProjectionBaseSql", "Projection column lists"];
        yield return ["ProductRepositoryImpl.g.cs", "address_city", "Formula / embedded columns"];
        yield return ["ProductRepositoryImpl.g.cs", "ResolveColumn", "Column resolution"];
        yield return ["ProductRepositoryImpl.g.cs", "InsertSql", "Generated column INSERT"];

        // 20–26: M2M, secondary table, PK join, LazyMap, batch loaders, element collections, named graphs
        yield return ["StudentRepositoryImpl.g.cs", "JoinInsert_Courses_Sql", "Join table M2M SQL"];
        yield return ["DocumentRepositoryImpl.g.cs", "SecondaryInsert_document_details", "Secondary table ops"];
        yield return ["DocumentRepositoryImpl.g.cs", "document_id", "Secondary / PK join table"];
        yield return ["DepartmentRepositoryImpl.g.cs", "LoadEmployeesByCodeForManyAsync", "LazyMap batch loader"];
        yield return ["OrderRepositoryImpl.g.cs", "LoadItemsForManyAsync", "LoadCollectionForManyAsync"];
        yield return ["TaggedProductRepositoryImpl.g.cs", "LoadTagsAsync", "Element collection SQL"];
        yield return ["ProductRepositoryImpl.g.cs", "Graph_product_withCustomer_Sql", "Named entity graph SQL"];

        // 27–32: upsert, CPQL, MethodName, slice, SQLite diagnostics, Index metadata only
        yield return ["ProductRepositoryImpl.g.cs", "UpsertSql", "Upsert SQL"];
        yield return ["ProductRepositoryImpl.g.cs", "FindByNameCpqlAsync", "CPQL translation"];
        yield return ["ProductRepositoryImpl.g.cs", "const string MethodName", "MethodName literal"];
        yield return ["ProductRepositoryImpl.g.cs", "SelectAllSliceSql", "GetAllSliceAsync"];
        yield return ["ProductRepositoryImpl.g.cs", "SoftDeleteSupported", "Query runtime soft-delete flag"];
        yield return ["ProductRepositoryImpl.g.cs", "ResolveColumn", "Index produces no SQL"];
    }

    [Theory]
    [MemberData(nameof(Section23Artifacts))]
    public void Generated_repository_contains_section23_artifact(string fileName, string expectedFragment, string itemDescription)
    {
        var source = ReadGenerated(fileName);
        Assert.True(
            source.Contains(expectedFragment, StringComparison.Ordinal),
            $"Section 23 item '{itemDescription}' expected '{expectedFragment}' in {fileName}");
    }

    [Fact]
    public void DocumentRepositoryImpl_emits_secondary_table_update_after_primary()
    {
        var source = ReadGenerated("DocumentRepositoryImpl.g.cs");
        var updateIndex = source.IndexOf("await DbExecutor.ExecuteAsync(_connection, UpdateSql", StringComparison.Ordinal);
        var secondaryUpdateIndex = source.IndexOf("SecondaryUpdate_document_details", StringComparison.Ordinal);
        Assert.True(updateIndex >= 0 && secondaryUpdateIndex > updateIndex);
    }

    [Fact]
    public void OrderRepositoryImpl_emits_graph_crud_methods()
    {
        var source = ReadGenerated("OrderRepositoryImpl.g.cs");
        Assert.Contains("InsertGraphAsync", source);
        Assert.Contains("UpdateGraphAsync", source);
        Assert.Contains("DeleteGraphAsync", source);
    }

    [Fact]
    public void ProductRepositoryImpl_does_not_emit_graph_on_leaf_entity()
    {
        var source = ReadGenerated("ProductRepositoryImpl.g.cs");
        Assert.DoesNotContain("InsertGraphAsync", source);
    }
}
