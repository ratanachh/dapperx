namespace DapperX.Core.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class StoredProcedureAttribute(string name) : Attribute
{
    public string Name { get; } = name;
    /// <summary>Output-only parameter names; order matches <c>ProcResult</c> type arguments.</summary>
    public string[] OutParameters { get; init; } = [];
    /// <summary>Input/output parameter names; must match method parameter names.</summary>
    public string[] InOutParameters { get; init; } = [];
    /// <summary>Scalar return parameter name when the procedure exposes a return value.</summary>
    public string? ReturnParameter { get; init; }
    public Type[] ResultSets { get; init; } = [];
}
