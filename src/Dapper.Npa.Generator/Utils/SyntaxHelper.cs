namespace Dapper.Npa.Generator.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class SyntaxHelper
{
    // Fully-qualified attribute names — generator identifies Dapper Npaattributes by string, not assembly reference
    public const string EntityAttr             = "Dapper.Npa.Core.Attributes.EntityAttribute";
    public const string MappedSuperclassAttr   = "Dapper.Npa.Core.Attributes.MappedSuperclassAttribute";
    public const string TableAttr              = "Dapper.Npa.Core.Attributes.TableAttribute";
    public const string ColumnAttr             = "Dapper.Npa.Core.Attributes.ColumnAttribute";
    public const string IdAttr                 = "Dapper.Npa.Core.Attributes.IdAttribute";
    public const string GeneratedValueAttr     = "Dapper.Npa.Core.Attributes.GeneratedValueAttribute";
    public const string SequenceGeneratorAttr  = "Dapper.Npa.Core.Attributes.SequenceGeneratorAttribute";
    public const string VersionAttr            = "Dapper.Npa.Core.Attributes.VersionAttribute";
    public const string TransientAttr          = "Dapper.Npa.Core.Attributes.TransientAttribute";
    public const string SortableAttr           = "Dapper.Npa.Core.Attributes.SortableAttribute";
    public const string FormulaAttr            = "Dapper.Npa.Core.Attributes.FormulaAttribute";
    public const string EmbeddedAttr           = "Dapper.Npa.Core.Attributes.EmbeddedAttribute";
    public const string EmbeddableAttr         = "Dapper.Npa.Core.Attributes.EmbeddableAttribute";
    public const string AttributeOverrideAttr  = "Dapper.Npa.Core.Attributes.AttributeOverrideAttribute";
    public const string ConverterAttr          = "Dapper.Npa.Core.Attributes.ConverterAttribute";
    public const string EnumeratedAttr         = "Dapper.Npa.Core.Attributes.EnumeratedAttribute";
    public const string ColumnTransformerAttr  = "Dapper.Npa.Core.Attributes.ColumnTransformerAttribute";
    public const string ProjectionAttr         = "Dapper.Npa.Core.Attributes.ProjectionAttribute";
    public const string ImmutableAttr          = "Dapper.Npa.Core.Attributes.ImmutableAttribute";
    public const string SoftDeleteAttr         = "Dapper.Npa.Core.Attributes.SoftDeleteAttribute";
    public const string TenantIdAttr           = "Dapper.Npa.Core.Attributes.TenantIdAttribute";
    public const string GlobalFilterAttr       = "Dapper.Npa.Core.Attributes.GlobalFilterAttribute";
    public const string GeneratedAttr          = "Dapper.Npa.Core.Attributes.GeneratedAttribute";
    public const string SecondaryTableAttr     = "Dapper.Npa.Core.Attributes.SecondaryTableAttribute";
    public const string IndexAttr              = "Dapper.Npa.Core.Attributes.IndexAttribute";
    public const string UniqueConstraintAttr   = "Dapper.Npa.Core.Attributes.UniqueConstraintAttribute";
    public const string CreatedDateAttr        = "Dapper.Npa.Core.Attributes.CreatedDateAttribute";
    public const string LastModifiedDateAttr   = "Dapper.Npa.Core.Attributes.LastModifiedDateAttribute";
    public const string CreatedByAttr          = "Dapper.Npa.Core.Attributes.CreatedByAttribute";
    public const string LastModifiedByAttr     = "Dapper.Npa.Core.Attributes.LastModifiedByAttribute";
    public const string OneToManyAttr          = "Dapper.Npa.Core.Attributes.OneToManyAttribute";
    public const string ManyToOneAttr          = "Dapper.Npa.Core.Attributes.ManyToOneAttribute";
    public const string OneToOneAttr           = "Dapper.Npa.Core.Attributes.OneToOneAttribute";
    public const string ManyToManyAttr         = "Dapper.Npa.Core.Attributes.ManyToManyAttribute";
    public const string JoinColumnAttr         = "Dapper.Npa.Core.Attributes.JoinColumnAttribute";
    public const string JoinTableAttr          = "Dapper.Npa.Core.Attributes.JoinTableAttribute";
    public const string OrderByAttr            = "Dapper.Npa.Core.Attributes.OrderByAttribute";
    public const string AssociationOverrideAttr = "Dapper.Npa.Core.Attributes.AssociationOverrideAttribute";
    public const string PrimaryKeyJoinColumnAttr = "Dapper.Npa.Core.Attributes.PrimaryKeyJoinColumnAttribute";
    public const string MapKeyAttr             = "Dapper.Npa.Core.Attributes.MapKeyAttribute";
    public const string NamedQueryAttr         = "Dapper.Npa.Core.Attributes.NamedQueryAttribute";
    public const string NamedQueriesAttr       = "Dapper.Npa.Core.Attributes.NamedQueriesAttribute";
    public const string QueryAttr              = "Dapper.Npa.Core.Attributes.QueryAttribute";
    public const string BulkOperationAttr      = "Dapper.Npa.Core.Attributes.BulkOperationAttribute";
    public const string StoredProcedureAttr    = "Dapper.Npa.Core.Attributes.StoredProcedureAttribute";
    public const string EntityListenersAttr    = "Dapper.Npa.Core.Attributes.EntityListenersAttribute";
    public const string PrePersistAttr         = "Dapper.Npa.Core.Attributes.PrePersistAttribute";
    public const string PostPersistAttr        = "Dapper.Npa.Core.Attributes.PostPersistAttribute";
    public const string PreUpdateAttr          = "Dapper.Npa.Core.Attributes.PreUpdateAttribute";
    public const string PostUpdateAttr         = "Dapper.Npa.Core.Attributes.PostUpdateAttribute";
    public const string PreRemoveAttr          = "Dapper.Npa.Core.Attributes.PreRemoveAttribute";
    public const string PostRemoveAttr         = "Dapper.Npa.Core.Attributes.PostRemoveAttribute";
    public const string PostLoadAttr           = "Dapper.Npa.Core.Attributes.PostLoadAttribute";
    public const string PrePersistBatchAttr    = "Dapper.Npa.Core.Attributes.PrePersistBatchAttribute";
    public const string PostPersistBatchAttr   = "Dapper.Npa.Core.Attributes.PostPersistBatchAttribute";
    public const string PreUpdateBatchAttr     = "Dapper.Npa.Core.Attributes.PreUpdateBatchAttribute";
    public const string PostUpdateBatchAttr    = "Dapper.Npa.Core.Attributes.PostUpdateBatchAttribute";
    public const string PreRemoveBatchAttr     = "Dapper.Npa.Core.Attributes.PreRemoveBatchAttribute";
    public const string PostRemoveBatchAttr    = "Dapper.Npa.Core.Attributes.PostRemoveBatchAttribute";
    public const string RepositoryAttr         = "Dapper.Npa.Core.Attributes.RepositoryAttribute";
    public const string IdClassAttr            = "Dapper.Npa.Core.Attributes.IdClassAttribute";
    public const string EmbeddedIdAttr         = "Dapper.Npa.Core.Attributes.EmbeddedIdAttribute";
    public const string ElementCollectionAttr  = "Dapper.Npa.Core.Attributes.ElementCollectionAttribute";
    public const string CollectionTableAttr    = "Dapper.Npa.Core.Attributes.CollectionTableAttribute";
    public const string NamedEntityGraphAttr   = "Dapper.Npa.Core.Attributes.NamedEntityGraphAttribute";
    public const string SubGraphAttr           = "Dapper.Npa.Core.Attributes.SubGraphAttribute";
    public const string OrderColumnAttr        = "Dapper.Npa.Core.Attributes.OrderColumnAttribute";

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
