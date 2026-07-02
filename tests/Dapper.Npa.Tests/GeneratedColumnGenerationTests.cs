using System.Text.RegularExpressions;

namespace Dapper.Npa.Tests;

public class GeneratedColumnGenerationTests
{
    private static string ReadGenerated(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "Dapper.Npa.Generator", "Dapper.Npa.Generator.DapperXSourceGenerator",
            fileName));
        return File.ReadAllText(path);
    }

    [Fact]
    public void GeneratedOrderRepositoryImpl_InsertSql_uses_OUTPUT_INSERTED_and_omits_generated_columns()
    {
        var source = ReadGenerated("GeneratedOrderRepositoryImpl.g.cs");
        Assert.Contains(
            "protected override string InsertSql => \"INSERT INTO generated_orders (name) OUTPUT INSERTED.id, INSERTED.created_at, INSERTED.total_with_tax VALUES (@Name)\";",
            source);
        Assert.DoesNotContain("SCOPE_IDENTITY", source);
    }

    [Fact]
    public void GeneratedOrderRepositoryImpl_UpdateSql_omits_generated_columns()
    {
        var source = ReadGenerated("GeneratedOrderRepositoryImpl.g.cs");
        var updateSql = ExtractProtectedOverride(source, "UpdateSql");
        Assert.DoesNotContain("created_at", updateSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("total_with_tax", updateSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name = @Name", updateSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GeneratedOrderRepositoryImpl_InsertAsync_uses_inline_fetch_and_update_reselects_always_only()
    {
        var source = ReadGenerated("GeneratedOrderRepositoryImpl.g.cs");
        Assert.Contains("DbExecutor.QueryFirstOrDefaultAsync<GeneratedInsertFetchRow>(_connection, InsertSql", source);
        Assert.Contains("ApplyGeneratedInsertFetch(entity, __insertFetch)", source);
        Assert.Contains("GeneratedColumnsReSelectSql", source);

        var insertStart = source.IndexOf("public override async Task InsertAsync", StringComparison.Ordinal);
        var updateStart = source.IndexOf("public override async Task UpdateAsync", StringComparison.Ordinal);
        Assert.True(insertStart >= 0 && updateStart > insertStart);
        var insertBody = source.Substring(insertStart, updateStart - insertStart);
        Assert.DoesNotContain("GeneratedColumnsReSelectSql", insertBody);

        var updateBody = ExtractMethodBody(source, updateStart);
        Assert.Contains("GeneratedColumnsReSelectSql", updateBody);
    }

    [Fact]
    public void MappedGeneratedItemRepositoryImpl_inherits_generated_insert_output()
    {
        var source = ReadGenerated("MappedGeneratedItemRepositoryImpl.g.cs");
        Assert.Contains(
            "protected override string InsertSql => \"INSERT INTO mapped_generated_items (title) OUTPUT INSERTED.id, INSERTED.created_at VALUES (@Title)\";",
            source);
    }

    private static string ExtractMethodBody(string source, int methodStart)
    {
        var braceStart = source.IndexOf('{', methodStart);
        Assert.True(braceStart >= 0, "Could not find method body opening brace.");
        var depth = 0;
        for (var i = braceStart; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}' && --depth == 0)
                return source.Substring(methodStart, i - methodStart + 1);
        }

        throw new InvalidOperationException("Could not find matching closing brace for method body.");
    }

    private static string ExtractProtectedOverride(string source, string constantName)
    {
        var match = Regex.Match(
            source,
            $@"protected override string {Regex.Escape(constantName)} => ""((?:[^""\\]|\\.)*)"";",
            RegexOptions.Singleline);
        Assert.True(match.Success, $"Missing {constantName} in generated source.");
        return match.Groups[1].Value;
    }
}
