namespace DapperX.Core.Attributes;
/// <summary>Set on every insert and update to the current timestamp.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)] public sealed class LastModifiedDateAttribute : Attribute { }
