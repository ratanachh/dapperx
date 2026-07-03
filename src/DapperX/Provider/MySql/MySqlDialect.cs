using DapperX.Provider.Common;

namespace DapperX.Provider.MySql;
using Provider.Common;
public sealed class MySqlDialect : SqlDialect
{
    public override string PagingTemplate => "LIMIT @pageSize OFFSET @offset";
    public override string SliceTemplate  => "LIMIT @sliceSize OFFSET @offset";
    public override string IdentityReturn => "SELECT LAST_INSERT_ID()";
    public override string CurrentTimestamp => "NOW()";
    public override string CurrentDate => "CURDATE()";
    public override string TrueValue => "1";
    public override string FalseValue => "0";
    public override bool SupportsSequences => false;
    public override bool SupportsPessimisticLocking => true;
    public override string GetPessimisticLockSql(string tableSql) => tableSql + " FOR UPDATE";
    public override string GetPessimisticReadLockSql(string tableSql) => tableSql + " FOR SHARE";
}
