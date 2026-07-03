using DapperX.Core.Enums;

namespace DapperX.Provider.Common;

using DapperX.Core.Enums;

public abstract class DatabaseProviderBase : IDatabaseProvider
{
    protected DatabaseProviderBase(DatabaseProvider provider, SqlDialect dialect, IBulkInsertExecutor? bulkInsertExecutor)
    {
        Provider = provider;
        Dialect = dialect;
        BulkInsertExecutor = bulkInsertExecutor;
    }

    protected SqlDialect Dialect { get; }

    public DatabaseProvider Provider { get; }

    public bool SupportsBulkInsert => BulkInsertExecutor is not null;

    public IBulkInsertExecutor? BulkInsertExecutor { get; }

    public string GetPagingSql(string baseSql, int pageSize)
        => $"{baseSql} {string.Format(Dialect.PagingTemplate, pageSize)}";

    public string GetSliceSql(string baseSql, int pageSize)
        => $"{baseSql} {string.Format(Dialect.SliceTemplate, pageSize + 1)}";

    public string GetIdentityReturnSql(string insertSql, string idColumn)
        => insertSql.Contains(Dialect.IdentityReturn, StringComparison.Ordinal)
            ? insertSql
            : $"{insertSql}; {Dialect.IdentityReturn.Replace("id", idColumn, StringComparison.OrdinalIgnoreCase)}";

    public virtual string GetUpsertSql(string table, string[] columns, string[] keyColumns)
        => throw new NotSupportedException($"Upsert SQL is generated at compile time for provider {Provider}.");

    public string GetPessimisticLockHint()
        => Dialect.GetPessimisticLockSql(string.Empty).Trim();

    public string GetPessimisticReadLockHint()
        => Dialect.GetPessimisticReadLockSql(string.Empty).Trim();

    public string GetLockTimeoutSql(int timeoutMs)
        => Dialect.GetLockTimeoutSql(timeoutMs);

    public string? GetSequenceSql(string sequenceName)
        => Dialect.SupportsSequences ? Dialect.GetSequenceSql(sequenceName) : null;
}
