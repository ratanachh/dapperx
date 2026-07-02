using System.Reflection;

namespace Dapper.Npa.Tests;

/// <summary>Smoke checks for Requirements.md Rules A–E in generated and runtime assemblies.</summary>
public class RulesComplianceTests
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

    [Fact]
    public void Rule_A_generated_repository_avoids_runtime_sql_concatenation_with_property_names()
    {
        var source = ReadGeneratedProductRepository();
        Assert.DoesNotContain("string.Concat", source);
        Assert.DoesNotContain("$\"SELECT", source);
        Assert.Contains("@", source);
    }

    [Fact]
    public void Rule_B_generated_repository_uses_ResolveColumn_switch()
    {
        var source = ReadGeneratedProductRepository();
        Assert.Contains("ResolveColumn", source);
        Assert.DoesNotContain("GetProperties(", source);
        Assert.DoesNotContain("MemberInfo", source);
    }

    [Fact]
    public void Rule_C_primary_key_join_assigns_id_in_csharp_not_sql()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "Dapper.Npa.Generator", "Dapper.Npa.Generator.DapperXSourceGenerator",
            "UserRepositoryImpl.g.cs"));
        var source = File.ReadAllText(path);
        Assert.Contains("entity.Profile.Id = entity.Id", source);
    }

    [Fact]
    public void Rule_D_repository_impl_has_no_tracking_or_sql_builder_state_fields()
    {
        var type = typeof(Fixtures.ProductRepositoryImpl);
        var instanceFields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(f => !f.Name.StartsWith('<')
                && f.Name is not ("_connection" or "_options" or "_lifecycle"))
            .ToList();
        Assert.Empty(instanceFields);
    }

    [Fact]
    public void Rule_E_index_does_not_emit_ddl_in_product_sql()
    {
        var source = ReadGeneratedProductRepository();
        Assert.DoesNotContain("CREATE INDEX", source, StringComparison.OrdinalIgnoreCase);
    }
}
