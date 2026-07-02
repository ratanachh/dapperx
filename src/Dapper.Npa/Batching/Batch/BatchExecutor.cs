using System.Data;

namespace Dapper.Npa.Batching.Batch;

public static class BatchExecutor
{
    public static async Task ExecuteAsync<T>(IDbConnection connection, string sql, IEnumerable<T> entities, IDbTransaction? transaction = null)
        => await connection.ExecuteAsync(sql, entities, transaction);
}
