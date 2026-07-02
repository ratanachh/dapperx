namespace Dapper.Npa.Core.Attributes;

/// <summary>Shared-PK OneToOne — child.Id = parent.Id, no separate FK column. Rule A: JOIN ON child.id = parent.id is compile-time literal.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class PrimaryKeyJoinColumnAttribute : Attribute { }
