namespace Dapper.Npa.Tests.Shared;

/// <summary>Per-provider SQL literal assertions for matrix-4 generation tests.</summary>
public static class ProviderExpectations
{
    public static string CurrentProvider
    {
        get
        {
#if DAPPERX_PROVIDER_SQLSERVER
            return "SqlServer";
#elif DAPPERX_PROVIDER_POSTGRESQL
            return "PostgreSql";
#elif DAPPERX_PROVIDER_MYSQL
            return "MySql";
#elif DAPPERX_PROVIDER_SQLITE
            return "Sqlite";
#else
            return "Unknown";
#endif
        }
    }

    public static void AssertUpsertSql(string upsertSql, string? provider = null)
    {
        provider ??= CurrentProvider;
        switch (provider)
        {
            case "SqlServer":
                Assert.Contains("MERGE", upsertSql, StringComparison.OrdinalIgnoreCase);
                break;
            case "PostgreSql":
            case "Sqlite":
                Assert.Contains("ON CONFLICT", upsertSql, StringComparison.OrdinalIgnoreCase);
                break;
            case "MySql":
                Assert.Contains("ON DUPLICATE KEY", upsertSql, StringComparison.OrdinalIgnoreCase);
                break;
            default:
                throw new InvalidOperationException($"Unknown provider: {provider}");
        }
    }

    public static void AssertSlicePaging(string sliceSql, string? provider = null)
    {
        provider ??= CurrentProvider;
        switch (provider)
        {
            case "SqlServer":
                Assert.Contains("FETCH", sliceSql, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("LIMIT @sliceSize", sliceSql, StringComparison.OrdinalIgnoreCase);
                break;
            case "PostgreSql":
            case "MySql":
            case "Sqlite":
                Assert.Contains("LIMIT @sliceSize", sliceSql, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("FETCH NEXT @sliceSize", sliceSql, StringComparison.OrdinalIgnoreCase);
                break;
            default:
                throw new InvalidOperationException($"Unknown provider: {provider}");
        }
    }

    public static void AssertPagePaging(string pageSql, string? provider = null)
    {
        provider ??= CurrentProvider;
        switch (provider)
        {
            case "SqlServer":
                Assert.Contains("OFFSET", pageSql, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("FETCH", pageSql, StringComparison.OrdinalIgnoreCase);
                break;
            case "PostgreSql":
            case "MySql":
            case "Sqlite":
                Assert.Contains("LIMIT @pageSize", pageSql, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("OFFSET @offset", pageSql, StringComparison.OrdinalIgnoreCase);
                break;
            default:
                throw new InvalidOperationException($"Unknown provider: {provider}");
        }
    }

    public static void AssertSqlServerOrderByBeforeOffset(string sql)
    {
        var orderIndex = sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        var offsetIndex = sql.IndexOf("OFFSET", StringComparison.OrdinalIgnoreCase);
        Assert.True(orderIndex >= 0, "Expected ORDER BY in paging SQL.");
        Assert.True(offsetIndex >= 0, "Expected OFFSET in paging SQL.");
        Assert.True(orderIndex < offsetIndex, "ORDER BY must precede OFFSET in SqlServer paging SQL.");
    }

    public static void AssertIdentityInsertExcludesId(string insertSql)
    {
        Assert.DoesNotContain("(id,", insertSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@Id", insertSql, StringComparison.OrdinalIgnoreCase);
    }

    public static void AssertIdentityInsert(string insertSql, string? provider = null)
    {
        provider ??= CurrentProvider;
        switch (provider)
        {
            case "SqlServer":
                Assert.True(
                    insertSql.Contains("OUTPUT INSERTED", StringComparison.OrdinalIgnoreCase)
                    || insertSql.Contains("SCOPE_IDENTITY", StringComparison.OrdinalIgnoreCase),
                    "SqlServer insert should use OUTPUT INSERTED or SCOPE_IDENTITY follow-up.");
                break;
            case "PostgreSql":
                Assert.Contains("RETURNING", insertSql, StringComparison.OrdinalIgnoreCase);
                break;
            case "MySql":
                Assert.True(
                    insertSql.Contains("LAST_INSERT_ID", StringComparison.OrdinalIgnoreCase)
                    || insertSql.Contains("ExecuteScalar", StringComparison.OrdinalIgnoreCase),
                    "MySql identity path expected.");
                break;
            case "Sqlite":
                Assert.True(
                    insertSql.Contains("last_insert_rowid", StringComparison.OrdinalIgnoreCase)
                    || insertSql.Contains("ExecuteScalar", StringComparison.OrdinalIgnoreCase),
                    "Sqlite identity path expected.");
                break;
            default:
                throw new InvalidOperationException($"Unknown provider: {provider}");
        }
    }

    public static void AssertBulkInsertPath(string repositorySource, string? provider = null)
    {
        provider ??= CurrentProvider;
        switch (provider)
        {
            case "SqlServer":
                Assert.Contains("SqlBulkCopy", repositorySource, StringComparison.OrdinalIgnoreCase);
                break;
            case "PostgreSql":
                Assert.True(
                    repositorySource.Contains("COPY", StringComparison.OrdinalIgnoreCase)
                    || repositorySource.Contains("PostgreSqlBulkExecutor", StringComparison.OrdinalIgnoreCase),
                    "PostgreSql bulk insert path expected.");
                break;
            case "MySql":
                Assert.True(
                    repositorySource.Contains("MySqlBulkCopy", StringComparison.OrdinalIgnoreCase)
                    || repositorySource.Contains("MySqlBulkExecutor", StringComparison.OrdinalIgnoreCase)
                    || repositorySource.Contains("MySqlBatchExecutor", StringComparison.OrdinalIgnoreCase),
                    "MySql bulk insert path expected.");
                break;
            case "Sqlite":
                Assert.DoesNotContain("SqlBulkCopy", repositorySource, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("BatchChunker.Chunk", repositorySource);
                break;
            default:
                throw new InvalidOperationException($"Unknown provider: {provider}");
        }
    }

    public static void AssertPessimisticReadSuffix(string sql, string? provider = null)
    {
        provider ??= CurrentProvider;
        switch (provider)
        {
            case "SqlServer":
                Assert.Contains("HOLDLOCK", sql, StringComparison.OrdinalIgnoreCase);
                break;
            case "PostgreSql":
            case "MySql":
                Assert.Contains("FOR SHARE", sql, StringComparison.OrdinalIgnoreCase);
                break;
            case "Sqlite":
                Assert.DoesNotContain("FOR SHARE", sql, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("HOLDLOCK", sql, StringComparison.OrdinalIgnoreCase);
                break;
            default:
                throw new InvalidOperationException($"Unknown provider: {provider}");
        }
    }

    public static void AssertAuditingTimestamp(string source, string? provider = null)
    {
        provider ??= CurrentProvider;
        switch (provider)
        {
            case "SqlServer":
                Assert.Contains("GETDATE()", source, StringComparison.OrdinalIgnoreCase);
                break;
            case "PostgreSql":
                Assert.True(
                    source.Contains("NOW()", StringComparison.OrdinalIgnoreCase)
                    || source.Contains("CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase),
                    "PostgreSql auditing timestamp expected.");
                break;
            case "MySql":
                Assert.True(
                    source.Contains("NOW()", StringComparison.OrdinalIgnoreCase)
                    || source.Contains("CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase),
                    "MySql auditing timestamp expected.");
                break;
            case "Sqlite":
                Assert.True(
                    source.Contains("datetime('now')", StringComparison.OrdinalIgnoreCase)
                    || source.Contains("CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase),
                    "Sqlite auditing timestamp expected.");
                break;
            default:
                throw new InvalidOperationException($"Unknown provider: {provider}");
        }
    }

    public static void AssertInClause(string sql, string column, string paramName, string? provider = null)
    {
        provider ??= CurrentProvider;
        if (provider == "PostgreSql")
            Assert.Contains($"{column} = ANY(@{paramName})", sql, StringComparison.OrdinalIgnoreCase);
        else
            Assert.Contains($"{column} IN @{paramName}", sql, StringComparison.OrdinalIgnoreCase);
    }

    public static void AssertBooleanFilterLiteral(string sql, string column, bool value, string? provider = null)
    {
        provider ??= CurrentProvider;
        var expected = provider == "PostgreSql"
            ? $"{column} = {(value ? "true" : "false")}"
            : $"{column} = {(value ? "1" : "0")}";
        Assert.Contains(expected, sql, StringComparison.OrdinalIgnoreCase);
    }

    public static void AssertSoftDeleteDeleteSql(string deleteSql)
    {
        Assert.Contains("is_deleted", deleteSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM", deleteSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UPDATE", deleteSql, StringComparison.OrdinalIgnoreCase);
    }

    public static void AssertTenancyFilter(string sql)
    {
        Assert.Contains("tenant_id", sql, StringComparison.OrdinalIgnoreCase);
    }

    public static void AssertPessimisticWriteSuffix(string sql, string? provider = null)
    {
        provider ??= CurrentProvider;
        switch (provider)
        {
            case "SqlServer":
                Assert.Contains("UPDLOCK", sql, StringComparison.OrdinalIgnoreCase);
                break;
            case "PostgreSql":
            case "MySql":
                Assert.Contains("FOR UPDATE", sql, StringComparison.OrdinalIgnoreCase);
                break;
            case "Sqlite":
                Assert.DoesNotContain("FOR UPDATE", sql, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("UPDLOCK", sql, StringComparison.OrdinalIgnoreCase);
                break;
            default:
                throw new InvalidOperationException($"Unknown provider: {provider}");
        }
    }

    public static void AssertSequenceSql(string source, string? provider = null)
    {
        provider ??= CurrentProvider;
        switch (provider)
        {
            case "SqlServer":
                Assert.Contains("NEXT VALUE FOR", source, StringComparison.OrdinalIgnoreCase);
                break;
            case "PostgreSql":
                Assert.Contains("nextval", source, StringComparison.OrdinalIgnoreCase);
                break;
            case "MySql":
                Assert.True(
                    source.Contains("NEXTVAL", StringComparison.OrdinalIgnoreCase)
                    || source.Contains("sequence", StringComparison.OrdinalIgnoreCase),
                    "MySql sequence call expected.");
                break;
            default:
                throw new InvalidOperationException($"Unknown provider: {provider}");
        }
    }

    public static void AssertStoredProcedureCall(string source, string? provider = null)
    {
        provider ??= CurrentProvider;
        switch (provider)
        {
            case "SqlServer":
                Assert.Contains("CommandType.StoredProcedure", source, StringComparison.OrdinalIgnoreCase);
                break;
            case "PostgreSql":
                Assert.Contains("SELECT * FROM", source, StringComparison.OrdinalIgnoreCase);
                break;
            case "MySql":
                Assert.Contains("CALL ", source, StringComparison.OrdinalIgnoreCase);
                break;
            default:
                throw new InvalidOperationException($"Unknown provider: {provider}");
        }
    }

    public static string ReadGeneratorSource(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Dapper.Npa.Generator", relativePath));
        return File.ReadAllText(path);
    }
}
