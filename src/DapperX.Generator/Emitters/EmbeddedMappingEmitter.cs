namespace DapperX.Generator.Emitters;

using System.Linq;
using System.Text;
using DapperX.Generator.Models;
using Microsoft.CodeAnalysis;

internal static class EmbeddedMappingEmitter
{
    public static string DbRowTypeName(EntityModel entity) => $"{entity.ClassName}DbRow";

    public static IReadOnlyList<PropertyModel> GetDbRowProperties(EntityModel entity)
        => entity.Properties.Where(p => !p.IsTransient).ToList();

    public static void EmitDbRowAndMapping(StringBuilder sb, EntityModel entity, Compilation compilation)
    {
        if (!entity.RequiresDbRow)
            return;

        var rowType = DbRowTypeName(entity);
        var entityFqn = entity.FullyQualifiedName;

        sb.AppendLine($"    private sealed class {rowType}");
        sb.AppendLine("    {");
        foreach (var p in GetDbRowProperties(entity))
        {
            var clr = ConverterEmitter.GetDbRowClrType(p, compilation);
            var useNullable = !p.IsId && (p.Nullable || IsReferenceType(clr));
            var nullableSuffix = useNullable && !clr.EndsWith("?", StringComparison.Ordinal) ? "?" : "";
            sb.AppendLine($"        public {clr}{nullableSuffix} {p.PropertyName} {{ get; set; }}");
        }
        sb.AppendLine("    }");
        sb.AppendLine();

        ConverterEmitter.EmitConverterFields(sb, entity);

        EmitMapFromDbRow(sb, entity, entityFqn, rowType);
        EmitBuildMutationParameters(sb, entity, entityFqn, compilation);
        sb.AppendLine();
    }

    private static void EmitMapFromDbRow(StringBuilder sb, EntityModel entity, string entityFqn, string rowType)
    {
        sb.AppendLine($"    private static {entityFqn} MapFromDbRow({rowType} row)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var entity = new {entityFqn}();");

        foreach (var p in entity.Properties.Where(p => !p.IsTransient && !p.IsEmbeddedColumn && p.ConverterTypeName is null))
            sb.AppendLine($"        entity.{p.PropertyName} = row.{p.PropertyName};");

        foreach (var site in entity.EmbeddedSites)
        {
            var embedFqn = site.EmbeddableTypeFqn;
            var cols = entity.Properties
                .Where(p => p.IsEmbeddedColumn && p.EmbeddedOwner == site.PropertyName)
                .ToList();

            if (cols.Count == 0)
                continue;

            var nullChecks = string.Join(" && ", cols.Select(c => $"row.{c.PropertyName} is null"));
            sb.AppendLine($"        if ({nullChecks})");
            sb.AppendLine($"            entity.{site.PropertyName} = null;");
            sb.AppendLine("        else");
            sb.AppendLine("        {");
            sb.AppendLine($"            entity.{site.PropertyName} = new {embedFqn}();");
            foreach (var c in cols.Where(c => c.ConverterTypeName is null))
                sb.AppendLine($"            entity.{site.PropertyName}.{c.EmbeddedInner} = row.{c.PropertyName};");
            sb.AppendLine("        }");
        }

        foreach (var p in entity.Properties.Where(p => !p.IsTransient && !p.IsEmbeddedColumn && p.ConverterTypeName is not null))
            sb.AppendLine($"        entity.{p.PropertyName} = default!;");

        ConverterEmitter.EmitApplyConvertersRead(sb, entity, "entity", "row");
        sb.AppendLine("        return entity;");
        sb.AppendLine("    }");
    }

    private static bool IsReferenceType(string clrTypeName)
    {
        if (clrTypeName.EndsWith("?", StringComparison.Ordinal))
            return false;
        return clrTypeName is "string" or "System.String"
            || !clrTypeName.StartsWith("System.", StringComparison.Ordinal)
                && clrTypeName is not "int" and not "long" and not "bool" and not "double" and not "float"
                and not "decimal" and not "byte" and not "short" and not "System.Int32" and not "System.Int64"
                and not "System.Boolean" and not "System.Double" and not "System.Single" and not "System.Decimal"
                and not "System.Byte" and not "System.Int16" and not "System.Guid" and not "System.DateTime"
                and not "System.DateTimeOffset";
    }

    private static void EmitBuildMutationParameters(StringBuilder sb, EntityModel entity, string entityFqn, Compilation compilation)
    {
        var mutating = entity.Properties
            .Where(p => !p.IsTransient && (p.Insertable || p.Updatable) && p.Formula is null
                        && p.GeneratedTime is null && p.SecondaryTable is null
                        && (p.ColumnTransformer is null || p.ColumnTransformer.Write is not null))
            .ToList();

        if (mutating.Count == 0)
            return;

        sb.AppendLine($"    private static object BuildMutationParameters({entityFqn} entity)");
        sb.AppendLine("    {");
        sb.AppendLine("        return new");
        sb.AppendLine("        {");
        for (var i = 0; i < mutating.Count; i++)
        {
            var p = mutating[i];
            var comma = i < mutating.Count - 1;
            ConverterEmitter.EmitMutationParameterEntry(sb, p, entity, "entity", compilation, comma);
        }
        sb.AppendLine("        };");
        sb.AppendLine("    }");
    }
}
