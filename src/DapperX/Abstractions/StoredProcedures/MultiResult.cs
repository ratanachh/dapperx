namespace DapperX.Abstractions.StoredProcedures;

public sealed class MultiResult<T1, T2>(IEnumerable<T1> first, IEnumerable<T2> second)
{
    public IReadOnlyList<T1> First { get; } = first.ToList();
    public IReadOnlyList<T2> Second { get; } = second.ToList();
}
