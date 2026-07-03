namespace DapperX.Tests;

public class ManyToManyGenerationTests
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

    private static string ReadGeneratorSource(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "DapperX.Generator", relativePath));
        return File.ReadAllText(path);
    }

    [Fact]
    public void StudentRepositoryImpl_emits_join_insert_and_delete_sql_literals()
    {
        var source = ReadGenerated("StudentRepositoryImpl.g.cs");

        Assert.Contains("JoinInsert_Courses_Sql", source);
        Assert.Contains("INSERT INTO student_courses (student_id, course_id) VALUES (@parentId, @childId)", source);
        Assert.Contains("JoinDelete_Courses_Sql", source);
        Assert.Contains("DELETE FROM student_courses WHERE student_id = @parentId", source);
    }

    [Fact]
    public void StudentRepositoryImpl_insert_graph_emits_batch_join_insert_with_dedupe_and_guard()
    {
        var source = ReadGenerated("StudentRepositoryImpl.g.cs");
        var insertStart = source.IndexOf("public override async Task InsertGraphAsync", StringComparison.Ordinal);
        Assert.True(insertStart >= 0);
        var insertEnd = source.IndexOf("public override async Task UpdateGraphAsync", insertStart, StringComparison.Ordinal);
        var body = source.Substring(insertStart, insertEnd - insertStart);

        Assert.Contains("JoinInsert_Courses_Sql", body);
        Assert.Contains(".Where(x => x.childId is not null)", body);
        Assert.Contains(".Distinct()", body);
        Assert.Contains("if (joinRows.Count > 0)", body);
    }

    [Fact]
    public void StudentRepositoryImpl_delete_graph_emits_join_delete_before_root_delete()
    {
        var source = ReadGenerated("StudentRepositoryImpl.g.cs");
        var deleteStart = source.IndexOf("public override async Task DeleteGraphAsync", StringComparison.Ordinal);
        Assert.True(deleteStart >= 0);
        var deleteBody = source.Substring(deleteStart);

        var joinDeleteIndex = deleteBody.IndexOf("JoinDelete_Courses_Sql", StringComparison.Ordinal);
        var rootDeleteIndex = deleteBody.IndexOf("await DeleteAsync(root", StringComparison.Ordinal);
        Assert.True(joinDeleteIndex >= 0);
        Assert.True(rootDeleteIndex >= 0);
        Assert.True(joinDeleteIndex < rootDeleteIndex);
    }

    [Fact]
    public void StudentRepositoryImpl_update_graph_reconciles_join_table_when_collection_loaded()
    {
        var source = ReadGenerated("StudentRepositoryImpl.g.cs");
        var updateStart = source.IndexOf("public override async Task UpdateGraphAsync", StringComparison.Ordinal);
        Assert.True(updateStart >= 0);
        var updateEnd = source.IndexOf("public override async Task DeleteGraphAsync", updateStart, StringComparison.Ordinal);
        var body = source.Substring(updateStart, updateEnd - updateStart);

        Assert.Contains("CoursesLinks = root.Courses.TryGet()", body);
        Assert.Contains("JoinDelete_Courses_Sql", body);
        Assert.Contains("JoinInsert_Courses_Sql", body);
        Assert.Contains(".Distinct()", body);
    }

    [Fact]
    public void StudentRepositoryImpl_execution_plan_includes_join_table_nodes()
    {
        var source = ReadGenerated("StudentRepositoryImpl.g.cs");

        Assert.Contains("Operation = \"InsertJoinTable\"", source);
        Assert.Contains("Operation = \"DeleteJoinTable\"", source);
        Assert.Contains("student_courses", source);
    }

    [Fact]
    public void DiagnosticsReporter_defines_many_to_many_join_table_dpx076_through_dpx078()
    {
        var source = ReadGeneratorSource("Utils/DiagnosticsReporter.cs");

        Assert.Contains("\"DPX076\"", source);
        Assert.Contains("Missing [JoinTable] on [ManyToMany]", source);
        Assert.Contains("\"DPX077\"", source);
        Assert.Contains("Missing [JoinTable] JoinColumn", source);
        Assert.Contains("\"DPX078\"", source);
        Assert.Contains("Missing [JoinTable] InverseJoinColumn", source);
    }

    [Fact]
    public void RelationshipValidator_validates_join_table_completeness_for_many_to_many()
    {
        var source = ReadGeneratorSource("Validation/RelationshipValidator.cs");

        Assert.Contains("Diagnostics.ManyToManyMissingJoinTable", source);
        Assert.Contains("Diagnostics.JoinTableMissingJoinColumn", source);
        Assert.Contains("Diagnostics.JoinTableMissingInverseJoinColumn", source);
    }
}
