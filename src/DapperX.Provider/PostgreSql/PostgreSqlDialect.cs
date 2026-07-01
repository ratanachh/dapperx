namespace DapperX.Provider.PostgreSql;
using DapperX.Provider.Common;
public sealed class PostgreSqlDialect : SqlDialect
{
    public override string PagingTemplate => "LIMIT @pageSize OFFSET @offset";
    public override string SliceTemplate  => "LIMIT @sliceSize OFFSET @offset";
    public override string IdentityReturn => "RETURNING id";
    public override string CurrentTimestamp => "CURRENT_TIMESTAMP";
    public override string CurrentDate => "CURRENT_DATE";
    public override string TrueValue => "TRUE";
    public override string FalseValue => "FALSE";
    public override bool SupportsSequences => true;
    public override bool SupportsPessimisticLocking => true;
    public override string GetSequenceSql(string name) => $"nextval('{name}')";
    public override string GetPessimisticLockSql(string tableSql) => tableSql + " FOR UPDATE";
    public override string GetPessimisticReadLockSql(string tableSql) => tableSql + " FOR SHARE";
    public override string GetLockTimeoutSql(int timeoutMs) => $"SET lock_timeout = {timeoutMs}";
}
