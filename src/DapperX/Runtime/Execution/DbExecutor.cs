using DapperX.Runtime.Logging;
using DapperX.Abstractions.Configuration;
using DapperX.Abstractions.Exceptions;
using DapperX.Core.Enums;

namespace DapperX.Runtime.Execution;

using System.Data;
using Dapper;
using DapperX.Abstractions.Configuration;
using DapperX.Abstractions.Exceptions;
using DapperX.Core.Enums;
using Runtime.Logging;

/// <summary>
/// Central Dapper execution wrapper — SQL context on failures and structured logging (Requirements §19).
/// </summary>
public static class DbExecutor
{
    public static DbExecutionLogContext CreateLogContext(
        string methodName,
        IDapperXOptions? options,
        DatabaseProvider provider)
        => DbExecutionLogContext.Create(methodName, options, provider);

    public static async Task<int> ExecuteAsync(
        IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        DbExecutionLogContext logContext = default)
    {
        SqlExecutionLogger.TryLog(in logContext, sql, param);
        try
        {
            return await connection.ExecuteAsync(sql, param, transaction, commandTimeout, commandType)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not SqlExecutionException)
        {
            throw Wrap(sql, ex);
        }
    }

    public static async Task<T> ExecuteScalarAsync<T>(
        IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        DbExecutionLogContext logContext = default)
    {
        SqlExecutionLogger.TryLog(in logContext, sql, param);
        try
        {
            return await connection.ExecuteScalarAsync<T>(sql, param, transaction, commandTimeout, commandType)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not SqlExecutionException)
        {
            throw Wrap(sql, ex);
        }
    }

    public static async Task<IEnumerable<T>> QueryAsync<T>(
        IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        DbExecutionLogContext logContext = default)
    {
        SqlExecutionLogger.TryLog(in logContext, sql, param);
        try
        {
            return await connection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not SqlExecutionException)
        {
            throw Wrap(sql, ex);
        }
    }

    public static async Task<T?> QueryFirstOrDefaultAsync<T>(
        IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        DbExecutionLogContext logContext = default)
    {
        SqlExecutionLogger.TryLog(in logContext, sql, param);
        try
        {
            return await connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not SqlExecutionException)
        {
            throw Wrap(sql, ex);
        }
    }

    public static async Task<SqlMapper.GridReader> QueryMultipleAsync(
        IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        DbExecutionLogContext logContext = default)
    {
        SqlExecutionLogger.TryLog(in logContext, sql, param);
        try
        {
            return await connection.QueryMultipleAsync(sql, param, transaction, commandTimeout, commandType)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not SqlExecutionException)
        {
            throw Wrap(sql, ex);
        }
    }

    public static TProperty ConvertToProperty<TColumn, TProperty>(
        Func<TColumn, TProperty> convert,
        TColumn columnValue,
        string propertyName)
    {
        try
        {
            return convert(columnValue);
        }
        catch (Exception ex) when (ex is not SqlExecutionException)
        {
            throw new SqlExecutionException(
                $"Value converter failed while reading property '{propertyName}'.",
                string.Empty,
                ex);
        }
    }

    public static TColumn ConvertToColumn<TProperty, TColumn>(
        Func<TProperty, TColumn> convert,
        TProperty propertyValue,
        string propertyName)
    {
        try
        {
            return convert(propertyValue);
        }
        catch (Exception ex) when (ex is not SqlExecutionException)
        {
            throw new SqlExecutionException(
                $"Value converter failed while writing property '{propertyName}'.",
                string.Empty,
                ex);
        }
    }

    public static IAsyncEnumerable<T> QueryUnbufferedAsync<T>(
        IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        DbExecutionLogContext logContext = default)
    {
        if (connection is not System.Data.Common.DbConnection dbConnection)
        {
            throw new InvalidOperationException(
                "QueryUnbufferedAsync requires a DbConnection implementation.");
        }

        SqlExecutionLogger.TryLog(in logContext, sql, param);
        try
        {
            return dbConnection.QueryUnbufferedAsync<T>(
                sql, param, transaction as System.Data.Common.DbTransaction, commandTimeout, commandType);
        }
        catch (Exception ex) when (ex is not SqlExecutionException)
        {
            throw Wrap(sql, ex);
        }
    }

    private static SqlExecutionException Wrap(string sql, Exception inner)
        => new($"SQL execution failed. Statement: {sql}", sql, inner);
}
