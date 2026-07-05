namespace DapperX.Core.Attributes;

/// <summary>Marks a class as a DapperX-mapped entity, making it eligible for compile-time repository generation. Combine with <see cref="TableAttribute"/> and an <see cref="IdAttribute"/>-annotated property.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EntityAttribute : Attribute { }
