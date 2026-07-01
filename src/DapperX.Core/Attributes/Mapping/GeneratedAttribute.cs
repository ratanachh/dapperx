namespace DapperX.Core.Attributes;

using DapperX.Core.Enums;

/// <summary>Column value set by the DB on INSERT and/or UPDATE; excluded from generated SQL and re-fetched after mutation.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class GeneratedAttribute(GenerationTime generationTime) : Attribute
{
    public GenerationTime GenerationTime { get; } = generationTime;
}
