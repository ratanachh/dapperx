using DapperX.Abstractions.Paging;
using DapperX.Abstractions.Query;
using DapperX.Abstractions.Sorting;

namespace DapperX.Abstractions.Repositories;

using System.Data;
using Abstractions.Paging;
using Abstractions.Query;
using Abstractions.Sorting;

/// <summary>
/// Compile-time-generated, reflection-free repository contract for entity <typeparamref name="T"/> with
/// identifier type <typeparamref name="TId"/>. Declare an interface extending
/// <see cref="IRepository{T, TId}"/> and annotate it with <c>[Repository]</c>; DapperX's source generator
/// emits a sealed implementation backed by Dapper, wired up automatically for dependency injection.
/// </summary>
/// <typeparam name="T">The mapped entity type (annotated with <c>[Entity]</c>).</typeparam>
/// <typeparam name="TId">The type of the entity's identifier property (annotated with <c>[Id]</c>).</typeparam>
public interface IRepository<T, TId>
{
    // CRUD
    /// <summary>Fetches a single entity by its identifier, or <c>null</c> if no matching row exists.</summary>
    Task<T?> GetByIdAsync(TId id, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Fetches every row for this entity, unordered.</summary>
    Task<IEnumerable<T>> GetAllAsync(bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Fetches every row for this entity, ordered by <paramref name="sort"/>.</summary>
    Task<IEnumerable<T>> GetAllAsync(Sort sort, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Fetches a single <see cref="Page{T}"/>, including a total row count for computing total pages.</summary>
    Task<Page<T>> GetAllAsync(Pageable pageable, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Fetches a single sorted <see cref="Page{T}"/>, including a total row count for computing total pages.</summary>
    Task<Page<T>> GetAllAsync(Sort sort, Pageable pageable, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Fetches a single <see cref="Slice{T}"/> — cheaper than <see cref="GetAllAsync(Pageable, bool, IDbTransaction?, CancellationToken)"/> since it avoids the COUNT query, at the cost of not knowing the total row count.</summary>
    Task<Slice<T>> GetAllSliceAsync(Pageable pageable, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Fetches a single sorted <see cref="Slice{T}"/> — cheaper than the paged overload since it avoids the COUNT query.</summary>
    Task<Slice<T>> GetAllSliceAsync(Sort sort, Pageable pageable, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Fetches every entity whose identifier is in <paramref name="ids"/>.</summary>
    Task<IEnumerable<T>> FindAllByIdAsync(IEnumerable<TId> ids, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Returns <c>true</c> if a row with the given identifier exists.</summary>
    Task<bool> ExistsByIdAsync(TId id, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Returns the total number of rows for this entity.</summary>
    Task<long> CountAsync(bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);

    /// <summary>Inserts a single entity.</summary>
    Task InsertAsync(T entity, IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Updates a single entity, matching on its identifier (and version column, if <c>[Version]</c> is present).</summary>
    Task UpdateAsync(T entity, IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Deletes a single entity — a soft delete if <c>[SoftDelete]</c> is present, otherwise a hard delete.</summary>
    Task DeleteAsync(T entity, IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Deletes a single entity by its identifier — a soft delete if <c>[SoftDelete]</c> is present, otherwise a hard delete.</summary>
    Task DeleteByIdAsync(TId id, IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Deletes every entity whose identifier is in <paramref name="ids"/>.</summary>
    Task DeleteAllByIdAsync(IEnumerable<TId> ids, IDbTransaction? transaction = null, CancellationToken ct = default);

    // Upsert
    /// <summary>Inserts the entity if no row with its identifier exists, otherwise updates it.</summary>
    Task UpsertAsync(T entity, IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Upserts a batch of entities, chunked into groups of <paramref name="batchSize"/> (defaults to <see cref="DapperX.Runtime.Configuration.DapperXOptions.BatchSize"/>).</summary>
    Task UpsertManyAsync(IEnumerable<T> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null);

    // Batch
    /// <summary>Inserts a batch of entities, chunked into groups of <paramref name="batchSize"/>; switches to a provider-specific bulk insert once the batch exceeds <paramref name="bulkThreshold"/> rows.</summary>
    Task InsertManyAsync(IEnumerable<T> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null, int? bulkThreshold = null);
    /// <summary>Updates a batch of entities, chunked into groups of <paramref name="batchSize"/>.</summary>
    Task UpdateManyAsync(IEnumerable<T> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null);
    /// <summary>Deletes a batch of entities, chunked into groups of <paramref name="batchSize"/>.</summary>
    Task DeleteManyAsync(IEnumerable<T> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null);

    // Graph
    /// <summary>Inserts <paramref name="root"/> together with its mapped relations, in dependency order.</summary>
    Task InsertGraphAsync(T root, IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Updates <paramref name="root"/> together with its mapped relations, in dependency order.</summary>
    Task UpdateGraphAsync(T root, IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Deletes <paramref name="root"/> together with its mapped relations, in reverse dependency order.</summary>
    Task DeleteGraphAsync(T root, IDbTransaction? transaction = null, CancellationToken ct = default);

    // Transaction helper
    /// <summary>Opens a transaction on the repository's connection and runs <paramref name="work"/> within it, committing on success and rolling back on exception.</summary>
    Task WithTransactionAsync(Func<IDbTransaction, Task> work, CancellationToken ct = default);

    /// <summary>Fluent runtime query over compile-time base SELECT (Requirements Pattern 4).</summary>
    IQuery<T> Query();
}
