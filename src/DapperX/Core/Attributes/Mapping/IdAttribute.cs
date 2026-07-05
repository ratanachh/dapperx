namespace DapperX.Core.Attributes;

/// <summary>Marks a property as the entity's primary key. Combine with <see cref="GeneratedValueAttribute"/> for database- or generator-assigned identifiers.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class IdAttribute : Attribute { }
