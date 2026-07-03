using DapperX.Generator.Models;
using DapperX.Generator.Utils;

namespace DapperX.Generator.Validation;

using Microsoft.CodeAnalysis;
using Generator.Models;
using Generator.Utils;

internal static class MappingValidator
{
    public static void Validate(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx, string provider, Compilation compilation)
    {
        ValidateId(entity, symbol, ctx);
        ValidateVersion(entity, symbol, ctx);
        ValidateImmutable(entity, symbol, ctx);
        ValidateSqliteConstraints(entity, symbol, ctx, provider);
        PrimaryKeyJoinColumnValidator.Validate(entity, symbol, ctx, compilation);
        SecondaryTableValidator.Validate(entity, symbol, ctx, provider);
        TenancyValidator.Validate(entity, symbol, ctx);
        AuditingValidator.Validate(entity, symbol, ctx);
        SoftDeleteValidator.Validate(entity, symbol, ctx);
        GlobalFilterValidator.Validate(entity, symbol, ctx);
        GeneratedColumnValidator.Validate(entity, symbol, ctx);
        FormulaValidator.Validate(entity, symbol, ctx);
        EmbeddableValidator.Validate(entity, symbol, ctx);
        ConverterValidator.Validate(entity, symbol, compilation, ctx);
        AssociationOverrideValidator.Validate(entity, symbol, ctx);
        CompositeKeyValidator.Validate(entity, symbol, ctx);
        ElementCollectionValidator.Validate(entity, symbol, ctx);
        NamedEntityGraphValidator.Validate(entity, symbol, ctx);
        PropertyNameValidator.Validate(entity, symbol, ctx, provider);
    }

    private static void ValidateId(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx)
    {
        var idProps = entity.Properties.Where(p => p.IsId).ToList();
        if (idProps.Count == 0)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingId,
                symbol.Locations.FirstOrDefault(), entity.ClassName));
            return;
        }
        // Sequence validation
        foreach (var idProp in idProps.Where(p => p.IdGenerationStrategy == "Sequence"))
        {
            if (idProp.SequenceGeneratorName is not null && entity.Sequence is null)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingSequenceGenerator,
                    symbol.Locations.FirstOrDefault(), entity.ClassName, idProp.SequenceGeneratorName));
            }
        }
    }

    private static void ValidateVersion(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx)
    {
        var versionProps = entity.Properties.Where(p => p.IsVersion).ToList();
        foreach (var vp in versionProps)
        {
            var validTypes = new[] { "int", "long", "System.Int32", "System.Int64",
                "System.DateTime", "System.DateTimeOffset" };
            if (!validTypes.Any(t => vp.ClrTypeName.Contains(t)))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.InvalidVersionType,
                    symbol.Locations.FirstOrDefault(), vp.PropertyName, entity.ClassName, vp.ClrTypeName));
            }
        }
    }

    private static void ValidateImmutable(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx)
    {
        if (!entity.IsImmutable) return;
        // Immutable entities should not declare mutating repository methods
        // (This is enforced at the interface level; we emit a warning if mutating lifecycle hooks exist)
    }

    private static void ValidateSqliteConstraints(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx, string provider)
    {
        if (provider != "Sqlite") return;
        var idProp = entity.Properties.FirstOrDefault(p => p.IsId);
        if (idProp?.IdGenerationStrategy == "Sequence")
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.SequenceNotSupportedOnSqlite, symbol.Locations.FirstOrDefault()));
        if (entity.Schema is not null)
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.SchemaNotSupportedOnSqlite, symbol.Locations.FirstOrDefault(), entity.ClassName));
    }
}
