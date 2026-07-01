namespace DapperX.Generator.Models;

internal enum StoredProcedureReturnKind
{
    Invalid,
    Void,
    EntityEnumerable,
    ProcResult1,
    ProcResult2,
    MultiResult2,
}

internal sealed class StoredProcedureModel
{
    public string ProcName { get; init; } = string.Empty;
    public IReadOnlyList<ProcParamModel> Parameters { get; init; } = [];
    public IReadOnlyList<string> ResultSetTypes { get; init; } = [];
    public IReadOnlyList<string> OutParameterNames { get; init; } = [];
    public string? ReturnParameterName { get; init; }
    public StoredProcedureReturnKind ReturnKind { get; init; }
    public IReadOnlyList<string> ProcResultTypeArguments { get; init; } = [];
    public bool HasOutputParameters =>
        OutParameterNames.Count > 0
        || Parameters.Any(p => p.Mode is "Out" or "InOut" or "Return")
        || ReturnParameterName is not null;
    public bool HasMultipleResultSets => ResultSetTypes.Count > 0;
}

internal sealed class ProcParamModel
{
    public string Name { get; init; } = string.Empty;
    public string Mode { get; init; } = "In"; // In, Out, InOut, Return
    public string? ClrTypeName { get; init; }
}
