namespace DapperX.Relations.Loaders;
using System.Data;
using Dapper;
public static class CollectionLoader
{
    public static async Task<IEnumerable<T>> LoadAsync<T>(
        IDbConnection connection, string sql, object parameters, IDbTransaction? transaction = null)
        => await connection.QueryAsync<T>(sql, parameters, transaction);
}
