using Dapper.Npa.Generator.Models;

namespace Dapper.Npa.Generator.Emitters;

using Generator.Models;

internal static class FormulaEmitter
{
    public static bool IsFormulaProperty(PropertyModel property) => property.Formula is not null;

    public static string FormatSelectColumn(PropertyModel property, string? tableAlias = null)
    {
        if (property.Formula is not null)
            return $"{property.Formula} AS {property.ColumnName}";

        if (property.ColumnTransformer?.Read is not null)
            return $"({property.ColumnTransformer.Read}) AS {property.PropertyName}";

        if (property.SecondaryTable is not null)
            return $"st_{SanitizeTableKey(property.SecondaryTable)}.{property.ColumnName} AS {property.PropertyName}";

        return tableAlias is not null
            ? $"{tableAlias}.{property.ColumnName} AS {property.PropertyName}"
            : $"{property.ColumnName} AS {property.PropertyName}";
    }

    public static string FormatProjectionExpression(PropertyModel property, string mainAlias)
    {
        if (property.Formula is not null)
            return property.Formula;

        if (property.ColumnTransformer?.Read is not null)
            return $"({property.ColumnTransformer.Read})";

        if (property.SecondaryTable is not null)
            return $"st_{SanitizeTableKey(property.SecondaryTable)}.{property.ColumnName}";

        return $"{mainAlias}.{property.ColumnName}";
    }

    private static string SanitizeTableKey(string tableName)
        => tableName.Replace(".", "_").Replace("-", "_");
}
