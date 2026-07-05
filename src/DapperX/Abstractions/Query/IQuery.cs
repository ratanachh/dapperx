using DapperX.Abstractions.Paging;

namespace DapperX.Abstractions.Query;

using System.Data;
using System.Linq.Expressions;
using Abstractions.Paging;
using Abstractions.Sorting;
using Core.Enums;

/// <summary>
/// A fluent, runtime-composable query built on top of an entity's compile-time base SELECT, obtained via
/// <see cref="DapperX.Abstractions.Repositories.IRepository{T, TId}.Query"/>. Every method returns
/// <c>this</c> for chaining; the query only executes when one of the terminal <c>...Async</c> methods is awaited.
/// </summary>
public interface IQuery<T>
{
    /// <summary>Adds a filter predicate. Multiple calls are combined with AND.</summary>
    IQuery<T> Where(Expression<Func<T, bool>> predicate);
    /// <summary>Sorts ascending by the selected property, resetting any prior ordering.</summary>
    IQuery<T> OrderBy(Expression<Func<T, object?>> keySelector);
    /// <summary>Sorts descending by the selected property, resetting any prior ordering.</summary>
    IQuery<T> OrderByDescending(Expression<Func<T, object?>> keySelector);
    /// <summary>Adds a secondary ascending sort key after a prior <see cref="OrderBy"/>/<see cref="OrderByDescending"/>.</summary>
    IQuery<T> ThenBy(Expression<Func<T, object?>> keySelector);
    /// <summary>Adds a secondary descending sort key after a prior <see cref="OrderBy"/>/<see cref="OrderByDescending"/>.</summary>
    IQuery<T> ThenByDescending(Expression<Func<T, object?>> keySelector);
    /// <summary>Skips the given number of rows.</summary>
    IQuery<T> Skip(int count);
    /// <summary>Limits the result to the given number of rows.</summary>
    IQuery<T> Take(int count);
    /// <summary>Eagerly loads the named navigation property alongside the root entity.</summary>
    IQuery<T> Include(string navigationProperty);
    /// <summary>Eagerly loads a navigation property nested under a prior <see cref="Include"/>.</summary>
    IQuery<T> ThenInclude(string navigationProperty);
    /// <summary>Loads each included relation with a separate query instead of a single joined query.</summary>
    IQuery<T> AsSplitQuery();
    /// <summary>Executes the terminal paging call as a <see cref="Slice{T}"/> (no COUNT query) instead of a <see cref="Page{T}"/>.</summary>
    IQuery<T> AsSlice();
    /// <summary>Includes rows excluded by <c>[SoftDelete]</c> filtering that would otherwise be hidden.</summary>
    IQuery<T> IncludeDeleted();
    /// <summary>Applies a row lock (e.g. <c>SELECT ... FOR UPDATE</c>) to the query.</summary>
    IQuery<T> WithLock(LockMode lockMode, int timeoutMs = 0);
    /// <summary>Projects results to <typeparamref name="TDto"/> instead of <typeparamref name="T"/>.</summary>
    IQuery<TDto> Select<TDto>() where TDto : class;

    /// <summary>Executes the query and returns all matching rows.</summary>
    Task<IEnumerable<T>> ToListAsync(IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Executes the query and returns the first matching row, or <c>null</c> if none match.</summary>
    Task<T?> FirstOrDefaultAsync(IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Executes the query as a single <see cref="Page{T}"/>, including a total row count.</summary>
    Task<Page<T>> ToPageAsync(Pageable pageable, IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Executes the query as a single <see cref="Slice{T}"/>, avoiding the COUNT query that <see cref="ToPageAsync"/> requires.</summary>
    Task<Slice<T>> ToSliceAsync(Pageable pageable, IDbTransaction? transaction = null, CancellationToken ct = default);
    /// <summary>Executes the query and streams rows as they're read from the underlying data reader.</summary>
    IAsyncEnumerable<T> ToAsyncEnumerable(IDbTransaction? transaction = null, CancellationToken ct = default);
}
