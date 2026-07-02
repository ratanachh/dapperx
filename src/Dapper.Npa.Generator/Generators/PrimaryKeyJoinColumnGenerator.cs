using Dapper.Npa.Generator.Models;

namespace Dapper.Npa.Generator.Generators;

using System.Text;
using Generator.Models;

/// <summary>Assigns child shared-PK from parent Id before or after parent INSERT.</summary>
internal static class PrimaryKeyJoinColumnGenerator
{
    public static void EmitAssignBeforeInsert(StringBuilder sb, EntityModel entity)
        => EmitAssignCore(sb, entity);

    public static void EmitAssignAfterParentId(StringBuilder sb, EntityModel entity)
        => EmitAssignCore(sb, entity);

    private static void EmitAssignCore(StringBuilder sb, EntityModel entity)
    {
        var pkJoinRels = entity.Relationships.Where(r => r.IsPrimaryKeyJoin).ToList();
        if (pkJoinRels.Count == 0)
            return;

        var idProp = entity.Properties.First(p => p.IsId);
        foreach (var rel in pkJoinRels)
        {
            sb.AppendLine($"        if (entity.{rel.PropertyName} is not null)");
            sb.AppendLine($"            entity.{rel.PropertyName}.{idProp.PropertyName} = entity.{idProp.PropertyName};");
        }
    }
}
