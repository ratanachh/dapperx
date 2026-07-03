namespace DapperX.Generator.Models;

internal sealed class NamedEntityGraphModel
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<string> AttributeNodes { get; init; } = [];
    public IReadOnlyList<SubGraphModel> SubGraphs { get; init; } = [];
    /// <summary>SELECT + FROM + JOINs (no WHERE).</summary>
    public string FromSql { get; init; } = string.Empty;
    /// <summary>Pre-generated SQL literal for LoadGraphAsync (FROM + WHERE id + root filters).</summary>
    public string GeneratedSql { get; init; } = string.Empty;
    public string FromSqlConstantName => $"Graph_{SanitizeName(Name)}_FromSql";
    public string SqlConstantName => $"Graph_{SanitizeName(Name)}_Sql";

    public static string SanitizeName(string name)
        => string.Concat(name.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
}

internal sealed class SubGraphModel
{
    public string RelationshipProperty { get; init; } = string.Empty;
    public IReadOnlyList<string> AttributeNodes { get; init; } = [];
}
