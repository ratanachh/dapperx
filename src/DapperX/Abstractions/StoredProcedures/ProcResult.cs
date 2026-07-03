namespace DapperX.Abstractions.StoredProcedures;

public sealed class ProcResult<T1>(T1? value, IReadOnlyDictionary<string, object?> outputParameters)
{
    public T1? Value { get; } = value;
    public IReadOnlyDictionary<string, object?> OutputParameters { get; } = outputParameters;
}

public sealed class ProcResult<T1, T2>(T1? value1, T2? value2, IReadOnlyDictionary<string, object?> outputParameters)
{
    public T1? Value1 { get; } = value1;
    public T2? Value2 { get; } = value2;
    public IReadOnlyDictionary<string, object?> OutputParameters { get; } = outputParameters;
}
