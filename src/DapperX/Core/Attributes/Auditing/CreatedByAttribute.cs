namespace DapperX.Core.Attributes;
/// <summary>Set on insert to the current principal, supplied via <c>IAuditingProvider</c>.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)] public sealed class CreatedByAttribute : Attribute { }
