namespace DapperX.Core.Attributes;

/// <summary>AllocationSize is intentionally omitted — block allocation requires cross-call state.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class SequenceGeneratorAttribute(string name, string sequenceName) : Attribute
{
    public string Name { get; } = name;
    public string SequenceName { get; } = sequenceName;
}
