namespace Dapper.Npa.Tests;

public class SequenceGenerationTests
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
    public void NumberedItemRepositoryImpl_emits_sequence_next_sql_and_allocator()
    {
        var source = ReadGenerated("NumberedItemRepositoryImpl.g.cs");

        Assert.Contains("ISequenceAllocator", source);
        Assert.Contains("SequenceNextSql", source);
        Assert.Contains("NEXT VALUE FOR item_id_seq", source);
        Assert.Contains("_sequenceAllocator.NextAsync", source);
        Assert.DoesNotContain("SCOPE_IDENTITY", source);
    }
}
