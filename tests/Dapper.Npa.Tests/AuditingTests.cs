using Dapper.Npa.Generator.Builders;
using Dapper.Npa.Generator.Models;
using Dapper.Npa.Tests.Fixtures;

namespace Dapper.Npa.Tests;

public class AuditingTests
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
    public void AuditedProductRepositoryImpl_UpdateSql_excludes_created_audit_columns()
    {
        var source = ReadGenerated("AuditedProductRepositoryImpl.g.cs");
        var updateSql = ExtractSqlConstant(source, "UpdateSql");
        Assert.DoesNotContain("created_at =", updateSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("created_by =", updateSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("modified_at = GETDATE()", updateSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("modified_by = @ModifiedBy", updateSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AuditedProductRepositoryImpl_InsertSql_uses_dialect_timestamps_for_dates()
    {
        var source = ReadGenerated("AuditedProductRepositoryImpl.g.cs");
        var insertSql = ExtractSqlConstant(source, "InsertSql");
        Assert.Contains("created_at", insertSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("modified_at", insertSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GETDATE()", insertSql);
        Assert.Contains("@CreatedBy", insertSql);
        Assert.Contains("@ModifiedBy", insertSql);
        Assert.DoesNotContain("@CreatedAt", insertSql);
        Assert.DoesNotContain("@ModifiedAt", insertSql);
    }

    [Fact]
    public void AuditedProductRepositoryImpl_populates_user_fields_before_lifecycle_hooks()
    {
        var source = ReadGenerated("AuditedProductRepositoryImpl.g.cs");
        var insertStart = source.IndexOf("public override async Task InsertAsync", StringComparison.Ordinal);
        var updateStart = source.IndexOf("public override async Task UpdateAsync", StringComparison.Ordinal);
        Assert.True(insertStart >= 0 && updateStart > insertStart);

        var insertBody = source.Substring(insertStart, updateStart - insertStart);
        Assert.Contains("GetCurrentUser()", insertBody);
        Assert.DoesNotContain("DateTime.UtcNow", insertBody);
        Assert.True(insertBody.IndexOf("GetCurrentUser()", StringComparison.Ordinal)
            < insertBody.IndexOf("OnPrePersist(entity)", StringComparison.Ordinal));

        var updateBody = source.Substring(updateStart, Math.Min(600, source.Length - updateStart));
        Assert.Contains("GetCurrentUser()", updateBody);
        Assert.DoesNotContain("DateTime.UtcNow", updateBody);
        Assert.True(updateBody.IndexOf("GetCurrentUser()", StringComparison.Ordinal)
            < updateBody.IndexOf("OnPreUpdate(entity)", StringComparison.Ordinal));
    }

    [Fact]
    public void AuditedProductRepositoryImpl_insert_many_populates_audit_fields_per_entity()
    {
        var source = ReadGenerated("AuditedProductRepositoryImpl.g.cs");
        var insertManyStart = source.IndexOf("InsertManyAsync", StringComparison.Ordinal);
        var insertManyEnd = source.IndexOf("UpdateManyAsync", insertManyStart, StringComparison.Ordinal);
        var body = source.Substring(insertManyStart, insertManyEnd - insertManyStart);
        Assert.Contains("_auditingProvider.GetCurrentUser()", body);
        Assert.Contains("OnPrePersist(entity)", body);
    }

    [Fact]
    public void AuditedProductRepositoryImpl_injects_IAuditingProvider()
    {
        var source = ReadGenerated("AuditedProductRepositoryImpl.g.cs");
        Assert.Contains("IAuditingProvider? auditingProvider", source);
        Assert.Contains("_auditingProvider = auditingProvider", source);
    }

    [Fact]
    public void MappedAuditItemRepositoryImpl_inherits_mapped_superclass_audit_sql()
    {
        var source = ReadGenerated("MappedAuditItemRepositoryImpl.g.cs");
        var insertSql = ExtractSqlConstant(source, "InsertSql");
        var updateSql = ExtractSqlConstant(source, "UpdateSql");
        Assert.Contains("created_at", insertSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GETDATE()", insertSql);
        Assert.DoesNotContain("created_at =", updateSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AuditingSqlBuilder_uses_provider_specific_timestamp_literals()
    {
        var entity = new EntityModel
        {
            ClassName = "Sample",
            TableName = "samples",
            Properties =
            [
                new PropertyModel { PropertyName = "CreatedAt", ColumnName = "created_at", AuditingRole = AuditingRole.CreatedDate, Insertable = false, Updatable = false },
            ],
            Auditing = new AuditingModel { CreatedDateProperty = "CreatedAt" },
        };

        Assert.Equal("GETDATE()", AuditingSqlBuilder.CurrentTimestampLiteral("SqlServer"));
        Assert.Equal("CURRENT_TIMESTAMP", AuditingSqlBuilder.CurrentTimestampLiteral("PostgreSql"));
        Assert.Equal("NOW()", AuditingSqlBuilder.CurrentTimestampLiteral("MySql"));
        Assert.Equal("datetime('now')", AuditingSqlBuilder.CurrentTimestampLiteral("Sqlite"));

        var assignments = AuditingSqlBuilder.GetInsertAssignments(entity, [], "SqlServer");
        Assert.Single(assignments);
        Assert.Equal("created_at", assignments[0].Column);
        Assert.Equal("GETDATE()", assignments[0].ValueExpression);
    }

    private static string ExtractSqlConstant(string source, string constantName)
    {
        var marker = $"protected override string {constantName} => \"";
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Missing {constantName} in generated source.");
        start += marker.Length;
        var end = source.IndexOf("\"; SELECT SCOPE_IDENTITY()", start, StringComparison.Ordinal);
        if (end < 0)
            end = source.IndexOf("\"; SELECT", start, StringComparison.Ordinal);
        if (end < 0)
            end = source.IndexOf("\";", start, StringComparison.Ordinal);
        Assert.True(end > start, $"Unterminated {constantName} literal.");
        return source.Substring(start, end - start);
    }
}
