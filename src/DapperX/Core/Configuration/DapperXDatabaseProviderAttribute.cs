using DapperX.Core.Enums;

namespace DapperX.Core.Configuration;

using Core.Enums;

/// <summary>
/// Declares the compile-time database provider for DapperX source generation (Requirements §29 / EPIC 23).
/// Overrides default SqlServer when no MSBuild <c>DapperXDatabaseProvider</c> property is set.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class DapperXDatabaseProviderAttribute(DatabaseProvider provider) : Attribute
{
    public DatabaseProvider Provider { get; } = provider;
}
