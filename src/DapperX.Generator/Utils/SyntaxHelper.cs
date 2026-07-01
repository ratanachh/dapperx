namespace DapperX.Generator.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class SyntaxHelper
{
    // Fully-qualified attribute names — generator identifies DapperX attributes by string, not assembly reference
    public const string EntityAttr             = "DapperX.Core.Attributes.EntityAttribute";
    public const string MappedSuperclassAttr   = "DapperX.Core.Attributes.MappedSuperclassAttribute";
    public const string TableAttr              = "DapperX.Core.Attributes.TableAttribute";
    public const string ColumnAttr             = "DapperX.Core.Attributes.ColumnAttribute";
    public const string IdAttr                 = "DapperX.Core.Attributes.IdAttribute";
    public const string GeneratedValueAttr     = "DapperX.Core.Attributes.GeneratedValueAttribute";
    public const string SequenceGeneratorAttr  = "DapperX.Core.Attributes.SequenceGeneratorAttribute";
    public const string VersionAttr            = "DapperX.Core.Attributes.VersionAttribute";
    public const string TransientAttr          = "DapperX.Core.Attributes.TransientAttribute";
    public const string SortableAttr           = "DapperX.Core.Attributes.SortableAttribute";
    public const string FormulaAttr            = "DapperX.Core.Attributes.FormulaAttribute";
    public const string EmbeddedAttr           = "DapperX.Core.Attributes.EmbeddedAttribute";
    public const string EmbeddableAttr         = "DapperX.Core.Attributes.EmbeddableAttribute";
    public const string AttributeOverrideAttr  = "DapperX.Core.Attributes.AttributeOverrideAttribute";
    public const string ConverterAttr          = "DapperX.Core.Attributes.ConverterAttribute";
    public const string EnumeratedAttr         = "DapperX.Core.Attributes.EnumeratedAttribute";
    public const string ColumnTransformerAttr  = "DapperX.Core.Attributes.ColumnTransformerAttribute";
    public const string ProjectionAttr         = "DapperX.Core.Attributes.ProjectionAttribute";
    public const string ImmutableAttr          = "DapperX.Core.Attributes.ImmutableAttribute";
    public const string SoftDeleteAttr         = "DapperX.Core.Attributes.SoftDeleteAttribute";
    public const string TenantIdAttr           = "DapperX.Core.Attributes.TenantIdAttribute";
    public const string GlobalFilterAttr       = "DapperX.Core.Attributes.GlobalFilterAttribute";
    public const string GeneratedAttr          = "DapperX.Core.Attributes.GeneratedAttribute";
    public const string SecondaryTableAttr     = "DapperX.Core.Attributes.SecondaryTableAttribute";
    public const string IndexAttr              = "DapperX.Core.Attributes.IndexAttribute";
    public const string UniqueConstraintAttr   = "DapperX.Core.Attributes.UniqueConstraintAttribute";
    public const string CreatedDateAttr        = "DapperX.Core.Attributes.CreatedDateAttribute";
    public const string LastModifiedDateAttr   = "DapperX.Core.Attributes.LastModifiedDateAttribute";
    public const string CreatedByAttr          = "DapperX.Core.Attributes.CreatedByAttribute";
    public const string LastModifiedByAttr     = "DapperX.Core.Attributes.LastModifiedByAttribute";
    public const string OneToManyAttr          = "DapperX.Core.Attributes.OneToManyAttribute";
    public const string ManyToOneAttr          = "DapperX.Core.Attributes.ManyToOneAttribute";
    public const string OneToOneAttr           = "DapperX.Core.Attributes.OneToOneAttribute";
    public const string ManyToManyAttr         = "DapperX.Core.Attributes.ManyToManyAttribute";
    public const string JoinColumnAttr         = "DapperX.Core.Attributes.JoinColumnAttribute";
    public const string JoinTableAttr          = "DapperX.Core.Attributes.JoinTableAttribute";
    public const string OrderByAttr            = "DapperX.Core.Attributes.OrderByAttribute";
    public const string AssociationOverrideAttr = "DapperX.Core.Attributes.AssociationOverrideAttribute";
    public const string PrimaryKeyJoinColumnAttr = "DapperX.Core.Attributes.PrimaryKeyJoinColumnAttribute";
    public const string MapKeyAttr             = "DapperX.Core.Attributes.MapKeyAttribute";
    public const string NamedQueryAttr         = "DapperX.Core.Attributes.NamedQueryAttribute";
    public const string NamedQueriesAttr       = "DapperX.Core.Attributes.NamedQueriesAttribute";
    public const string QueryAttr              = "DapperX.Core.Attributes.QueryAttribute";
    public const string BulkOperationAttr      = "DapperX.Core.Attributes.BulkOperationAttribute";
    public const string StoredProcedureAttr    = "DapperX.Core.Attributes.StoredProcedureAttribute";
    public const string EntityListenersAttr    = "DapperX.Core.Attributes.EntityListenersAttribute";
    public const string PrePersistAttr         = "DapperX.Core.Attributes.PrePersistAttribute";
    public const string PostPersistAttr        = "DapperX.Core.Attributes.PostPersistAttribute";
    public const string PreUpdateAttr          = "DapperX.Core.Attributes.PreUpdateAttribute";
    public const string PostUpdateAttr         = "DapperX.Core.Attributes.PostUpdateAttribute";
    public const string PreRemoveAttr          = "DapperX.Core.Attributes.PreRemoveAttribute";
    public const string PostRemoveAttr         = "DapperX.Core.Attributes.PostRemoveAttribute";
    public const string PostLoadAttr           = "DapperX.Core.Attributes.PostLoadAttribute";
    public const string PrePersistBatchAttr    = "DapperX.Core.Attributes.PrePersistBatchAttribute";
    public const string PostPersistBatchAttr   = "DapperX.Core.Attributes.PostPersistBatchAttribute";
    public const string PreUpdateBatchAttr     = "DapperX.Core.Attributes.PreUpdateBatchAttribute";
    public const string PostUpdateBatchAttr    = "DapperX.Core.Attributes.PostUpdateBatchAttribute";
    public const string PreRemoveBatchAttr     = "DapperX.Core.Attributes.PreRemoveBatchAttribute";
    public const string PostRemoveBatchAttr    = "DapperX.Core.Attributes.PostRemoveBatchAttribute";
    public const string RepositoryAttr         = "DapperX.Core.Attributes.RepositoryAttribute";
    public const string IdClassAttr            = "DapperX.Core.Attributes.IdClassAttribute";
    public const string EmbeddedIdAttr         = "DapperX.Core.Attributes.EmbeddedIdAttribute";
    public const string ElementCollectionAttr  = "DapperX.Core.Attributes.ElementCollectionAttribute";
    public const string CollectionTableAttr    = "DapperX.Core.Attributes.CollectionTableAttribute";
    public const string NamedEntityGraphAttr   = "DapperX.Core.Attributes.NamedEntityGraphAttribute";
    public const string SubGraphAttr           = "DapperX.Core.Attributes.SubGraphAttribute";
    public const string OrderColumnAttr        = "DapperX.Core.Attributes.OrderColumnAttribute";

    public static bool HasAttribute(ISymbol symbol, string fullyQualifiedAttributeName)
        => symbol.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == fullyQualifiedAttributeName);

    public static AttributeData? GetAttribute(ISymbol symbol, string fullyQualifiedAttributeName)
        => symbol.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString() == fullyQualifiedAttributeName);

    public static IEnumerable<AttributeData> GetAttributes(ISymbol symbol, string fullyQualifiedAttributeName)
        => symbol.GetAttributes().Where(a =>
            a.AttributeClass?.ToDisplayString() == fullyQualifiedAttributeName);

    public static T? GetNamedArg<T>(AttributeData? attr, string name)
    {
        if (attr is null || string.IsNullOrEmpty(name)) return default;
        foreach (var kvp in attr.NamedArguments)
        {
            if (kvp.Key != name) continue;
            return kvp.Value.Value is T t ? t : default;
        }
        return default;
    }

    public static T? GetConstructorArg<T>(AttributeData? attr, int index)
    {
        if (attr is null) return default;
        if (attr.ConstructorArguments.Length <= index) return default;
        return attr.ConstructorArguments[index].Value is T t ? t : default;
    }

    public static IReadOnlyList<string> GetStringArrayNamedArg(AttributeData? attr, string name)
    {
        if (attr is null) return [];
        foreach (var kvp in attr.NamedArguments)
        {
            if (kvp.Key != name) continue;
            return ExtractStringConstants(kvp.Value);
        }
        return [];
    }

    public static IReadOnlyList<string> GetTypeArrayNamedArg(AttributeData? attr, string name)
    {
        if (attr is null) return [];
        foreach (var kvp in attr.NamedArguments)
        {
            if (kvp.Key != name) continue;
            return ExtractTypeConstants(kvp.Value);
        }
        return [];
    }

    private static IReadOnlyList<string> ExtractTypeConstants(TypedConstant constant)
    {
        if (constant.IsNull)
            return [];

        if (constant.Kind == TypedConstantKind.Array)
        {
            return constant.Values
                .SelectMany(ExtractTypeConstants)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        if (constant.Value is INamedTypeSymbol type)
            return [type.ToDisplayString()];

        return [];
    }

    private static IReadOnlyList<string> ExtractStringConstants(TypedConstant constant)
    {
        if (constant.IsNull)
            return [];

        if (constant.Kind == TypedConstantKind.Array)
        {
            return constant.Values
                .SelectMany(ExtractStringConstants)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        if (constant.Value is string s && !string.IsNullOrEmpty(s))
            return [s];

        return [];
    }

    /// <summary>Converts PascalCase property name to snake_case column name.</summary>
    public static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                sb.Append('_');
            sb.Append(char.ToLowerInvariant(name[i]));
        }
        return sb.ToString();
    }
}
