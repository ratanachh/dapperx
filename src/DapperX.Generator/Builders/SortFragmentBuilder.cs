namespace DapperX.Generator.Builders;
using DapperX.Generator.Models;
/// <summary>Builds ORDER BY fragment literals per [Sortable] property × direction.</summary>
internal static class SortFragmentBuilder
{
    public static IReadOnlyList<(string Property, string Column, bool Ascending, string Fragment)>
        Build(EntityModel entity)
    {
        var result = new List<(string, string, bool, string)>();
        foreach (var p in entity.Properties.Where(p => p.IsSortable))
        {
            result.Add((p.PropertyName, p.ColumnName, true,  $" ORDER BY {p.ColumnName} ASC"));
            result.Add((p.PropertyName, p.ColumnName, false, $" ORDER BY {p.ColumnName} DESC"));
        }
        return result;
    }
}
