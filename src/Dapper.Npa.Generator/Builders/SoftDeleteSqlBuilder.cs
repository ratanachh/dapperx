using Dapper.Npa.Generator.Models;

namespace Dapper.Npa.Generator.Builders;

using Generator.Models;

/// <summary>Paired SELECT SQL for <c>[SoftDelete]</c> entities — active (filtered) and including-deleted (bypass).</summary>
internal static class SoftDeleteSqlBuilder
{
    internal sealed class ReadSqlPair
    {
        public string SelectById { get; init; } = string.Empty;
        public string? SelectByIdIncludingDeleted { get; init; }
        public string SelectAll { get; init; } = string.Empty;
        public string? SelectAllIncludingDeleted { get; init; }
        public string SelectByIds { get; init; } = string.Empty;
        public string? SelectByIdsIncludingDeleted { get; init; }
        public string Exists { get; init; } = string.Empty;
        public string? ExistsIncludingDeleted { get; init; }
        public string Count { get; init; } = string.Empty;
        public string? CountIncludingDeleted { get; init; }
        public string SelectAllPage { get; init; } = string.Empty;
        public string? SelectAllPageIncludingDeleted { get; init; }
        public string SelectAllSlice { get; init; } = string.Empty;
        public string? SelectAllSliceIncludingDeleted { get; init; }
        public string CountPage { get; init; } = string.Empty;
        public string? CountPageIncludingDeleted { get; init; }
    }

    public static ReadSqlPair BuildReadSqlPair(EntityModel entity, string provider)
    {
        var select = SqlBuilder.BuildSelect(entity, applySoftDelete: true, provider: provider);
        var selectBypass = SqlBuilder.BuildSelect(entity, applySoftDelete: false, provider: provider);
        var page = SqlBuilder.AppendPaging(select, provider, entity);
        var pageBypass = SqlBuilder.AppendPaging(selectBypass, provider, entity);
        var slice = SqlBuilder.AppendSlicePaging(select, provider, entity);
        var sliceBypass = SqlBuilder.AppendSlicePaging(selectBypass, provider, entity);

        return new ReadSqlPair
        {
            SelectById = SqlBuilder.BuildSelectById(entity, applySoftDelete: true, provider: provider),
            SelectByIdIncludingDeleted = SqlBuilder.BuildSelectById(entity, applySoftDelete: false, provider: provider),
            SelectAll = select,
            SelectAllIncludingDeleted = selectBypass,
            SelectByIds = SqlBuilder.BuildSelectByIds(entity, applySoftDelete: true, provider: provider),
            SelectByIdsIncludingDeleted = SqlBuilder.BuildSelectByIds(entity, applySoftDelete: false, provider: provider),
            Exists = SqlBuilder.BuildExists(entity, applySoftDelete: true, provider: provider),
            ExistsIncludingDeleted = SqlBuilder.BuildExists(entity, applySoftDelete: false, provider: provider),
            Count = SqlBuilder.BuildCount(entity, applySoftDelete: true, provider: provider),
            CountIncludingDeleted = SqlBuilder.BuildCount(entity, applySoftDelete: false, provider: provider),
            SelectAllPage = page,
            SelectAllPageIncludingDeleted = pageBypass,
            SelectAllSlice = slice,
            SelectAllSliceIncludingDeleted = sliceBypass,
            CountPage = SqlBuilder.BuildCountForPage(entity, applySoftDelete: true, provider: provider),
            CountPageIncludingDeleted = SqlBuilder.BuildCountForPage(entity, applySoftDelete: false, provider: provider),
        };
    }
}
