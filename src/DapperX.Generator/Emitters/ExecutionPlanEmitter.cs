using DapperX.Generator.Models;

namespace DapperX.Generator.Emitters;

using System.Text;
using Generator.Models;

internal static class ExecutionPlanEmitter
{
    public static void Emit(StringBuilder sb, ExecutionPlanModel? insertPlan, ExecutionPlanModel? deletePlan)
    {
        if (insertPlan is not null)
            EmitPlan(sb, insertPlan);

        if (deletePlan is not null)
            EmitPlan(sb, deletePlan);
    }

    private static void EmitPlan(StringBuilder sb, ExecutionPlanModel plan)
    {
        sb.AppendLine($"    private static readonly global::DapperX.Batching.Execution.ExecutionPlan {plan.PlanName} = new()");
        sb.AppendLine("    {");
        sb.AppendLine("        Nodes = new global::DapperX.Batching.Execution.ExecutionNode[]");
        sb.AppendLine("        {");

        foreach (var node in plan.Nodes)
        {
            sb.Append("            new global::DapperX.Batching.Execution.ExecutionNode { ");
            sb.Append($"EntityTypeName = \"{Esc(node.EntityTypeName)}\", ");
            sb.Append($"Operation = \"{Esc(node.Operation)}\", ");
            sb.Append($"Level = {node.Level}");
            if (node.ForeignKeyProperty is not null)
                sb.Append($", ForeignKeyProperty = \"{Esc(node.ForeignKeyProperty)}\"");
            if (node.ParentEntityTypeName is not null)
                sb.Append($", ParentEntityTypeName = \"{Esc(node.ParentEntityTypeName)}\"");
            sb.AppendLine(" },");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    };");
        sb.AppendLine();
    }

    private static string Esc(string s)
        => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
