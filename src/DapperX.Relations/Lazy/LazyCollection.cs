namespace DapperX.Relations.Lazy;
using System.Data;

public sealed class LazyCollection<T> where T : class
{
    private IReadOnlyList<T>? _cache;
    private readonly Func<IDbConnection, IDbTransaction?, Task<IEnumerable<T>>>? _loader;
    private readonly SemaphoreSlim _loadGate = new(1, 1);

    public LazyCollection() { }
    public LazyCollection(Func<IDbConnection, IDbTransaction?, Task<IEnumerable<T>>> loader)
        => _loader = loader;

    public bool IsLoaded => _cache is not null;

    public async Task<IReadOnlyList<T>> GetAsync(IDbConnection connection, IDbTransaction? transaction = null)
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
                _cache = [];
                return _cache;
            }

            _cache = (await _loader(connection, transaction).ConfigureAwait(false)).ToList();
            return _cache;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    public IReadOnlyList<T>? TryGet() => _cache;

    public void Set(IEnumerable<T> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _cache = data.ToList();
    }
}
