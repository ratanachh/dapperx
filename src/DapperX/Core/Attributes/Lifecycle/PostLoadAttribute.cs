namespace DapperX.Core.Attributes;
/// <summary>Marks a method to be invoked immediately after an entity is materialized from a query result.</summary>
[AttributeUsage(AttributeTargets.Method)] public sealed class PostLoadAttribute : Attribute { }
