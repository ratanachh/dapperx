namespace Dapper.Npa.Tests;

public class BatchRelationshipLoaderGenerationTests
{
    private static string ReadGenerated(string implFileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "Dapper.Npa.Generator", "Dapper.Npa.Generator.DapperXSourceGenerator",
            implFileName));
        return File.ReadAllText(path);
    }

    [Fact]
    public void OrderRepositoryImpl_emits_batch_collection_loader()
    {
        var source = ReadGenerated("OrderRepositoryImpl.g.cs");

        Assert.Contains("LoadItemsForManySql", source);
        Assert.Contains("LoadItemsForManyAsync", source);
        Assert.Contains("IN @parentIds", source);
        Assert.Contains("Items.Set(", source);
        Assert.Contains("ORDER BY position", source);
        Assert.Contains("AssignItemsOrderPositionSql", source);
        Assert.Contains("CloseItemsOrderGapSql", source);
        Assert.Contains("AssignItemsPositionAsync", source);
        Assert.Contains("CloseItemsOrderGapAsync", source);
        Assert.Contains("const string MethodName = \"LoadItemsForManyAsync\"", source);
    }

    [Fact]
    public void DepartmentRepositoryImpl_emits_batch_map_loader()
    {
        var source = ReadGenerated("DepartmentRepositoryImpl.g.cs");

        Assert.Contains("LoadEmployeesByCodeForManySql", source);
        Assert.Contains("LoadEmployeesByCodeForManyAsync", source);
        Assert.Contains("IN @parentIds", source);
        Assert.Contains("EmployeesByCode.Set(", source);
        Assert.Contains("ToDictionary(r => r.EmployeeCode)", source);
    }
}
