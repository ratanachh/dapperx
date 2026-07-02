using Dapper.Npa.Generator.Models;

namespace Dapper.Npa.Generator.Generators;

using System.Text;
using Generator.Models;
using Generator.Utils;

/// <summary>Emits child repository construction for graph operations with optional parent DI passthrough.</summary>
internal static class GraphChildRepositoryEmitter
{
    public static string EmitNewExpression(EntityModel parentEntity, EntityModel childEntity, string provider = "SqlServer")
    {
        var implFqn = ResolveImplFqn(childEntity);
        var args = new List<string> { "_connection" };

        if (!childEntity.IsImmutable)
            args.Add("_options");

        if (childEntity.Auditing is not null && parentEntity.Auditing is not null)
            args.Add("_auditingProvider");

        if (childEntity.TenantIdColumn is not null && parentEntity.TenantIdColumn is not null)
            args.Add("_tenantProvider");

        if (childEntity.Sequence is not null && parentEntity.Sequence is not null)
            args.Add("_sequenceAllocator");

        return $"new global::{implFqn}({string.Join(", ", args)})";
    }

    private static string ResolveImplFqn(EntityModel child)
    {
        var impl = child.ClassName + "RepositoryImpl";
        var ns = string.IsNullOrEmpty(child.Namespace)
            ? "Generated"
            : $"{child.Namespace}.Generated";
        return $"{ns}.{impl}";
    }
}
