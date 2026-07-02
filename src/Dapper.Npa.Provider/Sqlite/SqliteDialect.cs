using Dapper.Npa.Provider.Common;

namespace Dapper.Npa.Provider.Sqlite;
using Provider.Common;
public sealed class SqliteDialect : SqlDialect
{
    public override string PagingTemplate => "LIMIT @pageSize OFFSET @offset";
    public override string SliceTemplate  => "LIMIT @sliceSize OFFSET @offset";
    public override string IdentityReturn => "SELECT last_insert_rowid()";
    public override string CurrentTimestamp => "datetime('now')";
    public override string CurrentDate => "date('now')";
    public override string TrueValue => "1";
    public override string FalseValue => "0";
    public override bool SupportsSequences => false;
    public override bool SupportsPessimisticLocking => false;
    public override string GetPessimisticLockSql(string t) => throw new NotSupportedException("Pessimistic locking not supported on SQLite");
    public override string GetPessimisticReadLockSql(string t) => throw new NotSupportedException("Pessimistic read lock not supported on SQLite");
}
