using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Dapper;
using Dapper.Npa.Abstractions.Configuration;
using Dapper.Npa.Abstractions.Paging;
using Dapper.Npa.Abstractions.Query;
using Dapper.Npa.Core.Enums;
using Dapper.Npa.Query.Projections;
using Dapper.Npa.Query.Query;
using Dapper.Npa.Runtime.Execution;

namespace Dapper.Npa.Runtime.Query;

/// <summary>Runtime fluent query implementation (Requirements Pattern 4).</summary>
public sealed class RepositoryQuery<T> : IQuery<T> where T : class
{
    private readonly IDbConnection _connection;
    private readonly string _baseSql;
    private readonly string _countFromClause;
    private readonly QueryRuntimeConfig _config;
    private readonly Func<string, string> _resolveColumn;
    private readonly Action<T>? _postLoad;
    private readonly Func<IList<T>, IReadOnlyList<string>, IDbTransaction?, Task>? _applySplitIncludes;
    private readonly QueryBuilder<T> _builder = new();
    private readonly QueryBuilderStateSnapshot? _carriedState;
    private readonly IDapperXOptions? _options;
    private readonly DatabaseProvider _provider;

    public RepositoryQuery(
        IDbConnection connection,
        string baseSql,
        string countFromClause,
        Func<string, string> resolveColumn,
        QueryRuntimeConfig config,
        Action<T>? postLoad = null,
        Func<IList<T>, IReadOnlyList<string>, IDbTransaction?, Task>? applySplitIncludes = null,
        QueryBuilderStateSnapshot? carriedState = null,
        IDapperXOptions? options = null,
        DatabaseProvider provider = DatabaseProvider.SqlServer)
    {
        _connection = connection;
        _baseSql = baseSql;
        _countFromClause = countFromClause;
        _resolveColumn = resolveColumn;
        _config = config;
        _postLoad = postLoad;
        _applySplitIncludes = applySplitIncludes;
        _carriedState = carriedState;
        _options = options;
        _provider = provider;
    }

    private DbExecutionLogContext LogContext(string methodName)
        => DbExecutor.CreateLogContext(methodName, _options, _provider);

    public IQuery<T> Where(Expression<Func<T, bool>> predicate) { _builder.Where(predicate); return this; }
    public IQuery<T> OrderBy(Expression<Func<T, object?>> selector) { _builder.OrderBy(selector); return this; }
    public IQuery<T> OrderByDescending(Expression<Func<T, object?>> selector) { _builder.OrderByDescending(selector); return this; }
    public IQuery<T> ThenBy(Expression<Func<T, object?>> selector) { _builder.ThenBy(selector); return this; }
    public IQuery<T> ThenByDescending(Expression<Func<T, object?>> selector) { _builder.ThenByDescending(selector); return this; }
    public IQuery<T> Skip(int count) { _builder.Skip(count); return this; }
    public IQuery<T> Take(int count) { _builder.Take(count); return this; }
    public IQuery<T> Include(string navigationProperty) { _builder.Include(navigationProperty); return this; }
    public IQuery<T> ThenInclude(string navigationProperty) { _builder.ThenInclude(navigationProperty); return this; }
    public IQuery<T> AsSplitQuery() { _builder.AsSplitQuery(); return this; }
    public IQuery<T> AsSlice() { _builder.AsSlice(); return this; }
    public IQuery<T> IncludeDeleted()
    {
        if (!_config.SoftDeleteSupported)
        {
            throw new InvalidOperationException(
                "IncludeDeleted() requires the entity to be marked with [SoftDelete].");
        }
        _builder.IncludeDeleted();
        return this;
    }
    public IQuery<T> WithLock(LockMode lockMode, int timeoutMs = 0) { _builder.WithLock(lockMode, timeoutMs); return this; }

    public IQuery<TDto> Select<TDto>() where TDto : class
    {
        ProjectionMaterializer.EnsureProjection<TDto>();
        var key = typeof(TDto).FullName ?? typeof(TDto).Name;
        if (!TryResolveProjectionSql(key, out var projectionSql))
        {
            throw new NotSupportedException(
                $"No compile-time projection catalog entry for '{typeof(TDto).Name}'. " +
                "Ensure the DTO is annotated with [Projection(From = typeof(TEntity))].");
        }

        return new RepositoryQuery<TDto>(
            _connection,
            projectionSql,
            _countFromClause,
            _resolveColumn,
            _config,
            postLoad: null,
            applySplitIncludes: null,
            carriedState: BuildEffectiveState(),
            options: _options,
            provider: _provider);
    }

    private QueryBuilderStateSnapshot BuildEffectiveState()
    {
        var current = QueryBuilderStateSnapshot.From(_builder.Build());
        return _carriedState?.MergeWith(current) ?? current;
    }

    internal QueryBuilderStateSnapshot BuildEffectiveStateForTests() => BuildEffectiveState();

    public async Task<IEnumerable<T>> ToListAsync(IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var state = BuildEffectiveState();
        var (lockPreambleSql, sql, parameters) = QueryExecutor.BuildSelectSql(_baseSql, state, _resolveColumn, _config);
        var dapperParams = ToDapperParams(parameters);
        MergeGlobalFilterParameters(dapperParams);
        await ExecuteLockPreambleAsync(lockPreambleSql, dapperParams, transaction).ConfigureAwait(false);
        var list = (await DbExecutor.QueryAsync<T>(
            _connection, sql, dapperParams, transaction,
            logContext: LogContext("Query.ToListAsync"))).AsList();
        ApplyPostLoad(list);
        await ApplySplitIncludesAsync(list, state, transaction).ConfigureAwait(false);
        return list;
    }

    public async Task<T?> FirstOrDefaultAsync(IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        _builder.Take(1);
        var list = (await ToListAsync(transaction, ct).ConfigureAwait(false)).AsList();
        return list.Count > 0 ? list[0] : null;
    }

    public async Task<Page<T>> ToPageAsync(Pageable pageable, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var state = BuildEffectiveState();
        var (countSql, countParams) = QueryExecutor.BuildCountSql(_countFromClause, state, _resolveColumn, _config);
        var total = await DbExecutor.ExecuteScalarAsync<long>(
            _connection, countSql, ToDapperParams(countParams), transaction,
            logContext: LogContext("Query.ToPageAsync")).ConfigureAwait(false);

        _builder.Skip(pageable.Offset);
        _builder.Take(pageable.PageSize);
        var content = (await ToListAsync(transaction, ct).ConfigureAwait(false)).AsList();
        return new Page<T>(content, total, pageable);
    }

    public async Task<Slice<T>> ToSliceAsync(Pageable pageable, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        _builder.AsSlice();
        _builder.Skip(pageable.Offset);
        _builder.Take(pageable.PageSize + 1);
        var raw = (await ToListAsync(transaction, ct).ConfigureAwait(false)).AsList();
        return new Slice<T>(raw, pageable.PageSize);
    }

    public async IAsyncEnumerable<T> ToAsyncEnumerable(
        IDbTransaction? transaction = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var (lockPreambleSql, sql, parameters) = QueryExecutor.BuildSelectSql(_baseSql, BuildEffectiveState(), _resolveColumn, _config);
        var dapperParams = ToDapperParams(parameters);
        await ExecuteLockPreambleAsync(lockPreambleSql, dapperParams, transaction).ConfigureAwait(false);
        await foreach (var item in DbExecutor.QueryUnbufferedAsync<T>(
                           _connection, sql, dapperParams, transaction,
                           logContext: LogContext("Query.ToAsyncEnumerable"))
                           .WithCancellation(ct))
        {
            _postLoad?.Invoke(item);
            yield return item;
        }
    }

    private bool TryResolveProjectionSql(string key, out string sql)
    {
        if (_config.ProjectionBaseSql.TryGetValue(key, out sql!))
            return true;
        var metadataKey = key.StartsWith("global::", StringComparison.Ordinal) ? key.Substring(8) : key;
        return _config.ProjectionBaseSql.TryGetValue(metadataKey, out sql!);
    }

    private void ApplyPostLoad(IEnumerable<T> entities)
    {
        if (_postLoad is null) return;
        foreach (var e in entities)
            _postLoad(e);
    }

    private async Task ApplySplitIncludesAsync(
        List<T> list,
        QueryBuilderStateSnapshot state,
        IDbTransaction? transaction)
    {
        if (!state.SplitQuery || state.Includes.Count == 0 || _applySplitIncludes is null)
            return;
        await _applySplitIncludes(list, state.Includes, transaction).ConfigureAwait(false);
    }

    private static DynamicParameters ToDapperParams(Dictionary<string, object?> parameters)
    {
        var dp = new DynamicParameters();
        foreach (var kv in parameters)
            dp.Add(kv.Key, kv.Value);
        return dp;
    }

    private async Task ExecuteLockPreambleAsync(
        string? lockPreambleSql,
        DynamicParameters dapperParams,
        IDbTransaction? transaction)
    {
        if (string.IsNullOrEmpty(lockPreambleSql))
            return;
        await DbExecutor.ExecuteAsync(
            _connection, lockPreambleSql, dapperParams, transaction,
            logContext: LogContext("Query.LockPreamble")).ConfigureAwait(false);
    }

    private void MergeGlobalFilterParameters(DynamicParameters parameters)
    {
        if (_options is null || _config.GlobalFilterNames.Count == 0)
            return;

        foreach (var filterName in _config.GlobalFilterNames)
        {
            if (!_options.IsFilterActive(filterName))
                continue;
            var filterParams = _options.GetFilterParameters(filterName);
            if (filterParams is not null)
                parameters.AddDynamicParams(filterParams);
        }
    }
}
