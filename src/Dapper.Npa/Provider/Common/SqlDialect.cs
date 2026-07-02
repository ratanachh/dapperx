namespace Dapper.Npa.Provider.Common;
public abstract class SqlDialect
{
    public abstract string PagingTemplate { get; }      // OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY
    public abstract string SliceTemplate { get; }       // OFFSET @offset ROWS FETCH NEXT @sliceSize ROWS ONLY
    public abstract string IdentityReturn { get; }      // e.g. OUTPUT INSERTED.id / RETURNING id
    public abstract string CurrentTimestamp { get; }
    public abstract string CurrentDate { get; }
    public abstract string TrueValue { get; }
    public abstract string FalseValue { get; }
    public abstract bool SupportsSequences { get; }
    public abstract bool SupportsPessimisticLocking { get; }
    public virtual string GetSequenceSql(string name) => throw new NotSupportedException();
    public virtual string GetPessimisticLockSql(string tableSql) => tableSql;
    public virtual string GetPessimisticReadLockSql(string tableSql) => tableSql;
    public virtual string GetLockTimeoutSql(int timeoutMs) => string.Empty;
}
