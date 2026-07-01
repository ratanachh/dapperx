namespace DapperX.Query.Projections;

using System.Data;
using System.Reflection;
using Dapper;
using DapperX.Core.Attributes;

public static class ProjectionMaterializer
{
    public static void EnsureProjection<TDto>()
    {
        if (typeof(TDto).GetCustomAttribute<ProjectionAttribute>() is null)
        {
            throw new InvalidOperationException(
                $"Type '{typeof(TDto).Name}' must be annotated with [Projection(From = typeof(TEntity))] for query projections.");
        }
    }

    public static IEnumerable<TDto> Materialize<TDto>(
        IDbConnection connection,
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null)
    {
        EnsureProjection<TDto>();
        return connection.Query<TDto>(sql, parameters, transaction);
    }
}
