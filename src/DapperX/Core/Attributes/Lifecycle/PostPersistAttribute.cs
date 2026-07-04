namespace DapperX.Core.Attributes;
/// <summary>Marks a method to be invoked immediately after an entity is inserted.</summary>
[AttributeUsage(AttributeTargets.Method)] public sealed class PostPersistAttribute : Attribute { }
