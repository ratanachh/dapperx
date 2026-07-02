namespace Dapper.Npa.Core.Attributes;

/// <summary>Generator produces only SELECT methods — no INSERT/UPDATE/DELETE.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ImmutableAttribute : Attribute { }
