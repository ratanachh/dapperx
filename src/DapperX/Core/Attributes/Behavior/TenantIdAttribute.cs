namespace DapperX.Core.Attributes;
/// <summary>Marks a property as the multi-tenancy discriminator column; generated queries automatically scope to the current tenant, supplied via <c>ITenantProvider</c>.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)] public sealed class TenantIdAttribute : Attribute { }
