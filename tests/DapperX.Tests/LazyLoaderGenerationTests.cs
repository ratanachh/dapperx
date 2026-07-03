namespace DapperX.Tests;

public class LazyLoaderGenerationTests
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

    [Fact]
    public void OrderRepositoryImpl_emits_single_parent_lazy_loader_and_wires_on_post_load()
    {
        var source = ReadGenerated("OrderRepositoryImpl.g.cs");

        Assert.Contains("LoadItemsSql", source);
        Assert.Contains("order_id = @parentId", source);
        Assert.Contains("WireLazyLoaders", source);
        Assert.Contains("protected override void OnPostLoad", source);
        Assert.Contains("LazyCollection<", source);
        Assert.Contains("OrderItem>", source);
    }

    [Fact]
    public void StudentRepositoryImpl_emits_many_to_many_batch_loader_and_single_loader()
    {
        var source = ReadGenerated("StudentRepositoryImpl.g.cs");

        Assert.Contains("LoadCoursesLinksForManySql", source);
        Assert.Contains("LoadCoursesForManyAsync", source);
        Assert.Contains("LoadCoursesChildSelectByIdsSql", source);
        Assert.Contains("LoadCoursesSql", source);
        Assert.Contains("student_courses", source);
        Assert.Contains("Courses.Set(", source);
    }

    [Fact]
    public void OrderRepositoryImpl_insert_graph_assigns_order_positions()
    {
        var source = ReadGenerated("OrderRepositoryImpl.g.cs");

        Assert.Contains("InsertGraphAsync", source);
        Assert.Contains("AssignItemsOrderPositionSql", source);
        Assert.Contains("CloseItemsOrderGapAsync", source);
        Assert.Contains("DeleteGraphAsync", source);
    }
}
