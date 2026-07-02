namespace Dapper.Npa.Relations.Loaders;
using System.Data;
using Dapper;
/// <summary>
/// Loads LazyMap data — executes same SELECT as CollectionLoader, groups by [MapKey] column in-memory.
/// No dynamic SQL. Grouping is runtime LINQ data processing.
/// </summary>
public static class MapLoader
{
    public static async Task<IReadOnlyDictionary<TKey, TValue>> LoadAsync<TKey, TValue>(
        IDbConnection connection, string sql, object parameters,
        Func<TValue, TKey> keySelector, IDbTransaction? transaction = null)
        where TKey : notnull
    {
        var results = await connection.QueryAsync<TValue>(sql, parameters, transaction);
        return results.ToDictionary(keySelector);
    }
}
