namespace DapperX.Runtime.Repositories;

using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DapperX.Abstractions.Configuration;
using DapperX.Abstractions.Paging;
using DapperX.Abstractions.Query;
using DapperX.Abstractions.Repositories;
using DapperX.Abstractions.Sorting;
using DapperX.Abstractions.Exceptions;
using DapperX.Core.Enums;
using DapperX.Runtime.Execution;

/// <summary>
/// Abstract base class for all DapperX repositories.
/// All Dapper call logic lives here — never duplicated in generated classes.
/// The generator emits sealed subclasses that override the abstract SQL string properties
/// with compile-time literals, and adds derived query method bodies.
/// </summary>
public abstract class DapperXRepositoryBase<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : class
{
    protected readonly IDbConnection _connection;

    protected IDapperXOptions? Options { get; set; }

    protected abstract DatabaseProvider Provider { get; }

    protected DapperXRepositoryBase(IDbConnection connection)
        => _connection = connection;

    protected DbExecutionLogContext LogContext(string methodName)
        => DbExecutor.CreateLogContext(methodName, Options, Provider);

    // ─── Abstract SQL properties (overridden by generator with compile-time literals) ──

    protected abstract string SelectByIdSql   { get; }
    protected abstract string SelectAllSql    { get; }
    protected abstract string SelectByIdsSql  { get; }
    protected abstract string ExistsSql       { get; }
    protected abstract string CountSql        { get; }
    protected abstract string InsertSql       { get; }
    protected abstract string UpdateSql       { get; }
    protected abstract string DeleteSql       { get; }
    protected abstract string DeleteByIdSql   { get; }
    protected abstract string DeleteByIdsSql  { get; }
    protected abstract string SelectAllPageSql  { get; }
    protected abstract string SelectAllSliceSql { get; }
    protected abstract string CountPageSql    { get; }
    protected abstract string UpsertSql       { get; }

    protected virtual string GetSortFragment(Sort sort)
        => throw new InvalidSortException(sort.Column);

    /// <summary>Override in generated repository when entity declares lifecycle methods.</summary>
    protected virtual void OnPrePersist(TEntity entity) { }
    protected virtual void OnPostPersist(TEntity entity) { }
    protected virtual void OnPreUpdate(TEntity entity) { }
    protected virtual void OnPostUpdate(TEntity entity) { }
    protected virtual void OnPreRemove(TEntity entity) { }
    protected virtual void OnPostRemove(TEntity entity) { }
    protected virtual void OnPostLoad(TEntity entity) { }

    protected void ApplyPostLoad(TEntity? entity)
    {
        if (entity is not null)
            OnPostLoad(entity);
    }

    protected void ApplyPostLoad(IEnumerable<TEntity> entities)
    {
        foreach (var entity in entities)
            OnPostLoad(entity);
    }

    // ─── IRepository<TEntity, TId> implementation ─────────────────────────────────

    public virtual async Task<TEntity?> GetByIdAsync(TId id, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var result = await DbExecutor.QueryFirstOrDefaultAsync<TEntity>(
            _connection, SelectByIdSql, new { Id = id }, transaction, logContext: LogContext(nameof(GetByIdAsync)));
        ApplyPostLoad(result);
        return result;
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var results = await DbExecutor.QueryAsync<TEntity>(
            _connection, SelectAllSql, null, transaction, logContext: LogContext(nameof(GetAllAsync)));
        ApplyPostLoad(results);
        return results;
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(Sort sort, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var sql = SelectAllSql + GetSortFragment(sort);
        var results = await DbExecutor.QueryAsync<TEntity>(
            _connection, sql, null, transaction, logContext: LogContext(nameof(GetAllAsync)));
        ApplyPostLoad(results);
        return results;
    }

    public virtual async Task<Page<TEntity>> GetAllAsync(Pageable pageable, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var total = await DbExecutor.ExecuteScalarAsync<long>(
            _connection, CountPageSql, null, transaction, logContext: LogContext(nameof(GetAllAsync)));
        var content = (await DbExecutor.QueryAsync<TEntity>(
            _connection, SelectAllPageSql, new { offset = pageable.Offset, pageSize = pageable.PageSize }, transaction,
            logContext: LogContext(nameof(GetAllAsync)))).AsList();
        ApplyPostLoad(content);
        return new Page<TEntity>(content, total, pageable);
    }

    public virtual async Task<Page<TEntity>> GetAllAsync(Sort sort, Pageable pageable, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var total = await DbExecutor.ExecuteScalarAsync<long>(
            _connection, CountPageSql, null, transaction, logContext: LogContext(nameof(GetAllAsync)));
        var sql = ApplySortToPagedSql(SelectAllPageSql, GetSortFragment(sort));
        var content = (await DbExecutor.QueryAsync<TEntity>(
            _connection, sql, new { offset = pageable.Offset, pageSize = pageable.PageSize }, transaction,
            logContext: LogContext(nameof(GetAllAsync)))).AsList();
        ApplyPostLoad(content);
        return new Page<TEntity>(content, total, pageable);
    }

    public virtual async Task<Slice<TEntity>> GetAllSliceAsync(Pageable pageable, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var results = (await DbExecutor.QueryAsync<TEntity>(
            _connection, SelectAllSliceSql, new { offset = pageable.Offset, sliceSize = pageable.PageSize + 1 }, transaction,
            logContext: LogContext(nameof(GetAllSliceAsync)))).AsList();
        ApplyPostLoad(results);
        return new Slice<TEntity>(results, pageable.PageSize);
    }

    public virtual async Task<Slice<TEntity>> GetAllSliceAsync(Sort sort, Pageable pageable, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var sql = ApplySortToPagedSql(SelectAllSliceSql, GetSortFragment(sort));
        var results = (await DbExecutor.QueryAsync<TEntity>(
            _connection, sql, new { offset = pageable.Offset, sliceSize = pageable.PageSize + 1 }, transaction,
            logContext: LogContext(nameof(GetAllSliceAsync)))).AsList();
        ApplyPostLoad(results);
        return new Slice<TEntity>(results, pageable.PageSize);
    }

    public virtual async Task<IEnumerable<TEntity>> FindAllByIdAsync(IEnumerable<TId> ids, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var results = await DbExecutor.QueryAsync<TEntity>(
            _connection, SelectByIdsSql, new { ids }, transaction, logContext: LogContext(nameof(FindAllByIdAsync)));
        ApplyPostLoad(results);
        return results;
    }

    public virtual async Task<bool> ExistsByIdAsync(TId id, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)
        => await DbExecutor.ExecuteScalarAsync<int>(
            _connection, ExistsSql, new { Id = id }, transaction, logContext: LogContext(nameof(ExistsByIdAsync))) == 1;

    public virtual async Task<long> CountAsync(bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)
        => await DbExecutor.ExecuteScalarAsync<long>(
            _connection, CountSql, null, transaction, logContext: LogContext(nameof(CountAsync)));

    public virtual async Task InsertAsync(TEntity entity, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        OnPrePersist(entity);
        await DbExecutor.ExecuteAsync(_connection, InsertSql, entity, transaction, logContext: LogContext(nameof(InsertAsync)));
        OnPostPersist(entity);
    }

    public virtual async Task UpdateAsync(TEntity entity, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        OnPreUpdate(entity);
        var affected = await DbExecutor.ExecuteAsync(_connection, UpdateSql, entity, transaction, logContext: LogContext(nameof(UpdateAsync)));
        if (affected == 0) throw new ConcurrencyException("Update failed — record may have been modified.");
        OnPostUpdate(entity);
    }

    public virtual async Task DeleteAsync(TEntity entity, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        OnPreRemove(entity);
        var affected = await DbExecutor.ExecuteAsync(_connection, DeleteSql, entity, transaction, logContext: LogContext(nameof(DeleteAsync)));
        if (affected == 0) throw new ConcurrencyException("Delete failed — record may have been modified.");
        OnPostRemove(entity);
    }

    public virtual async Task DeleteByIdAsync(TId id, IDbTransaction? transaction = null, CancellationToken ct = default)
        => await DbExecutor.ExecuteAsync(_connection, DeleteByIdSql, new { Id = id }, transaction, logContext: LogContext(nameof(DeleteByIdAsync)));

    public virtual async Task DeleteAllByIdAsync(IEnumerable<TId> ids, IDbTransaction? transaction = null, CancellationToken ct = default)
        => await DbExecutor.ExecuteAsync(_connection, DeleteByIdsSql, new { ids }, transaction, logContext: LogContext(nameof(DeleteAllByIdAsync)));

    public virtual async Task UpsertAsync(TEntity entity, IDbTransaction? transaction = null, CancellationToken ct = default)
        => await DbExecutor.ExecuteAsync(_connection, UpsertSql, entity, transaction, logContext: LogContext(nameof(UpsertAsync)));

    public virtual async Task UpsertManyAsync(IEnumerable<TEntity> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null)
    {
        foreach (var entity in entities)
            await DbExecutor.ExecuteAsync(_connection, UpsertSql, entity, transaction, logContext: LogContext(nameof(UpsertManyAsync)));
    }

    public virtual Task InsertManyAsync(IEnumerable<TEntity> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null, int? bulkThreshold = null)
        => throw new NotImplementedException("InsertManyAsync is emitted by the generator.");

    public virtual Task UpdateManyAsync(IEnumerable<TEntity> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null)
        => throw new NotImplementedException("UpdateManyAsync is emitted by the generator.");

    public virtual Task DeleteManyAsync(IEnumerable<TEntity> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null)
        => throw new NotImplementedException("DeleteManyAsync is emitted by the generator.");

    public virtual Task InsertGraphAsync(TEntity root, IDbTransaction? transaction = null, CancellationToken ct = default)
        => throw new NotImplementedException("InsertGraphAsync is emitted by the generator.");

    public virtual Task UpdateGraphAsync(TEntity root, IDbTransaction? transaction = null, CancellationToken ct = default)
        => throw new NotImplementedException("UpdateGraphAsync is emitted by the generator.");

    public virtual Task DeleteGraphAsync(TEntity root, IDbTransaction? transaction = null, CancellationToken ct = default)
        => throw new NotImplementedException("DeleteGraphAsync is emitted by the generator.");

    public virtual async Task WithTransactionAsync(Func<IDbTransaction, Task> work, CancellationToken ct = default)
    {
        using var transaction = _connection.BeginTransaction();
        try { await work(transaction); transaction.Commit(); }
        catch { transaction.Rollback(); throw; }
    }

    public virtual IQuery<TEntity> Query()
        => throw new NotSupportedException("Query() is emitted on generated repository implementations.");

    protected static string ApplySortToPagedSql(string pageSql, string sortFragment)
    {
        if (string.IsNullOrEmpty(sortFragment))
            return pageSql;

        const string offsetToken = " OFFSET ";
        var offsetIndex = pageSql.IndexOf(offsetToken, StringComparison.OrdinalIgnoreCase);
        if (offsetIndex < 0)
            return pageSql + sortFragment;

        var beforeOffset = pageSql[..offsetIndex];
        var afterOffset = pageSql[offsetIndex..];
        var orderIndex = beforeOffset.LastIndexOf(" ORDER BY ", StringComparison.OrdinalIgnoreCase);
        if (orderIndex >= 0)
            beforeOffset = beforeOffset[..orderIndex];

        return beforeOffset + sortFragment + afterOffset;
    }
}
