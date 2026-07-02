namespace Dapper.Npa.Relations.Lazy;
using System.Data;

public sealed class LazyMap<TKey, TValue> where TValue : class where TKey : notnull
{
    private IReadOnlyDictionary<TKey, TValue>? _cache;
    private readonly Func<IDbConnection, IDbTransaction?, Task<IEnumerable<TValue>>>? _loader;
    private readonly Func<TValue, TKey> _keySelector;
    private readonly SemaphoreSlim _loadGate = new(1, 1);

    public LazyMap(Func<TValue, TKey> keySelector,
                   Func<IDbConnection, IDbTransaction?, Task<IEnumerable<TValue>>>? loader = null)
    {
        _keySelector = keySelector;
        _loader = loader;
    }

    public bool IsLoaded => _cache is not null;

    /// <summary>Loads and groups by key column — in-memory LINQ, no dynamic SQL.</summary>
    public async Task<IReadOnlyDictionary<TKey, TValue>> GetAsync(IDbConnection connection, IDbTransaction? transaction = null)
    {
        if (_cache is not null)
            return _cache;

        await _loadGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_cache is not null)
                return _cache;

            if (_loader is null)
            {
                _cache = new Dictionary<TKey, TValue>();
                return _cache;
            }

            var results = await _loader(connection, transaction).ConfigureAwait(false);
            _cache = results.ToDictionary(_keySelector);
            return _cache;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    public IReadOnlyDictionary<TKey, TValue>? TryGet() => _cache;

    public void Set(IDictionary<TKey, TValue> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _cache = new Dictionary<TKey, TValue>(data);
    }
}
