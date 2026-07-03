namespace DapperX.Tests;

public class LazyMapGenerationTests
{
    private static string ReadGenerated(string implFileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            implFileName));
        Assert.True(File.Exists(path), $"Expected generated file at {path}");
        return File.ReadAllText(path);
    }

    [Fact]
    public void DepartmentRepositoryImpl_emits_compile_time_lazy_map_sql_literals()
    {
        var source = ReadGenerated("DepartmentRepositoryImpl.g.cs");

        Assert.Contains("private const string LoadEmployeesByCodeSql = \"SELECT id AS Id, department_id AS DepartmentId, employee_code AS EmployeeCode, full_name AS FullName FROM employees WHERE department_id = @parentId\"", source);
        Assert.Contains("private const string LoadEmployeesByCodeForManySql = \"SELECT id AS Id, department_id AS DepartmentId, employee_code AS EmployeeCode, full_name AS FullName FROM employees WHERE department_id IN @parentIds\"", source);
        Assert.DoesNotContain("LoadEmployeesByCodeSql +", source);
    }

    [Fact]
    public void DepartmentRepositoryImpl_lazy_map_sql_matches_one_to_many_pattern()
    {
        var mapSource = ReadGenerated("DepartmentRepositoryImpl.g.cs");
        var collectionSource = ReadGenerated("OrderRepositoryImpl.g.cs");

        const string mapSelect = "SELECT id AS Id, department_id AS DepartmentId, employee_code AS EmployeeCode, full_name AS FullName FROM employees WHERE department_id";
        const string collectionSelect = "SELECT id AS Id, order_id AS OrderId, sku AS Sku, position AS Position FROM order_items WHERE order_id";

        Assert.Contains(mapSelect, mapSource);
        Assert.Contains(collectionSelect, collectionSource);
        Assert.Contains("= @parentId", mapSource);
        Assert.Contains("IN @parentIds", mapSource);
    }

    [Fact]
    public void DepartmentRepositoryImpl_groups_by_map_key_in_memory()
    {
        var source = ReadGenerated("DepartmentRepositoryImpl.g.cs");

        Assert.Contains("LazyMap<string, DapperX.Tests.Fixtures.Employee>", source);
        Assert.Contains("ToDictionary(r => r.EmployeeCode)", source);
        Assert.Contains("EmployeesByCode.Set(map)", source);
        Assert.DoesNotContain("CONCAT", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DepartmentRepositoryImpl_batch_loader_uses_same_sql_shape_as_lazy_collection()
    {
        var mapSource = ReadGenerated("DepartmentRepositoryImpl.g.cs");
        var collectionSource = ReadGenerated("OrderRepositoryImpl.g.cs");

        Assert.Contains("LoadEmployeesByCodeForManyAsync", mapSource);
        Assert.Contains("LoadItemsForManyAsync", collectionSource);
        Assert.Contains("var parentIds = parents.Select(p => p.Id).ToList();", mapSource);
        Assert.Contains("if (parentIds.Count == 0) return;", mapSource);
    }
}
