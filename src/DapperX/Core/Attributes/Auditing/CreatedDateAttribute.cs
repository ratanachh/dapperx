namespace DapperX.Core.Attributes;
/// <summary>Set on insert to the current timestamp.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)] public sealed class CreatedDateAttribute : Attribute { }
