namespace DapperX.Abstractions.Query;

using System.Data;
using System.Linq.Expressions;
using DapperX.Abstractions.Paging;
using DapperX.Abstractions.Sorting;
using DapperX.Core.Enums;

public interface IQuery<T>
{
    IQuery<T> Where(Expression<Func<T, bool>> predicate);
    IQuery<T> OrderBy(Expression<Func<T, object?>> keySelector);
    IQuery<T> OrderByDescending(Expression<Func<T, object?>> keySelector);
    IQuery<T> ThenBy(Expression<Func<T, object?>> keySelector);
    IQuery<T> ThenByDescending(Expression<Func<T, object?>> keySelector);
    IQuery<T> Skip(int count);
    IQuery<T> Take(int count);
    IQuery<T> Include(string navigationProperty);
    IQuery<T> ThenInclude(string navigationProperty);
    IQuery<T> AsSplitQuery();
    IQuery<T> AsSlice();
    IQuery<T> IncludeDeleted();
    IQuery<T> WithLock(LockMode lockMode, int timeoutMs = 0);
    IQuery<TDto> Select<TDto>() where TDto : class;

    Task<IEnumerable<T>> ToListAsync(IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<Page<T>> ToPageAsync(Pageable pageable, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<Slice<T>> ToSliceAsync(Pageable pageable, IDbTransaction? transaction = null, CancellationToken ct = default);
    IAsyncEnumerable<T> ToAsyncEnumerable(IDbTransaction? transaction = null, CancellationToken ct = default);
}
