using DapperX.Generator.Models;
using DapperX.Generator.Utils;

namespace DapperX.Generator.Builders;

using Microsoft.CodeAnalysis;
using Generator.Models;
using Generator.Utils;

internal static class RelationshipMetadataEnricher
{
    public static void Enrich(
        IReadOnlyDictionary<string, EntityModel> models,
        Compilation compilation,
        SourceProductionContext ctx)
    {
        foreach (var model in models.Values)
        {
            foreach (var rel in model.Relationships)
            {
                if (rel.Kind == "OneToMany")
                    EnrichOneToMany(model, rel, models, compilation, ctx);
                else if (rel.Kind == "ManyToMany" && rel.IsLazyCollection)
                    EnrichManyToMany(model, rel, models, compilation, ctx);
            }
        }
    }

    private static void EnrichManyToMany(
        EntityModel parent,
        RelationshipModel rel,
        IReadOnlyDictionary<string, EntityModel> models,
        Compilation compilation,
        SourceProductionContext ctx)
    {
        if (string.IsNullOrEmpty(rel.JoinTable) || string.IsNullOrEmpty(rel.TargetEntity))
            return;

        var targetFqn = NormalizeFqn(rel.TargetEntity!);
        if (!models.TryGetValue(targetFqn, out var targetModel))
        {
            var sym = compilation.GetTypeByMetadataName(ToMetadataName(rel.TargetEntity!));
            if (sym is null) return;
            targetModel = MetadataBuilder.Build(sym, ctx);
            if (targetModel is null) return;
        }

        rel.ChildEntityFqn = targetModel.FullyQualifiedName;
        rel.ChildTableName = targetModel.TableName;
        rel.ChildSchema = targetModel.Schema;
        rel.ChildHasPostLoad = targetModel.HasPostLoad;
        rel.IsBatchLoadable = true;
    }

    private static void EnrichOneToMany(
        EntityModel parent,
        RelationshipModel rel,
        IReadOnlyDictionary<string, EntityModel> models,
        Compilation compilation,
        SourceProductionContext ctx)
    {
        if (string.IsNullOrEmpty(rel.TargetEntity))
        {
            ReportInvalidCollection(parent, rel, ctx);
            return;
        }

        if (!rel.IsLazyCollection && !rel.IsLazyMap)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.BatchLoadInvalidCollectionType,
                Location.None,
                rel.PropertyName,
                parent.ClassName));
            return;
        }

        var childFqn = rel.TargetEntity!;
        var normalizedChild = NormalizeFqn(childFqn);
        if (!models.TryGetValue(normalizedChild, out var childModel))
        {
            var childSymbol = compilation.GetTypeByMetadataName(ToMetadataName(childFqn));
            if (childSymbol is null)
                return;
            childModel = MetadataBuilder.Build(childSymbol, ctx);
            if (childModel is null)
                return;
        }

        rel.ChildEntityFqn = childModel.FullyQualifiedName;
        rel.ChildTableName = childModel.TableName;
        rel.ChildSchema = childModel.Schema;
        rel.ChildHasPostLoad = childModel.HasPostLoad;

        ResolveForeignKey(parent, rel, childModel, compilation, ctx);

        if (rel.IsLazyMap && !string.IsNullOrEmpty(rel.MapKeyColumn))
            rel.MapKeyPropertyName = FindPropertyByColumn(childModel, rel.MapKeyColumn!);

        rel.IsBatchLoadable = !string.IsNullOrEmpty(rel.ForeignKeyColumn)
                              && !string.IsNullOrEmpty(rel.FkPropertyNameOnChild);
    }

    private static void ResolveForeignKey(
        EntityModel parent,
        RelationshipModel rel,
        EntityModel child,
        Compilation compilation,
        SourceProductionContext ctx)
    {
        var childSymbol = compilation.GetTypeByMetadataName(ToMetadataName(child.FullyQualifiedName));
        if (childSymbol is null)
            return;

        string? fkColumn = null;

        if (!string.IsNullOrEmpty(rel.MappedBy))
        {
            var backRef = childSymbol.GetMembers(rel.MappedBy!)
                .OfType<IPropertySymbol>()
                .FirstOrDefault();
            if (backRef is not null && SyntaxHelper.HasAttribute(backRef, SyntaxHelper.ManyToOneAttr))
            {
                var jc = SyntaxHelper.GetAttribute(backRef, SyntaxHelper.JoinColumnAttr);
                fkColumn = SyntaxHelper.GetConstructorArg<string>(jc, 0)
                           ?? SyntaxHelper.ToSnakeCase(parent.ClassName) + "_id";
            }
        }

        fkColumn ??= rel.ForeignKeyColumn;

        if (string.IsNullOrEmpty(fkColumn))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.BatchLoadUnresolvedFk,
                Location.None,
                rel.PropertyName,
                parent.ClassName));
            return;
        }

        rel.ForeignKeyColumn = fkColumn;
        rel.FkPropertyNameOnChild = FindPropertyByColumn(child, fkColumn!)
            ?? FindFkPropertyByConvention(child, parent.ClassName);

        if (rel.FkPropertyNameOnChild is null)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.BatchLoadUnresolvedFk,
                Location.None,
                rel.PropertyName,
                parent.ClassName));
        }
    }

    private static string? FindPropertyByColumn(EntityModel entity, string columnName)
        => entity.Properties.FirstOrDefault(p =>
            string.Equals(p.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))?.PropertyName;

    private static string? FindFkPropertyByConvention(EntityModel child, string parentClassName)
    {
        var candidate = parentClassName + "Id";
        return child.Properties.Any(p => p.PropertyName == candidate) ? candidate : null;
    }

    private static void ReportInvalidCollection(EntityModel parent, RelationshipModel rel, SourceProductionContext ctx)
        => ctx.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.BatchLoadInvalidCollectionType,
            Location.None,
            rel.PropertyName,
            parent.ClassName));

    private static string NormalizeFqn(string fqn)
        => fqn.StartsWith("global::", StringComparison.Ordinal) ? fqn.Substring("global::".Length) : fqn;

    private static string ToMetadataName(string fqn) => NormalizeFqn(fqn);
}
