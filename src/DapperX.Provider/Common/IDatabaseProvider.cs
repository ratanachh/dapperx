namespace DapperX.Provider.Common;

using DapperX.Core.Enums;

public interface IDatabaseProvider
{
    DatabaseProvider Provider { get; }
    string GetPagingSql(string baseSql, int pageSize);
    string GetSliceSql(string baseSql, int pageSize);
    string GetIdentityReturnSql(string insertSql, string idColumn);
    string GetUpsertSql(string table, string[] columns, string[] keyColumns);
    string GetPessimisticLockHint();
    string GetPessimisticReadLockHint();
    string GetLockTimeoutSql(int timeoutMs);
    string? GetSequenceSql(string sequenceName);
    bool SupportsBulkInsert { get; }
    IBulkInsertExecutor? BulkInsertExecutor { get; }
}
