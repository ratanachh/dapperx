using DapperX.Generator.Models;

namespace DapperX.Generator.Generators;

using System.Text;
using Generator.Models;

/// <summary>When an entity has secondary tables, multi-statement mutating paths run in one transaction.</summary>
internal static class SecondaryTableTransactionEmitter
{
    public static void EmitScopeStart(StringBuilder sb, EntityModel entity)
    {
        if (!entity.SecondaryTables.Any())
            return;

        sb.AppendLine("        var ownsTransaction = transaction is null;");
        sb.AppendLine("        transaction ??= _connection.BeginTransaction();");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
    }

    public static void EmitScopeEnd(StringBuilder sb, EntityModel entity)
    {
        if (!entity.SecondaryTables.Any())
            return;

        sb.AppendLine("            if (ownsTransaction) transaction.Commit();");
        sb.AppendLine("        }");
        sb.AppendLine("        catch");
        sb.AppendLine("        {");
        sb.AppendLine("            if (ownsTransaction) transaction.Rollback();");
        sb.AppendLine("            throw;");
        sb.AppendLine("        }");
    }
}
