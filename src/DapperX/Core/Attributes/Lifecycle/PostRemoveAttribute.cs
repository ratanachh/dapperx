namespace DapperX.Core.Attributes;
/// <summary>Marks a method to be invoked immediately after an entity is deleted.</summary>
[AttributeUsage(AttributeTargets.Method)] public sealed class PostRemoveAttribute : Attribute { }
