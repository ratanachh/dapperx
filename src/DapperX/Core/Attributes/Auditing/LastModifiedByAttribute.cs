namespace DapperX.Core.Attributes;
/// <summary>Set on every insert and update to the current principal, supplied via <c>IAuditingProvider</c>.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)] public sealed class LastModifiedByAttribute : Attribute { }
