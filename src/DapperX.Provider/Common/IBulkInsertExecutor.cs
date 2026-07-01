namespace DapperX.Provider.Common;

public interface IBulkInsertExecutor
{
    Task InsertAsync(BulkInsertContext context, CancellationToken cancellationToken = default);
}
