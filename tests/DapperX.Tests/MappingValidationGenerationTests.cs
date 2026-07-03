namespace DapperX.Tests;

public class MappingValidationGenerationTests
{
    private static string ReadGenerated(string implFileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            implFileName));
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    [Fact]
    public void UserRepositoryImpl_generates_with_primary_key_join_assigned_id()
    {
        var source = ReadGenerated("UserRepositoryImpl.g.cs");
        Assert.Contains("entity.Profile.Id = entity.Id", source);
    }

    [Fact]
    public void DocumentRepositoryImpl_generates_with_secondary_table_join()
    {
        var source = ReadGenerated("DocumentRepositoryImpl.g.cs");
        Assert.Contains("SecondaryInsert_document_details", source);
        Assert.Contains("document_id", source);
    }

    [Fact]
    public void ReadOnlyCatalogItemRepositoryImpl_is_immutable_without_mutations()
    {
        var source = ReadGenerated("ReadOnlyCatalogItemRepositoryImpl.g.cs");
        Assert.Contains("Entity is marked [Immutable]", source);
        Assert.DoesNotContain("public override async Task InsertAsync", source);
    }

    [Fact]
    public void MappedAuditItemRepositoryImpl_inherits_auditing_from_mapped_superclass()
    {
        var source = ReadGenerated("MappedAuditItemRepositoryImpl.g.cs");
        Assert.Contains("CreatedAt", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("InsertSql", source);
    }

    [Fact]
    public void MappedTenantItemRepositoryImpl_emits_tenant_filter()
    {
        var source = ReadGenerated("MappedTenantItemRepositoryImpl.g.cs");
        Assert.Contains("tenant_id", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormulaOrderRepositoryImpl_includes_formula_in_select()
    {
        var source = ReadGenerated("FormulaOrderRepositoryImpl.g.cs");
        Assert.Contains("Formula", source, StringComparison.OrdinalIgnoreCase);
    }
}
