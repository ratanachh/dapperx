namespace DapperX.Core.Attributes;
/// <summary>Marks a method to be invoked immediately after an entity is updated.</summary>
[AttributeUsage(AttributeTargets.Method)] public sealed class PostUpdateAttribute : Attribute { }
