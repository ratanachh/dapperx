namespace Dapper.Npa.Relations.Lazy;
using System.Data;
using System.Threading;
public sealed class LazyReference<T> where T : class
{
    private T? _cache;
    private bool _loaded;
    private readonly Func<IDbConnection, IDbTransaction?, Task<T?>>? _loader;
    private readonly SemaphoreSlim _loadGate = new(1, 1);

    public LazyReference() { }
    public LazyReference(Func<IDbConnection, IDbTransaction?, Task<T?>> loader)
        => _loader = loader;

    public bool IsLoaded => _loaded;

    public async Task<T?> GetAsync(IDbConnection connection, IDbTransaction? transaction = null)
    {
        if (_loaded) return _cache;

        await _loadGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_loaded) return _cache;
            if (_loader is null) { _loaded = true; return null; }
            _cache = await _loader(connection, transaction).ConfigureAwait(false);
            _loaded = true;
            return _cache;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    public T? TryGet() => _loaded ? _cache : null;

    public void Set(T? entity) { _cache = entity; _loaded = true; }
}
