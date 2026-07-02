using Dapper.Npa.Generator.Models;

namespace Dapper.Npa.Generator.Cpql;

using Generator.Models;
using Microsoft.CodeAnalysis;

internal enum CpqlValueKind
{
    Unknown,
    Null,
    Bool,
    String,
    Numeric,
    DateTime,
    Entity,
}

internal static class CpqlTypeHelper
{
    public static CpqlValueKind KindFromClrType(string clrTypeName)
    {
        if (string.IsNullOrEmpty(clrTypeName))
            return CpqlValueKind.Unknown;

        var t = clrTypeName.TrimEnd('?');
        if (t is "string" or "System.String")
            return CpqlValueKind.String;
        if (t is "bool" or "System.Boolean")
            return CpqlValueKind.Bool;
        if (t is "int" or "long" or "short" or "byte" or "decimal" or "double" or "float"
            or "System.Int32" or "System.Int64" or "System.Int16" or "System.Byte"
            or "System.Decimal" or "System.Double" or "System.Single")
            return CpqlValueKind.Numeric;
        if (t.Contains("DateTime", StringComparison.Ordinal))
            return CpqlValueKind.DateTime;
        return CpqlValueKind.Unknown;
    }

    public static bool IsNumeric(CpqlValueKind kind) => kind == CpqlValueKind.Numeric;
    public static bool IsStringCompatible(CpqlValueKind kind)
        => kind is CpqlValueKind.String or CpqlValueKind.Unknown;

    public static bool AreCompatible(CpqlValueKind a, CpqlValueKind b)
    {
        if (a == CpqlValueKind.Unknown || b == CpqlValueKind.Unknown || a == CpqlValueKind.Null || b == CpqlValueKind.Null)
            return true;
        if (a == b) return true;
        if (IsNumeric(a) && IsNumeric(b)) return true;
        return false;
    }

    public static CpqlValueKind InferValueKind(CpqlValueNode? node, CpqlSemanticContext ctx)
    {
        if (node is null) return CpqlValueKind.Null;
        return node switch
        {
            CpqlLiteralNode lit when lit.IsNull => CpqlValueKind.Null,
            CpqlLiteralNode lit when lit.IsTrue || lit.IsFalse => CpqlValueKind.Bool,
            CpqlLiteralNode lit when lit.Value is string => CpqlValueKind.String,
            CpqlLiteralNode lit when lit.Value is int or long or decimal or double or float => CpqlValueKind.Numeric,
            CpqlPropertyPathNode path => InferPropertyKind(path, ctx),
            CpqlParameterNode p => InferParameterKind(p, ctx),
            CpqlArithmeticNode => CpqlValueKind.Numeric,
            CpqlAggregateNode => CpqlValueKind.Numeric,
            CpqlScalarFunctionNode sf => InferScalarKind(sf),
            _ => CpqlValueKind.Unknown,
        };
    }

    private static CpqlValueKind InferPropertyKind(CpqlPropertyPathNode path, CpqlSemanticContext ctx)
    {
        if (path.Path.Count == 0)
            return CpqlValueKind.Entity;
        var entity = ctx.GetAliasEntity(path.Alias);
        if (entity is null) return CpqlValueKind.Unknown;
        for (var i = 0; i < path.Path.Count - 1; i++)
        {
            var rel = entity.Relationships.FirstOrDefault(r => r.PropertyName == path.Path[i]);
            if (rel is null) return CpqlValueKind.Unknown;
            entity = ctx.ResolveTargetEntity(rel);
            if (entity is null) return CpqlValueKind.Unknown;
        }
        var prop = entity.Properties.FirstOrDefault(p => p.PropertyName == path.Path[path.Path.Count - 1]);
        return prop is null ? CpqlValueKind.Unknown : KindFromClrType(prop.ClrTypeName);
    }

    private static CpqlValueKind InferParameterKind(CpqlParameterNode p, CpqlSemanticContext ctx)
    {
        var sym = ctx.Method.Parameters.FirstOrDefault(x =>
            string.Equals(x.Name, p.Name, StringComparison.OrdinalIgnoreCase));
        if (sym is null) return CpqlValueKind.Unknown;
        return KindFromClrType(sym.Type.ToDisplayString());
    }

    private static CpqlValueKind InferScalarKind(CpqlScalarFunctionNode sf)
    {
        var name = sf.Name.ToUpperInvariant();
        if (name is "YEAR" or "MONTH" or "DAY" or "HOUR" or "MINUTE" or "SECOND" or "DATEDIFF" or "LENGTH" or "LEN")
            return CpqlValueKind.Numeric;
        if (name is "LOWER" or "UPPER" or "TRIM" or "LTRIM" or "RTRIM" or "SUBSTRING" or "REPLACE"
            or "LEFT" or "RIGHT" or "CONCAT")
            return CpqlValueKind.String;
        if (name is "NOW" or "CURRENT_DATE" or "CURRENT_TIMESTAMP" or "DATEADD")
            return CpqlValueKind.DateTime;
        if (name is "ABS" or "CEILING" or "FLOOR" or "ROUND" or "MOD" or "POWER")
            return CpqlValueKind.Numeric;
        return CpqlValueKind.Unknown;
    }
}

internal sealed class CpqlSemanticContext
{
    public EntityModel RootEntity { get; init; } = null!;
    public IMethodSymbol Method { get; init; } = null!;
    public Compilation? Compilation { get; init; }
    public IReadOnlyDictionary<string, EntityModel> AllModels { get; init; } = null!;
    public Dictionary<string, EntityModel> Aliases { get; } = new(StringComparer.Ordinal);
    public HashSet<string> CteNames { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ReferencedCtes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, IReadOnlyList<string>> CteColumns { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, IReadOnlyList<string>> CteAliasColumns { get; } =
        new(StringComparer.Ordinal);

    public EntityModel? ResolveEntityByName(string name)
    {
        foreach (var model in AllModels.Values)
        {
            if (string.Equals(model.ClassName, name, StringComparison.Ordinal))
                return model;
        }
        return null;
    }

    public EntityModel? GetAliasEntity(string alias)
        => Aliases.TryGetValue(alias, out var e) ? e : null;

    public EntityModel? ResolveTargetEntity(RelationshipModel rel)
    {
        var fqn = rel.ChildEntityFqn ?? rel.TargetEntity;
        if (string.IsNullOrEmpty(fqn)) return null;
        var key = fqn.StartsWith("global::", StringComparison.Ordinal) ? fqn.Substring(8) : fqn;
        if (AllModels.TryGetValue(key, out var model)) return model;
        foreach (var m in AllModels.Values)
        {
            if (string.Equals(m.FullyQualifiedName, key, StringComparison.Ordinal)
                || string.Equals(m.ClassName, key, StringComparison.Ordinal))
                return m;
        }
        return null;
    }
}
