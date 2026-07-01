namespace DapperX.Provider.SqlServer;
using DapperX.Provider.Common;
public sealed class SqlServerDialect : SqlDialect
{
    public override string PagingTemplate => "OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
    public override string SliceTemplate  => "OFFSET @offset ROWS FETCH NEXT @sliceSize ROWS ONLY";
    public override string IdentityReturn => "OUTPUT INSERTED.id";
    public override string CurrentTimestamp => "GETDATE()";
    public override string CurrentDate => "CAST(GETDATE() AS DATE)";
    public override string TrueValue => "1";
    public override string FalseValue => "0";
    public override bool SupportsSequences => true;
    public override bool SupportsPessimisticLocking => true;
    public override string GetSequenceSql(string name) => $"NEXT VALUE FOR {name}";
    public override string GetPessimisticLockSql(string tableSql) => tableSql.Replace("FROM ", "FROM ") + " WITH (UPDLOCK, ROWLOCK)";
    public override string GetPessimisticReadLockSql(string tableSql) => tableSql + " WITH (HOLDLOCK, ROWLOCK)";
    public override string GetLockTimeoutSql(int timeoutMs) => $"SET LOCK_TIMEOUT {timeoutMs}";
}
