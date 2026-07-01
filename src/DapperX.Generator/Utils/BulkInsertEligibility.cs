namespace DapperX.Generator.Utils;

using DapperX.Generator.Builders;
using DapperX.Generator.Models;

internal static class BulkInsertEligibility
{
    public static bool IsEligible(EntityModel entity, string provider)
    {
        if (provider == "Sqlite")
            return false;
        if (entity.IsImmutable || entity.HasCompositeKey)
            return false;
        if (entity.SecondaryTables.Count > 0 || entity.ElementCollections.Count > 0)
            return false;
        if (entity.RequiresDbRow || entity.TenantIdColumn is not null || entity.Auditing is not null)
            return false;
        if (entity.Sequence is not null || GeneratedColumnSqlBuilder.HasGeneratedProperties(entity))
            return false;

        var idProp = entity.Properties.FirstOrDefault(p => p.IsId);
        return idProp?.IdGenerationStrategy == "Assigned";
    }

    public static bool ProviderSupportsBulkInsert(string provider)
        => provider is "SqlServer" or "PostgreSql" or "MySql";
}
