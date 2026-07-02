using Dapper.Npa.Generator.Builders;
using Dapper.Npa.Generator.Models;

namespace Dapper.Npa.Generator.Generators;

using System.Linq;
using System.Text;
using Generator.Builders;
using Generator.Models;

/// <summary>Emits INSERT fetch row type and apply helpers for <c>[Generated]</c> columns.</summary>
internal static class GeneratedColumnEmitter
{
    public static void EmitInsertFetchRowType(StringBuilder sb, EntityModel entity, string idType)
    {
        var idProp = entity.Properties.First(p => p.IsId);
        var generated = GeneratedColumnSqlBuilder.GetGeneratedProperties(entity);
        var useIdentity = idProp.IdGenerationStrategy == "Identity";

        sb.AppendLine("    private sealed class GeneratedInsertFetchRow");
        sb.AppendLine("    {");
        if (useIdentity)
            sb.AppendLine($"        public {idType} {idProp.PropertyName} {{ get; set; }}");
        foreach (var p in generated)
            sb.AppendLine($"        public {p.ClrTypeName} {p.PropertyName} {{ get; set; }}");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    public static void EmitApplyInsertFetchHelper(StringBuilder sb, EntityModel entity, string entityFqn, string idType)
    {
        var idProp = entity.Properties.First(p => p.IsId);
        var generated = GeneratedColumnSqlBuilder.GetGeneratedProperties(entity);
        var useIdentity = idProp.IdGenerationStrategy == "Identity";

        sb.AppendLine($"    private static void ApplyGeneratedInsertFetch({entityFqn} entity, GeneratedInsertFetchRow row)");
        sb.AppendLine("    {");
        if (useIdentity)
            sb.AppendLine($"        entity.{idProp.PropertyName} = row.{idProp.PropertyName};");
        foreach (var p in generated)
        {
            if (p.ConverterTypeName is not null)
                sb.AppendLine($"        entity.{p.PropertyName} = _conv_{p.PropertyName}.ToProperty(row.{p.PropertyName});");
            else
                sb.AppendLine($"        entity.{p.PropertyName} = row.{p.PropertyName};");
        }
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    public static bool AnyGeneratedUsesConverter(EntityModel entity)
        => GeneratedColumnSqlBuilder.GetGeneratedProperties(entity).Any(p => p.ConverterTypeName is not null);
}
