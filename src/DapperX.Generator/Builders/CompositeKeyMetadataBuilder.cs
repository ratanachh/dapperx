namespace DapperX.Generator.Builders;

using Microsoft.CodeAnalysis;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;

internal static class CompositeKeyMetadataBuilder
{
    public static CompositeKeyModel? Build(
        INamedTypeSymbol entity,
        IReadOnlyList<PropertyModel> properties,
        SourceProductionContext ctx)
    {
        var hasEmbeddedId = SyntaxHelper.HasAttribute(entity, SyntaxHelper.EmbeddedIdAttr);
        var idClassAttr = SyntaxHelper.GetAttribute(entity, SyntaxHelper.IdClassAttr);
        var idProps = properties.Where(p => p.IsId).ToList();

        if (hasEmbeddedId)
            return BuildEmbeddedId(entity, ctx);

        if (idClassAttr is not null || idProps.Count > 1)
            return BuildIdClass(entity, idClassAttr, idProps, ctx);

        return null;
    }

    private static CompositeKeyModel? BuildIdClass(
        INamedTypeSymbol entity,
        AttributeData? idClassAttr,
        IReadOnlyList<PropertyModel> idProps,
        SourceProductionContext ctx)
    {
        if (idProps.Count < 2)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.CompositeKeyRequiresMultipleIds,
                entity.Locations.FirstOrDefault(),
                entity.Name));
            return null;
        }

        INamedTypeSymbol? keyType = null;
        if (idClassAttr?.ConstructorArguments.FirstOrDefault().Value is INamedTypeSymbol keyTypeSymbol)
            keyType = keyTypeSymbol;

        var keyTypeName = keyType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            ?? InferKeyTypeName(idProps);

        var keyClassProps = keyType is not null
            ? keyType.GetMembers().OfType<IPropertySymbol>()
                .Where(p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public)
                .Where(p => !SyntaxHelper.HasAttribute(p, SyntaxHelper.TransientAttr))
                .Select(p => p.Name)
                .ToList()
            : idProps.Select(p => p.PropertyName).ToList();

        var entityIdNames = idProps.Select(p => p.PropertyName).ToList();
        if (keyType is not null)
        {
            foreach (var keyName in keyClassProps)
            {
                if (!entityIdNames.Contains(keyName))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.CompositeKeyIdClassMismatch,
                        entity.Locations.FirstOrDefault(),
                        keyName,
                        keyType.Name,
                        entity.Name));
                }
            }

            foreach (var entityId in entityIdNames)
            {
                if (!keyClassProps.Contains(entityId))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.CompositeKeyIdClassMismatch,
                        entity.Locations.FirstOrDefault(),
                        entityId,
                        keyType.Name,
                        entity.Name));
                }
            }
        }

        var parts = idProps
            .OrderBy(p => keyClassProps.IndexOf(p.PropertyName))
            .Select(p => new CompositeKeyPartModel
            {
                KeyClassPropertyName = p.PropertyName,
                EntityPropertyName = p.PropertyName,
                ColumnName = p.ColumnName,
                ClrTypeName = p.ClrTypeName,
                IdGenerationStrategy = p.IdGenerationStrategy,
            })
            .ToList();

        return new CompositeKeyModel
        {
            KeyTypeName = keyTypeName,
            IsEmbeddedId = false,
            Parts = parts,
        };
    }

    private static CompositeKeyModel? BuildEmbeddedId(INamedTypeSymbol entity, SourceProductionContext ctx)
    {
        var embeddedIdProp = entity.GetMembers().OfType<IPropertySymbol>()
            .FirstOrDefault(p => SyntaxHelper.HasAttribute(p, SyntaxHelper.EmbeddedIdAttr));
        if (embeddedIdProp?.Type is not INamedTypeSymbol embedType)
            return null;

        var parts = new List<CompositeKeyPartModel>();
        foreach (var inner in embedType.GetMembers().OfType<IPropertySymbol>())
        {
            if (inner.IsStatic || inner.DeclaredAccessibility != Accessibility.Public)
                continue;
            if (SyntaxHelper.HasAttribute(inner, SyntaxHelper.TransientAttr))
                continue;

            var colAttr = SyntaxHelper.GetAttribute(inner, SyntaxHelper.ColumnAttr);
            var columnName = SyntaxHelper.GetConstructorArg<string>(colAttr, 0)
                ?? SyntaxHelper.ToSnakeCase(inner.Name);

            string? genStrategy = null;
            if (SyntaxHelper.HasAttribute(inner, SyntaxHelper.IdAttr))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.CompositeKeyEmbeddedIdInnerId,
                    inner.Locations.FirstOrDefault(),
                    inner.Name,
                    embedType.Name));
            }

            var genAttr = SyntaxHelper.GetAttribute(inner, SyntaxHelper.GeneratedValueAttr);
            if (genAttr is not null)
            {
                var strategyVal = genAttr.ConstructorArguments.FirstOrDefault().Value;
                genStrategy = strategyVal switch
                {
                    0 => "Identity",
                    1 => "Sequence",
                    2 => "Uuid",
                    3 => "Assigned",
                    _ => "Identity",
                };
            }

            parts.Add(new CompositeKeyPartModel
            {
                KeyClassPropertyName = inner.Name,
                EntityPropertyName = embeddedIdProp.Name,
                EmbeddedInnerProperty = inner.Name,
                ColumnName = columnName,
                ClrTypeName = inner.Type.ToDisplayString(),
                IdGenerationStrategy = genStrategy,
            });
        }

        if (parts.Count < 2)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.CompositeKeyRequiresMultipleIds,
                embeddedIdProp.Locations.FirstOrDefault(),
                entity.Name));
            return null;
        }

        return new CompositeKeyModel
        {
            KeyTypeName = embedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            IsEmbeddedId = true,
            EmbeddedIdPropertyName = embeddedIdProp.Name,
            Parts = parts,
        };
    }

    private static string InferKeyTypeName(IReadOnlyList<PropertyModel> idProps)
        => "global::System.Object";
}
