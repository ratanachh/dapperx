using Dapper.Npa.Generator.Builders;
using Dapper.Npa.Generator.Models;

namespace Dapper.Npa.Generator.Generators;

using Generator.Builders;
using Generator.Models;

internal static class ExecutionPlanGenerator
{
    public static ExecutionPlanModel? BuildInsertPlan(
        EntityModel entity,
        IReadOnlyDictionary<string, EntityModel> allModels)
    {
        var graphRels = GraphBuilder.GetGraphRelationships(entity, allModels, GraphCascadeOperation.Insert);
        var manyToMany = GraphBuilder.GetManyToManyGraphRelationships(entity, GraphCascadeOperation.Insert);

        if (graphRels.Count == 0 && manyToMany.Count == 0)
            return null;

        var nodes = new List<ExecutionNodeModel>
        {
            new()
            {
                EntityTypeName = entity.FullyQualifiedName,
                Operation = "Insert",
                Level = 0,
            },
        };

        foreach (var rel in graphRels)
        {
            nodes.Add(new ExecutionNodeModel
            {
                EntityTypeName = rel.ChildFqn,
                Operation = "Insert",
                Level = 1,
                ForeignKeyProperty = rel.FkPropertyName,
                ParentEntityTypeName = entity.FullyQualifiedName,
                RelationshipProperty = rel.Relationship.PropertyName,
            });
        }

        foreach (var rel in manyToMany)
        {
            nodes.Add(new ExecutionNodeModel
            {
                EntityTypeName = entity.FullyQualifiedName,
                Operation = "InsertJoinTable",
                Level = graphRels.Count > 0 ? 2 : 1,
                ParentEntityTypeName = entity.FullyQualifiedName,
                RelationshipProperty = rel.PropertyName,
                JoinTable = rel.JoinTable,
            });
        }

        return new ExecutionPlanModel
        {
            PlanName = "InsertGraphExecutionPlan",
            Nodes = nodes,
        };
    }

    public static ExecutionPlanModel? BuildDeletePlan(
        EntityModel entity,
        IReadOnlyDictionary<string, EntityModel> allModels)
    {
        var graphRels = GraphBuilder.GetGraphRelationships(entity, allModels, GraphCascadeOperation.Delete);
        var manyToMany = GraphBuilder.GetManyToManyGraphRelationships(entity, GraphCascadeOperation.Delete);

        if (graphRels.Count == 0 && manyToMany.Count == 0)
            return null;

        var nodes = new List<ExecutionNodeModel>();

        foreach (var rel in graphRels.AsEnumerable().Reverse())
        {
            nodes.Add(new ExecutionNodeModel
            {
                EntityTypeName = rel.ChildFqn,
                Operation = "Delete",
                Level = 0,
                ForeignKeyProperty = rel.FkPropertyName,
                ParentEntityTypeName = entity.FullyQualifiedName,
                RelationshipProperty = rel.Relationship.PropertyName,
            });
        }

        foreach (var rel in manyToMany)
        {
            nodes.Add(new ExecutionNodeModel
            {
                EntityTypeName = entity.FullyQualifiedName,
                Operation = "DeleteJoinTable",
                Level = 0,
                ParentEntityTypeName = entity.FullyQualifiedName,
                RelationshipProperty = rel.PropertyName,
                JoinTable = rel.JoinTable,
            });
        }

        nodes.Add(new ExecutionNodeModel
        {
            EntityTypeName = entity.FullyQualifiedName,
            Operation = "Delete",
            Level = 1,
        });

        return new ExecutionPlanModel
        {
            PlanName = "DeleteGraphExecutionPlan",
            Nodes = nodes,
        };
    }
}
