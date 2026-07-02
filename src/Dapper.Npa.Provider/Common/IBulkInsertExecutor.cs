namespace Dapper.Npa.Provider.Common;

public interface IBulkInsertExecutor
{
    Task InsertAsync(BulkInsertContext context, CancellationToken cancellationToken = default);
}
