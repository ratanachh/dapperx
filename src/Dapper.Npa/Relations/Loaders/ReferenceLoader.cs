namespace Dapper.Npa.Relations.Loaders;
using System.Data;
using Dapper;
public static class ReferenceLoader
{
    public static async Task<T?> LoadAsync<T>(
        IDbConnection connection, string sql, object parameters, IDbTransaction? transaction = null)
        where T : class
        => await connection.QueryFirstOrDefaultAsync<T>(sql, parameters, transaction);
}
