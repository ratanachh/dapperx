using DapperX.Abstractions.Paging;
using DapperX.Abstractions.Query;
using DapperX.Abstractions.Sorting;

namespace DapperX.Abstractions.Repositories;

using System.Data;
using Abstractions.Paging;
using Abstractions.Query;
using Abstractions.Sorting;

public interface IRepository<T, TId>
{
    // CRUD
    Task<T?> GetByIdAsync(TId id, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(Sort sort, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<Page<T>> GetAllAsync(Pageable pageable, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<Page<T>> GetAllAsync(Sort sort, Pageable pageable, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<Slice<T>> GetAllSliceAsync(Pageable pageable, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<Slice<T>> GetAllSliceAsync(Sort sort, Pageable pageable, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<IEnumerable<T>> FindAllByIdAsync(IEnumerable<TId> ids, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<bool> ExistsByIdAsync(TId id, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<long> CountAsync(bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);

    Task InsertAsync(T entity, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task UpdateAsync(T entity, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task DeleteAsync(T entity, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task DeleteByIdAsync(TId id, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task DeleteAllByIdAsync(IEnumerable<TId> ids, IDbTransaction? transaction = null, CancellationToken ct = default);

    // Upsert
    Task UpsertAsync(T entity, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task UpsertManyAsync(IEnumerable<T> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null);

    // Batch
    Task InsertManyAsync(IEnumerable<T> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null, int? bulkThreshold = null);
    Task UpdateManyAsync(IEnumerable<T> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null);
    Task DeleteManyAsync(IEnumerable<T> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null);

    // Graph
    Task InsertGraphAsync(T root, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task UpdateGraphAsync(T root, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task DeleteGraphAsync(T root, IDbTransaction? transaction = null, CancellationToken ct = default);

    // Transaction helper
    Task WithTransactionAsync(Func<IDbTransaction, Task> work, CancellationToken ct = default);

    /// <summary>Fluent runtime query over compile-time base SELECT (Requirements Pattern 4).</summary>
    IQuery<T> Query();
}
