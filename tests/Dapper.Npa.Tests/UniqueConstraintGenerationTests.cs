namespace Dapper.Npa.Tests;

public class UniqueConstraintGenerationTests
{
    [Fact]
    public void ConstraintProductRepositoryImpl_emits_no_ddl_or_unique_metadata_sql()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "Dapper.Npa.Generator", "Dapper.Npa.Generator.DapperXSourceGenerator",
            "ConstraintProductRepositoryImpl.g.cs"));
        var source = File.ReadAllText(path);
        Assert.Contains("INSERT INTO constraint_products", source);
        Assert.DoesNotContain("CREATE UNIQUE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ALTER TABLE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UniqueConstraintMetadata", source);
    }
}
